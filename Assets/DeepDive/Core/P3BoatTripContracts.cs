using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // docs/plan/CONTRACTS.md "BoatTripState": Docked -> Outbound -> Anchored -> Inbound -> Docked.
    // Deliberately separate from SessionState.DiveId/SessionPhase - a shore diver and a boat
    // passenger can share one DiveId, and a boat departing or returning never starts a new dive.
    public enum BoatTripPhase : byte
    {
        Docked = 0,
        Outbound = 1,
        Anchored = 2,
        Inbound = 3
    }

    // Fixed campaign boat, its four seats and its one near route (Plan 3.0: "baslangic sandali...
    // Sabit rota korunur"; more boats/routes are P4.3). Ids are stable so a save and a world anchor
    // can reference them without drifting, the same reasoning as BoatRepairParts.
    public static class BoatTripIds
    {
        public const string BoatId = BoatRepairParts.BoatId;   // "boat-1" - #62 already fixed this
        public const string NearRouteId = "route-near-1";
        public const string Seat0 = "seat-0";
        public const string Seat1 = "seat-1";
        public const string Seat2 = "seat-2";
        public const string Seat3 = "seat-3";

        public static readonly IReadOnlyList<string> Seats = new[] { Seat0, Seat1, Seat2, Seat3 };

        public static bool IsSeat(string seatId)
        {
            for (var i = 0; i < Seats.Count; i++)
                if (string.Equals(Seats[i], seatId, System.StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // One occupied seat. An empty seat is simply absent from BoatTripState.Seats rather than a
    // fixed-size array with a placeholder id, so nothing downstream has to know how to skip a hole.
    public readonly struct BoatSeatAssignment
    {
        public readonly string SeatId;
        public readonly PlayerId Player;

        public BoatSeatAssignment(string seatId, PlayerId player)
        {
            SeatId = seatId ?? string.Empty;
            Player = player;
        }
    }

    // docs/plan/CONTRACTS.md "BoatTripState": boatId, tripId, routeId, phase, seat->player mapping,
    // trip party, trip owner, revision. Mert owns this shape and the transitions between phases;
    // Mehmet's seat/boarding and fixed-route movement and Utku's dock/anchor/route data are read
    // here, never written here - both call the seams below instead of mutating trip state directly.
    public readonly struct BoatTripState
    {
        public readonly string BoatId;
        public readonly string TripId;
        public readonly string RouteId;
        public readonly BoatTripPhase Phase;
        public readonly IReadOnlyList<BoatSeatAssignment> Seats;
        public readonly IReadOnlyList<PlayerId> Party;
        public readonly PlayerId Owner;
        public readonly bool HasOwner;
        public readonly int Revision;

        public BoatTripState(string boatId, string tripId, string routeId, BoatTripPhase phase,
            IReadOnlyList<BoatSeatAssignment> seats, IReadOnlyList<PlayerId> party,
            PlayerId owner, bool hasOwner, int revision)
        {
            BoatId = boatId ?? string.Empty;
            TripId = tripId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            Phase = phase;
            Seats = seats;
            Party = party;
            Owner = owner;
            HasOwner = hasOwner;
            Revision = revision;
        }
    }

    // Utku's world data for one route (docs/plan/CONTRACTS.md "DiveRouteDefinition"): stable anchor
    // ids only, the same reasoning ServicePointDefinition.WorldAnchor gives - the runtime Transform
    // lives on a World-authored scene component, so Core stays free of UnityEngine types. Mehmet's
    // fixed-route movement reads DepartureDockAnchor/AnchorPointAnchor as its start/end; Mert's map
    // UI reads them to place the dock and anchor icons.
    public readonly struct DiveRouteDefinition
    {
        public readonly string RouteId;
        public readonly string DepartureDockAnchor;
        public readonly string AnchorPointAnchor;
        public readonly float OutboundSeconds;
        public readonly float InboundSeconds;

        public DiveRouteDefinition(string routeId, string departureDockAnchor, string anchorPointAnchor,
            float outboundSeconds, float inboundSeconds)
        {
            RouteId = routeId ?? string.Empty;
            DepartureDockAnchor = departureDockAnchor ?? string.Empty;
            AnchorPointAnchor = anchorPointAnchor ?? string.Empty;
            OutboundSeconds = outboundSeconds;
            InboundSeconds = inboundSeconds;
        }
    }

    // A player's live-approved position for the map icon layer (docs/plan/CONTRACTS.md
    // "WorldMapState": "Mehmet onaylı oyuncu/aktif tekne konumu, Utku dünya-harita dönüşümü... Mert
    // UI ve kayıt katmanı"). MapX/MapZ are already in Utku's normalized map space (0..1 across the
    // region), not raw world coordinates - the world<->map transform is his, never duplicated here
    // or in the UI. A shore diver has no entry; only the approved boat party and the boat itself
    // are ever on the map, so a hidden fish or a diver mid-dive is never exposed.
    public readonly struct MapIcon
    {
        public readonly string IconId;
        public readonly float MapX;
        public readonly float MapZ;

        public MapIcon(string iconId, float mapX, float mapZ)
        {
            IconId = iconId ?? string.Empty;
            MapX = mapX;
            MapZ = mapZ;
        }
    }

    // Seam for Mehmet's seat/boarding interaction: his layer validates proximity, one player per
    // seat and that the boat is not already moving, then calls TryBoard/TryDisembark. Mirrors
    // BoatPartClaim's Bind/Unbind pattern exactly. Composition binds the handler to the trip
    // manager while it is the host; unbound (client, no manager) means boarding cannot be resolved.
    public static class BoatBoarding
    {
        public static System.Func<PlayerId, string, string, ulong, TransactionResult> BoardHandler { get; private set; }
        public static System.Func<PlayerId, ulong, TransactionResult> DisembarkHandler { get; private set; }

        public static bool IsBound => BoardHandler != null && DisembarkHandler != null;

        public static void Bind(
            System.Func<PlayerId, string, string, ulong, TransactionResult> boardHandler,
            System.Func<PlayerId, ulong, TransactionResult> disembarkHandler)
        {
            BoardHandler = boardHandler;
            DisembarkHandler = disembarkHandler;
        }

        public static void Unbind(
            System.Func<PlayerId, string, string, ulong, TransactionResult> boardHandler,
            System.Func<PlayerId, ulong, TransactionResult> disembarkHandler)
        {
            if (BoardHandler == boardHandler) BoardHandler = null;
            if (DisembarkHandler == disembarkHandler) DisembarkHandler = null;
        }

        // boatId/seatId, the same order BoatPartClaim.TryClaimFound takes its target id in.
        public static TransactionResult TryBoard(PlayerId player, string boatId, string seatId, ulong requestId) =>
            BoardHandler != null ? BoardHandler(player, boatId, seatId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TryDisembark(PlayerId player, ulong requestId) =>
            DisembarkHandler != null ? DisembarkHandler(player, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    // Seam for Mehmet's fixed-route movement: once his host-authoritative mover physically reaches
    // the departure dock or the route's anchor point, it reports that arrival here rather than the
    // trip manager guessing from a distance check of its own - one movement authority, matching
    // CONTRACTS.md "ucu sandal prefabina ve sahneye sirayla baglanirlar; uc ayri sandal otoritesi
    // kurulmaz". The trip manager still decides whether the report is valid for the current phase.
    public static class BoatRouteProgress
    {
        public static System.Func<string, BoatTripPhase, ulong, TransactionResult> ArrivalHandler { get; private set; }

        public static bool IsBound => ArrivalHandler != null;

        public static void Bind(System.Func<string, BoatTripPhase, ulong, TransactionResult> handler) => ArrivalHandler = handler;

        public static void Unbind(System.Func<string, BoatTripPhase, ulong, TransactionResult> handler)
        {
            if (ArrivalHandler == handler) ArrivalHandler = null;
        }

        // reachedPhase is the phase the arrival completes (Anchored for the outbound leg, Docked
        // for the inbound leg) - not the phase the trip was in, so the caller cannot get the two
        // directions backwards.
        public static TransactionResult ReportArrival(string boatId, BoatTripPhase reachedPhase, ulong requestId) =>
            ArrivalHandler != null ? ArrivalHandler(boatId, reachedPhase, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }
}
