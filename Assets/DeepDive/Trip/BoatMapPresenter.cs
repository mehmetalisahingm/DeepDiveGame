using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Trip
{
    // Pure computation, no Unity scene queries and no world<->map math: given the trip state, where
    // the dock/anchor sit in map space (Utku's data, already the normalized coordinates MapIcon
    // expects) and, once under way, Mehmet's approved live boat position, produces exactly the icons
    // the map screen should draw. Keeping the conversion math entirely out of here is what keeps it
    // entirely Utku's, never duplicated in the UI (docs/plan/CONTRACTS.md "Harita ve kalici kesif":
    // "gizli balik/boss konumunu yayinlamaz" - nothing but the dock and the boat is ever built here,
    // so there is nothing to leak).
    public static class BoatMapPresenter
    {
        public const string DockIconId = "dock";

        // dock is always known (it exists whether or not a trip is running). anchor/liveBoat are
        // null until Utku's route data and Mehmet's live-position publishing exist - callers pass
        // what they actually have, this never invents a position for what it was not given.
        public static IReadOnlyList<MapIcon> BuildIcons(
            BoatTripState trip,
            (float X, float Z) dock,
            (float X, float Z)? anchor,
            (float X, float Z)? liveBoat)
        {
            var icons = new List<MapIcon>(2) { new MapIcon(DockIconId, dock.X, dock.Z) };

            var boat = BoatPosition(trip.Phase, dock, anchor, liveBoat);
            if (boat.HasValue) icons.Add(new MapIcon(trip.BoatId, boat.Value.X, boat.Value.Z));

            return icons;
        }

        // Docked -> the dock; Anchored -> the anchor point; Outbound/Inbound -> Mehmet's live
        // approved position if it has been published, otherwise the boat is simply not drawn -
        // interpolating a guess would itself be an unapproved position, exactly what CONTRACTS
        // reserves for Mehmet ("Mehmet onayli oyuncu/aktif tekne konumu"), so "not yet published"
        // means "not shown", never a guess.
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
