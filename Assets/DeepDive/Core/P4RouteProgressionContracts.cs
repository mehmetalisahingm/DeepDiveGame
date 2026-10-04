namespace DeepDive.Core.Contracts
{
    // Read-only seam from World into the trip authority. World remains the owner of canonical
    // route definitions; Trip only asks what a route requires and never carries its own table.
    public interface IBoatTripRouteCatalog
    {
        bool TryGetDefinition(string routeId, out DiveRouteDefinition definition);
    }

    // Campaign-level read seam used by progression rules such as the research-boat unlock.
    // The implementation must survive scene changes/save reloads; Economy must not depend on
    // the live World exploration authority directly.
    public interface IExplorationProgressReadModel
    {
        bool HasDiscoveredCellInBand(string depthBandId);
    }
}
