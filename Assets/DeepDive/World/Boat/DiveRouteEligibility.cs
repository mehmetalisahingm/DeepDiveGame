using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // Which vehicle may sail which route (P4.3-B2 #108, CONTRACTS "Tekne satin alma ve aktif arac": "Utku rota
    // uygunlugunu saglar"). One rule: the active vehicle's class is at least the route's RequiredVehicleClass. A
    // bigger boat sails every route a smaller one can; there is no route x vehicle matrix and no routeId -> class
    // table here - the class comes from the route's own definition.
    //
    // Fail-closed. None and any value outside the defined classes are refused on either side, so default(...) (class
    // None, id null) and a corrupt enum never pass: a numeric (VehicleClass)9 >= ResearchBoat would otherwise unlock
    // everything. Pure and static: it decides nothing about ownership (Mert) or movement (Mehmet).
    public static class DiveRouteEligibility
    {
        public static bool IsEligible(in DiveRouteDefinition route, VehicleClass activeClass)
        {
            if (string.IsNullOrEmpty(route.RouteId)) return false;
            if (!IsBoat(route.RequiredVehicleClass) || !IsBoat(activeClass)) return false;
            return activeClass >= route.RequiredVehicleClass;
        }

        private static bool IsBoat(VehicleClass vehicleClass) =>
            vehicleClass == VehicleClass.Rowboat ||
            vehicleClass == VehicleClass.Motorboat ||
            vehicleClass == VehicleClass.ResearchBoat;
    }
}
