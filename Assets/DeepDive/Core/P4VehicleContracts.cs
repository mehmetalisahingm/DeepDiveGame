using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.3-C (#109) "VesselOwnershipState": the three vehicles of the campaign, their stable ids and the ownership/active
    // selection read-model. The AUTHORITY is EconomyManager (host); this file only holds ids, catalog data, the read shape
    // and the seams Mehmet's movement / Utku's routes / the map consume. Nothing here moves a boat or decides a route.
    //
    // The boatId IS the vessel instance id: the campaign has exactly one of each vehicle, so a purchase can never create a
    // second one and the id is stable across saves. "boat-1" is the P3 rowboat (BoatRepairParts.BoatId): it is owned when
    // its repair is complete (the P3 repair state stays the single source of that fact), it is never sold.
    public enum VehicleClass : byte
    {
        None = 0,
        Rowboat = 1,
        Motorboat = 2,
        ResearchBoat = 3
    }

    public static class VehicleIds
    {
        public const string Rowboat = BoatRepairParts.BoatId;   // "boat-1" - P3 id kept, saves/map/trip already reference it
        public const string Motorboat = "boat-2";
        public const string ResearchBoat = "boat-3";

        public static readonly IReadOnlyList<string> All = new[] { Rowboat, Motorboat, ResearchBoat };

        public static bool IsVehicle(string boatId)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i], boatId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    public readonly struct VehicleDefinition
    {
        public readonly string BoatId;
        public readonly string DisplayName;
        public readonly VehicleClass Class;
        public readonly int Price;               // 0 = not for sale (the rowboat is repaired, not bought)
        public readonly string RequiresBoatId;   // must be owned before this one can be bought ("" = none)

        public VehicleDefinition(string boatId, string displayName, VehicleClass vehicleClass, int price, string requiresBoatId)
        {
            BoatId = boatId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Class = vehicleClass;
            Price = price;
            RequiresBoatId = requiresBoatId ?? string.Empty;
        }

        public bool IsForSale => Price > 0;
    }

    public static class VehicleCatalog
    {
        // WORKING VALUES (like ChannelResultRules): the progression order is fixed by the plan (rowboat -> motorboat ->
        // research boat); the prices are first numbers for the balance pass in P4.5, not final.
        public const int MotorboatPrice = 900;
        public const int ResearchBoatPrice = 2400;

        public static readonly IReadOnlyList<VehicleDefinition> All = new[]
        {
            new VehicleDefinition(VehicleIds.Rowboat, "Sandal", VehicleClass.Rowboat, 0, ""),
            new VehicleDefinition(VehicleIds.Motorboat, "Motorlu tekne", VehicleClass.Motorboat, MotorboatPrice, VehicleIds.Rowboat),
            new VehicleDefinition(VehicleIds.ResearchBoat, "Kucuk arastirma teknesi", VehicleClass.ResearchBoat, ResearchBoatPrice, VehicleIds.Motorboat)
        };

        public static bool TryGet(string boatId, out VehicleDefinition definition)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i].BoatId, boatId, StringComparison.Ordinal)) { definition = All[i]; return true; }
            definition = default;
            return false;
        }

        public static VehicleClass ClassOf(string boatId) => TryGet(boatId, out var definition) ? definition.Class : VehicleClass.None;
    }

    // What every consumer reads. ActiveBoatId is "" only while no vehicle is owned at all (the rowboat is still broken).
    public readonly struct VehicleFleetState
    {
        public readonly IReadOnlyList<string> OwnedBoatIds;
        public readonly string ActiveBoatId;
        public readonly int Revision;

        public VehicleFleetState(IReadOnlyList<string> ownedBoatIds, string activeBoatId, int revision)
        {
            OwnedBoatIds = ownedBoatIds ?? Array.Empty<string>();
            ActiveBoatId = activeBoatId ?? string.Empty;
            Revision = revision;
        }

        public bool IsOwned(string boatId)
        {
            if (OwnedBoatIds == null) return false;
            for (var i = 0; i < OwnedBoatIds.Count; i++)
                if (string.Equals(OwnedBoatIds[i], boatId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // Seam: which vehicle is the one at sea. Host composition binds it to the economy; Mehmet's controller/seat layer,
    // the boat map icon and Utku's route eligibility read it instead of assuming "boat-1". Unbound (no economy) falls
    // back to the P3 rowboat so every P3 path keeps working untouched.
    public static class ActiveVehicle
    {
        private static Func<string> provider;

        public static void Bind(Func<string> activeBoatId) => provider = activeBoatId;

        public static void Unbind(Func<string> activeBoatId)
        {
            if (provider == activeBoatId) provider = null;
        }

        public static string BoatId
        {
            get
            {
                var id = provider != null ? provider() : null;
                return string.IsNullOrEmpty(id) ? VehicleIds.Rowboat : id;
            }
        }

        public static VehicleClass Class => VehicleCatalog.ClassOf(BoatId);
    }

    // Seam: "is the vehicle in a state where the active one may be swapped?" Bound by the trip authority (host). Returns
    // null when allowed, otherwise the reject reason: TripActive (the boat is not docked) or SeatsOccupied.
    // Unbound = nothing is travelling = allowed.
    public static class VehicleSwitchGate
    {
        private static Func<string> provider;

        public static void Bind(Func<string> reasonOrNull) => provider = reasonOrNull;

        public static void Unbind(Func<string> reasonOrNull)
        {
            if (provider == reasonOrNull) provider = null;
        }

        public static string Check() => provider != null ? provider() : null;
    }
}
