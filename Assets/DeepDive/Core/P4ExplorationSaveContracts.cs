using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.1-C (#90): the persistent side of Utku's exploration domain (#89/#94) and the one place the map
    // and encyclopedia UI find the host's read model. Utku owns what a cell or an observation IS and when
    // one counts; this file only says how it is written into the campaign file and how the UI reaches it.
    //
    // Save rule: store only what the host already counted, in its canonical form - cell ids with their
    // grid coordinates and band, and the observations themselves (their own ids, cells and bands). A reload
    // re-applies them through the authority's own idempotent path, so nothing is re-derived from a world
    // position and nothing can be counted twice. No map coordinate is saved: where a cell is drawn is
    // always derived again from the grid (ExplorationMapProjection), never a second stored transform.

    [Serializable]
    public sealed class ExplorationCellSave
    {
        public string CellId = "";
        public int GridX;
        public int GridZ;
        public string DepthBandId = "";
    }

    [Serializable]
    public sealed class SpeciesObservationSave
    {
        public string ObservationId = "";
        public string SpeciesId = "";
        public byte Evidence;
        public string RegionId = "";
        public string CellId = "";
        public string DepthBandId = "";
        public int DayNumber;

        public static SpeciesObservationSave From(in SpeciesObservation observation) => new SpeciesObservationSave
        {
            ObservationId = observation.ObservationId,
            SpeciesId = observation.SpeciesId,
            Evidence = (byte)observation.Evidence,
            RegionId = observation.RegionId,
            CellId = observation.CellId,
            DepthBandId = observation.DepthBandId,
            DayNumber = observation.DayNumber
        };

        public SpeciesObservation ToObservation() => new SpeciesObservation(ObservationId, SpeciesId,
            (SpeciesEvidence)Evidence, RegionId, CellId, DepthBandId ?? string.Empty, DayNumber);
    }

    [Serializable]
    public sealed class ExplorationSaveData
    {
        public string RegionId = "";
        public List<ExplorationCellSave> DiscoveredCells = new List<ExplorationCellSave>();
        public List<SpeciesObservationSave> Observations = new List<SpeciesObservationSave>();
    }

    // Implemented next to the host's exploration authorities (Composition); composed by the campaign save
    // store into the SAME file as the day, the money and the boat.
    public interface IExplorationPersistence
    {
        ExplorationSaveData ExportExploration();
        bool RestoreExploration(ExplorationSaveData data);
    }

    // Where the host's read model is published for the UI layer (map fog, encyclopedia, client mirror).
    // The host shell that owns Utku's authorities binds it (Mehmet's composition binding, #89 follow-up);
    // unbound means "no exploration on this machine" and the UI draws no fog rather than inventing one.
    public static class ExplorationFeed
    {
        public static IExplorationReadModel Current { get; private set; }

        public static void Bind(IExplorationReadModel model) => Current = model;

        public static void Unbind(IExplorationReadModel model)
        {
            if (ReferenceEquals(Current, model)) Current = null;
        }
    }
}
