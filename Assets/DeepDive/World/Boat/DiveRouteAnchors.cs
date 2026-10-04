using System;
using System.Collections.Generic;

namespace DeepDive.World
{
    // The ends of the dive routes, by name (#76, P4.3 #108). docs/plan/CONTRACTS.md P3.3-C note 1:
    // "Dock/anchor/route dunya verisi Utku'nundur; Core yalniz stabil anchor id'lerini tutar
    // (DiveRouteDefinition.DepartureDockAnchor/AnchorPointAnchor)". Core holds the fields that
    // carry these strings; the strings themselves are world data and live here, the way
    // BoatRepairParts' three ids live beside the repair they describe.
    //
    // Frozen. A scene anchor, Mehmet's mover and Mert's map icons all name the same two strings,
    // so renaming one is a coordinated change across three people, not a local edit - which is
    // exactly why they are constants here instead of literals typed into each caller.
    //
    // One departure shared by every route and one anchorage per route: the near anchorage (P3.3)
    // and P4.3's reef and deep ones, the three sea anchors of GAMEPLAY_LOOP. The boarding spot
    // beside an anchored hull is NOT another id (see RouteAnchor.BoardingPosition); minting one
    // would put a new string into a contract three people already agreed on.
    public static class DiveRouteAnchors
    {
        public const string Dock = "dock-town-1";
        public const string AnchorPoint = "anchor-near-1";
        public const string ReefAnchorPoint = "anchor-reef-1";
        public const string DeepAnchorPoint = "anchor-deep-1";

        public static readonly IReadOnlyList<string> AnchorPoints = new[] { AnchorPoint, ReefAnchorPoint, DeepAnchorPoint };

        public static readonly IReadOnlyList<string> All = new[] { Dock, AnchorPoint, ReefAnchorPoint, DeepAnchorPoint };

        public static bool IsAnchor(string anchorId) => Contains(All, anchorId);

        // A sea anchorage: where a route ends. The dock is a departure, never an anchorage.
        public static bool IsAnchorPoint(string anchorId) => Contains(AnchorPoints, anchorId);

        private static bool Contains(IReadOnlyList<string> ids, string anchorId)
        {
            for (var i = 0; i < ids.Count; i++)
                if (string.Equals(ids[i], anchorId, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
