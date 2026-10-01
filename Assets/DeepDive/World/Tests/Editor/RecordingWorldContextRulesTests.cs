using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // #101 Process: every row of the rule table, acceptance and the first flag in one call, replay and
    // order independence (AlreadyCounted with the same context), and the same result after a restore.
    public class RecordingWorldContextRulesTests
    {
        private const string SeaBass = "sea_bass";
        private const string Plankton = "event_bioluminescence";

        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        private static readonly Vector3 Centre = new Vector3(1f, 4f, 1f);   // cell gx0 gz0, shallow
        private static readonly Vector3 East = new Vector3(6f, 4f, 1f);     // cell gx1 gz0, shallow
        private static readonly Vector3 Outside = new Vector3(100f, 4f, 100f);

        private const RecordingSubjectKind Species = RecordingSubjectKind.Species;
        private const RecordingSubjectKind Event = RecordingSubjectKind.Event;

        private sealed class FakeSource : IExplorerPositionSource
        {
            private readonly List<Vector3> next = new List<Vector3>(8);

            public void CollectPositions(List<ExplorerPosition> into)
            {
                into.Clear();
                for (var i = 0; i < next.Count; i++) into.Add(new ExplorerPosition(new PlayerId((ulong)i), next[i]));
            }

            public FakeSource Set(params Vector3[] worlds)
            {
                next.Clear();
                next.AddRange(worlds);
                return this;
            }
        }

        private static ExplorationCellAuthority Cells() =>
            new ExplorationCellAuthority(ExplorationIds.NearRegionId, Arena, Sea);

        private static SpeciesObservationAuthority Explored(out ExplorationCellAuthority cells)
        {
            cells = Cells();
            cells.Tick(new FakeSource().Set(Centre, East));
            return new SpeciesObservationAuthority(cells);
        }

        private static string Id(int gx, int gz) => ExplorationIds.CellId(ExplorationIds.NearRegionId, gx, gz);

        private static RecordingContextProcessResult Run(SpeciesObservationAuthority species,
            ExplorationCellAuthority cells, string recordingId, string subjectId, RecordingSubjectKind kind,
            Vector3 observer, bool observerKnown = true, int day = 1) =>
            RecordingWorldContextRules.Process(species, cells, recordingId, subjectId, kind, observerKnown, observer, day);

        private static void AssertContext(in RecordingWorldContext c, RecordingSubjectKind kind, string region,
            string cell, string band, bool first)
        {
            Assert.That(c.Kind, Is.EqualTo(kind));
            Assert.That(c.RegionId, Is.EqualTo(region));
            Assert.That(c.CellId, Is.EqualTo(cell));
            Assert.That(c.DepthBandId, Is.EqualTo(band));
            Assert.That(c.FirstRecordingOfSubject, Is.EqualTo(first));
        }

        private static void AssertSame(in RecordingWorldContext a, in RecordingWorldContext b) =>
            AssertContext(a, b.Kind, b.RegionId, b.CellId, b.DepthBandId, b.FirstRecordingOfSubject);

        // ---- rows 1-3: nothing to decide from, nothing accepted ----

        [Test]
        public void Process_UnknownKind_IsEmpty_NoAccept()
        {
            var obs = Explored(out var cells);
            foreach (var kind in new[] { RecordingSubjectKind.Unknown, (RecordingSubjectKind)7 })
            {
                var r = Run(obs, cells, "rec-1", SeaBass, kind, Centre);
                Assert.That(r.Outcome, Is.Null);
                Assert.That(r.ObservationAttempted, Is.False);
                AssertContext(r.Context, RecordingSubjectKind.Unknown, "", "", "", false);
            }
            Assert.That(obs.Observations.Count, Is.EqualTo(0));
        }

        [Test]
        public void Process_NullSpecies_EmptyKeepsKind_NoAccept()
        {
            Explored(out var cells);
            var r = Run(null, cells, "rec-1", SeaBass, Species, Centre);
            Assert.That(r.Outcome, Is.Null);
            AssertContext(r.Context, Species, "", "", "", false);
        }

        [Test]
        public void Process_NullCells_EmptyKeepsKind_NoAccept()
        {
            var obs = Explored(out _);
            var r = Run(obs, null, "rec-1", SeaBass, Event, Centre);
            Assert.That(r.Outcome, Is.Null);
            AssertContext(r.Context, Event, "", "", "", false);

            var s = Run(obs, null, "rec-2", SeaBass, Species, Centre);
            Assert.That(s.Outcome, Is.Null);
            AssertContext(s.Context, Species, "", "", "", false);
            Assert.That(obs.Observations.Count, Is.EqualTo(0));
        }

        [Test]
        public void Process_ObserverUnknown_EmptyKeepsKind_NoAccept()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-1", SeaBass, Species, Centre, observerKnown: false);
            Assert.That(r.Outcome, Is.Null);
            AssertContext(r.Context, Species, "", "", "", false);
            Assert.That(obs.Observations.Count, Is.EqualTo(0));
            Assert.That(obs.IsCounted("rec-1"), Is.False);
        }

        // ---- row 4: event ----

        [Test]
        public void Process_Event_DoesNotAccept_UsesPlace()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-e", Plankton, Event, East);
            Assert.That(r.Outcome, Is.Null, "an event is not a species observation");
            AssertContext(r.Context, Event, ExplorationIds.NearRegionId, Id(1, 0), DepthBandIds.Shallow, false);
            Assert.That(obs.Observations.Count, Is.EqualTo(0));
        }

        [Test]
        public void Process_EventOutOfRegion_KindKeptRestEmpty()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-e", Plankton, Event, Outside);
            Assert.That(r.Outcome, Is.Null);
            AssertContext(r.Context, Event, "", "", "", false);
        }

        // ---- row 5: counted species recording ----

        [Test]
        public void Process_Species_AcceptsAndGivesFirst_InOneCall()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-1", SeaBass, Species, Centre, day: 3);

            Assert.That(r.Outcome, Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));
            Assert.That(obs.IsCounted("rec-1"), Is.True, "Process itself accepted the recording");
            Assert.That(obs.Observations[0].Evidence, Is.EqualTo(SpeciesEvidence.Recorded));
            Assert.That(obs.Observations[0].DayNumber, Is.EqualTo(3));
            AssertContext(r.Context, Species, ExplorationIds.NearRegionId, Id(0, 0), DepthBandIds.Shallow, true);
        }

        [Test]
        public void Process_SecondRecordingOfSpecies_CountedNotFirst()
        {
            var obs = Explored(out var cells);
            Run(obs, cells, "rec-1", SeaBass, Species, Centre);
            var r = Run(obs, cells, "rec-2", SeaBass, Species, East);

            Assert.That(r.Outcome, Is.EqualTo(SpeciesObservationOutcome.Counted));
            AssertContext(r.Context, Species, ExplorationIds.NearRegionId, Id(1, 0), DepthBandIds.Shallow, false);
        }

        [Test]
        public void Process_SameRecordingIdTwice_AlreadyCounted_SameContext()
        {
            var obs = Explored(out var cells);
            var first = Run(obs, cells, "rec-1", SeaBass, Species, Centre);
            var again = Run(obs, cells, "rec-1", SeaBass, Species, Centre);

            Assert.That(again.Outcome, Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            AssertSame(again.Context, first.Context);
            Assert.That(again.Context.FirstRecordingOfSubject, Is.True);
            Assert.That(obs.Observations.Count, Is.EqualTo(1));
        }

        [Test]
        public void Process_Replay_IgnoresObserverMove()
        {
            var obs = Explored(out var cells);
            var first = Run(obs, cells, "rec-1", SeaBass, Species, Centre);
            var moved = Run(obs, cells, "rec-1", "red_mullet", Species, East, day: 5);

            Assert.That(moved.Outcome, Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            AssertSame(moved.Context, first.Context);
            Assert.That(moved.Context.CellId, Is.EqualTo(Id(0, 0)));
        }

        // ---- row 6: species recording not counted ----

        [Test]
        public void Process_SpeciesOutOfRegion_OutOfRegion_EmptyKeepsKind()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-1", SeaBass, Species, Outside);
            Assert.That(r.Outcome, Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            AssertContext(r.Context, Species, "", "", "", false);
        }

        [Test]
        public void Process_SpeciesInvalid_UsesPlace_KindSpecies()
        {
            var obs = Explored(out var cells);
            var r = Run(obs, cells, "rec-1", "", Species, East);
            Assert.That(r.Outcome, Is.EqualTo(SpeciesObservationOutcome.Invalid));
            AssertContext(r.Context, Species, ExplorationIds.NearRegionId, Id(1, 0), DepthBandIds.Shallow, false);
            Assert.That(obs.Observations.Count, Is.EqualTo(0));
        }

        // ---- order independence and restore ----

        [Test]
        public void Process_AfterPriorAcceptRecording_SameAsIfProcessAccepted()
        {
            var direct = Explored(out var directCells);
            direct.AcceptRecording("rec-1", SeaBass, Centre, 1); // e.g. another binding got there first
            var late = Run(direct, directCells, "rec-1", SeaBass, Species, Centre);

            var own = Explored(out var ownCells);
            var fresh = Run(own, ownCells, "rec-1", SeaBass, Species, Centre);

            Assert.That(late.Outcome, Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            AssertSame(late.Context, fresh.Context);
            Assert.That(late.Context.FirstRecordingOfSubject, Is.True);
        }

        [Test]
        public void Process_AfterRestore_GivesSameContext()
        {
            var obs = Explored(out var cells);
            var rec1 = Run(obs, cells, "rec-1", SeaBass, Species, Centre);
            var rec2 = Run(obs, cells, "rec-2", SeaBass, Species, East);
            var evt = Run(obs, cells, "rec-e", Plankton, Event, East);
            var rejected = Run(obs, cells, "rec-x", "", Species, Centre);

            // Restore the way the save does: discovered cells through TryRestore, observations through TryApply.
            var restoredCells = Cells();
            var snapshot = cells.Snapshot();
            for (var i = 0; i < snapshot.Cells.Count; i++)
            {
                var cell = snapshot.Cells[i];
                if (cell.Discovered) restoredCells.TryRestore(cell.CellId, cell.GridX, cell.GridZ, cell.DepthBandId);
            }
            var restored = new SpeciesObservationAuthority(restoredCells);
            for (var i = 0; i < obs.Observations.Count; i++) restored.TryApply(obs.Observations[i]);

            var again1 = Run(restored, restoredCells, "rec-1", SeaBass, Species, Centre);
            var again2 = Run(restored, restoredCells, "rec-2", SeaBass, Species, East);
            Assert.That(again1.Outcome, Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            Assert.That(again2.Outcome, Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            AssertSame(again1.Context, rec1.Context);
            AssertSame(again2.Context, rec2.Context);
            Assert.That(again1.Context.FirstRecordingOfSubject, Is.True);
            Assert.That(again2.Context.FirstRecordingOfSubject, Is.False);

            AssertSame(Run(restored, restoredCells, "rec-e", Plankton, Event, East).Context, evt.Context);
            AssertSame(Run(restored, restoredCells, "rec-x", "", Species, Centre).Context, rejected.Context);
        }
    }
}
