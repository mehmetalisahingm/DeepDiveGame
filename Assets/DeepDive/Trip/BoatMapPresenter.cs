using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Trip
{
    // Pure computation, no Unity scene queries and no world<->map math: every position it is given
    // is already normalized map space (Utku's DiveRegionField did the conversion) and every live
    // position is one the host approved (Mehmet's replicated boat pose / player transforms). Keeping
    // the arithmetic out of here is what keeps it entirely Utku's, never duplicated in the UI
    // (docs/plan/CONTRACTS.md "Harita ve kalici kesif": "gizli balik/boss konumunu yayinlamaz" -
    // only the dock, the boat, the connected party and a return marker are ever built here, so
    // there is nothing else to leak).
    public static class BoatMapPresenter
    {
        public const string DockIconId = "dock";
        public const string ReturnMarkerIconId = "boat-return";
        public const string PlayerIconPrefix = "player-";

        public static string PlayerIconId(PlayerId player) => PlayerIconPrefix + player.Value;

        // Dock + boat only. Kept as the original entry point; the party-aware overload below is a
        // superset of it.
        public static IReadOnlyList<MapIcon> BuildIcons(
            BoatTripState trip,
            (float X, float Z) dock,
            (float X, float Z)? anchor,
            (float X, float Z)? liveBoat) =>
            BuildIcons(trip.Phase, trip.BoatId, dock, anchor, liveBoat, null, false);

        // dock is always known (it exists whether or not a trip is running). anchor/liveBoat are
        // null until the world/host actually supply them - callers pass what they really have and
        // this never invents a position for what it was not given.
        //
        // players are the CONNECTED party's host-approved map positions. A player whose position
        // could not be converted (outside the region) is simply absent from the list, never pinned
        // to an edge. localPlayerAboard suppresses the return marker: a return marker answers "where
        // do I swim back to", which is meaningless while already sitting in the boat.
        public static IReadOnlyList<MapIcon> BuildIcons(
            BoatTripPhase phase,
            string boatId,
            (float X, float Z) dock,
            (float X, float Z)? anchor,
            (float X, float Z)? liveBoat,
            IReadOnlyList<(PlayerId Player, float X, float Z)> players,
            bool localPlayerAboard)
        {
            var icons = new List<MapIcon>(4) { new MapIcon(DockIconId, dock.X, dock.Z) };

            var boat = BoatPosition(phase, dock, anchor, liveBoat);
            if (boat.HasValue) icons.Add(new MapIcon(boatId, boat.Value.X, boat.Value.Z));

            // Only while the boat waits at the anchorage does a diver have somewhere to swim back
            // to. Docked needs no marker (the dock icon is the destination), and underway the hull
            // is in motion - a fixed marker there would point at where the boat WAS.
            if (phase == BoatTripPhase.Anchored && boat.HasValue && !localPlayerAboard)
                icons.Add(new MapIcon(ReturnMarkerIconId, boat.Value.X, boat.Value.Z));

            if (players != null)
                for (var i = 0; i < players.Count; i++)
                    icons.Add(new MapIcon(PlayerIconId(players[i].Player), players[i].X, players[i].Z));

            return icons;
        }

        // Docked -> the dock; Anchored -> the anchor point; Outbound/Inbound -> the live approved
        // position if it has been published, otherwise the boat is simply not drawn - interpolating
        // a guess would itself be an unapproved position, exactly what CONTRACTS reserves for
        // Mehmet, so "not yet published" means "not shown", never a guess.
        private static (float X, float Z)? BoatPosition(
            BoatTripPhase phase, (float X, float Z) dock, (float X, float Z)? anchor, (float X, float Z)? liveBoat) =>
            phase switch
            {
                BoatTripPhase.Docked => dock,
                BoatTripPhase.Anchored => anchor,
                _ => liveBoat
            };
    }
}
