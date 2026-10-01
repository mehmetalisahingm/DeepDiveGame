using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // One host-approved explorer position: a connected player and where the host's own
    // authoritative state puts them this tick.
    public readonly struct ExplorerPosition
    {
        public readonly PlayerId Player;
        public readonly Vector3 WorldPosition;

        public ExplorerPosition(PlayerId player, Vector3 worldPosition)
        {
            Player = player;
            WorldPosition = worldPosition;
        }
    }

    // The input seam of the exploration fog (docs/plan/CONTRACTS.md "Harita ve kalici kesif":
    // "Mehmet onayli oyuncu/aktif tekne konumunu ... saglar"). World's cell logic reads approved
    // positions through this and nothing else; it never finds players in the scene itself.
    //
    // Players only. The active boat's position is for the map icon and navigation (P4MapPositionFeed)
    // and never unlocks a cell; a cell opens because a player's approved position reached it.
    //
    // No compile dependency on Mehmet's P4MapPositionFeed (#92) or on NetworkPlayer: tests hand in
    // a fake source, and Composition binds the real feed at runtime. Only host-validated positions
    // belong here - a client-reported position never opens a cell.
    public interface IExplorerPositionSource
    {
        // Fills into (cleared first) with this tick's approved positions, so the host can poll every
        // tick without allocating.
        void CollectPositions(List<ExplorerPosition> into);
    }
}
