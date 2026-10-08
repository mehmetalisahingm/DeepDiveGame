using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Trip;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.Composition
{
    // Read-only P4.1 map seam for Mert's map/UI. No position is authored here:
    // players come from server-authoritative NetworkPlayer transforms, the boat comes from the
    // host-published BoatTripPlayerSync mirror, and world->map conversion stays in Utku's World layer.
    public static class P4MapPositionFeed
    {
        public static bool TryGetPlayerWorldPosition(PlayerId playerId, out Vector3 worldPosition)
        {
            var players = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!player.IsSpawned || player.OwnerClientId != playerId.Value) continue;
                worldPosition = player.transform.position;
                return true;
            }
            worldPosition = default;
            return false;
        }

        public static bool TryGetBoatWorldPosition(out Vector3 worldPosition)
        {
            var syncs = Object.FindObjectsByType<BoatTripPlayerSync>(FindObjectsSortMode.None);
            for (var i = 0; i < syncs.Length; i++)
            {
                var sync = syncs[i];
                if (!sync.IsSpawned || !sync.IsOwner || !sync.BoatVisible.Value) continue;
                worldPosition = sync.BoatWorldPosition.Value;
                return true;
            }
            worldPosition = default;
            return false;
        }

        public static IReadOnlyList<(PlayerId Player, Vector3 WorldPosition)> SnapshotPlayers()
        {
            var result = new List<(PlayerId, Vector3)>();
            var players = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
                if (players[i].IsSpawned)
                    result.Add((new PlayerId(players[i].OwnerClientId), players[i].transform.position));
            return result;
        }

        // P4.5-A team pings use the same approved player objects and the same World->Map
        // conversion as party/return icons. The ping position was stamped from the server
        // transform in NetworkPlayer; clients never send a coordinate.
        public static IReadOnlyList<(PlayerId Player, CrewPingKind Kind, Vector3 WorldPosition)> SnapshotTeamPings()
        {
            var result = new List<(PlayerId, CrewPingKind, Vector3)>();
            var players = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!player.IsSpawned || !player.TryGetActiveTeamPing(out var kind, out var world)) continue;
                result.Add((new PlayerId(player.OwnerClientId), kind, world));
            }
            return result;
        }

        public static bool TryWorldToMap(Vector3 worldPosition, out Vector2 mapPosition)
        {
            if (DiveRegionField.TryFind(out var region) && region.TryWorldToMap(worldPosition, out var map))
            {
                mapPosition = map;
                return true;
            }
            mapPosition = default;
            return false;
        }
    }
}
