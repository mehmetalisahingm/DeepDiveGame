using System;
using System.Collections.Generic;

namespace DeepDive.World
{
    // The two ends of the near route, by name (#76). docs/plan/CONTRACTS.md P3.3-C note 1:
    // "Dock/anchor/route dunya verisi Utku'nundur; Core yalniz stabil anchor id'lerini tutar
    // (DiveRouteDefinition.DepartureDockAnchor/AnchorPointAnchor)". Core holds the fields that
    // carry these strings; the strings themselves are world data and live here, the way
    // BoatRepairParts' three ids live beside the repair they describe.
    //
    // Frozen. A scene anchor, Mehmet's mover and Mert's map icons all name the same two strings,
    // so renaming one is a coordinated change across three people, not a local edit - which is
    // exactly why they are constants here instead of literals typed into each caller.
    //
    // Only two, and deliberately so: the near route has one departure and one anchorage. The
    // boarding spot beside the anchored hull is NOT a third id (see RouteAnchor.BoardingPosition);
    // minting one would put a new string into a contract three people already agreed on.
    public static class DiveRouteAnchors
    {
        public const string Dock = "dock-town-1";
        public const string AnchorPoint = "anchor-near-1";

        public static readonly IReadOnlyList<string> All = new[] { Dock, AnchorPoint };

        public static bool IsAnchor(string anchorId)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i], anchorId, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
