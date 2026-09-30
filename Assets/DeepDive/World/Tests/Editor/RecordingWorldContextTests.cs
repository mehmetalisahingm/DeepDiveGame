using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // #101 reads: a counted species recording's context comes from its own stored observation (first
    // Recorded observation of the species = FirstRecordingOfSubject), and a place context (an event or an
    // uncounted species recording) comes from the observer's cell - never "first", never a position.
    public class RecordingWorldContextTests
    {
        private const string SeaBass = "sea_bass";
        private const string Mullet = "red_mullet";

        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        private static readonly Vector3 Centre = new Vector3(1f, 4f, 1f);   // cell gx0 gz0, shallow
        private static readonly Vector3 East = new Vector3(6f, 4f, 1f);     // cell gx1 gz0, shallow
        private static readonly Vector3 Outside = new Vector3(100f, 4f, 100f);

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

        // Centre and East explored (bands known), the usual state when a recording is made.
        private static SpeciesObservationAuthority Explored(out ExplorationCellAuthority cells)
        {
            cells = Cells();
            cells.Tick(new FakeSource().Set(Centre, East));
            return new SpeciesObservationAuthority(cells);
        }

        private static string Id(int gx, int gz) => ExplorationIds.CellId(ExplorationIds.NearRegionId, gx, gz);

        private static CaptureResult Capture(string captureId, string speciesId) =>
            new CaptureResult(captureId, "dive-1", speciesId, 1200, 42UL);

        private static void AssertContext(in RecordingWorldContext c, RecordingSubjectKind kind, string region,
            string cell, string band, bool first)
        {
            Assert.That(c.Kind, Is.EqualTo(kind));
            Assert.That(c.RegionId, Is.EqualTo(region));
            Assert.That(c.CellId, Is.EqualTo(cell));
            Assert.That(c.DepthBandId, Is.EqualTo(band));
            Assert.That(c.FirstRecordingOfSubject, Is.EqualTo(first));
        }

        private static void AssertEmpty(in RecordingWorldContext c) =>
            AssertContext(c, RecordingSubjectKind.Unknown, "", "", "", false);

        // ---- species recording ----

        [Test]
        public void FirstRecording_OfSpecies_IsFirst()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 2);

            Assert.That(obs.TryGetRecordingContext("rec-1", out var c), Is.True);
            AssertContext(c, RecordingSubjectKind.Species, ExplorationIds.NearRegionId, Id(0, 0),
                DepthBandIds.Shallow, true);
        }

        [Test]
        public void SecondRecording_OfSameSpecies_IsNotFirst()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 2);
            obs.AcceptRecording("rec-2", SeaBass, East, 2);

            Assert.That(obs.TryGetRecordingContext("rec-2", out var c), Is.True);
            AssertContext(c, RecordingSubjectKind.Species, ExplorationIds.NearRegionId, Id(1, 0),
                DepthBandIds.Shallow, false);
            Assert.That(obs.TryGetRecordingContext("rec-1", out var first), Is.True);
            Assert.That(first.FirstRecordingOfSubject, Is.True);
        }

        [Test]
        public void SightingAndCatchBetween_DoNotMoveFirst()
        {
            var obs = Explored(out _);
            obs.AcceptSighting(SeaBass, Centre, 1);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            obs.AcceptCatch(Capture("cap-1", SeaBass), Centre, 1);
            obs.AcceptRecording("rec-2", SeaBass, Centre, 1);

            Assert.That(obs.TryGetRecordingContext("rec-1", out var a), Is.True);
            Assert.That(obs.TryGetRecordingContext("rec-2", out var b), Is.True);
            Assert.That(a.FirstRecordingOfSubject, Is.True, "a sighting before it does not make it second");
            Assert.That(b.FirstRecordingOfSubject, Is.False, "a catch in between does not make it first");
        }

        [Test]
        public void OtherSpecies_HasItsOwnFirst()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            obs.AcceptRecording("rec-2", Mullet, Centre, 1);

            Assert.That(obs.TryGetRecordingContext("rec-2", out var c), Is.True);
            Assert.That(c.FirstRecordingOfSubject, Is.True);
        }

        [Test]
        public void Reload_ThroughTryApply_GivesSameContexts()
        {
            var obs = Explored(out _);
            obs.AcceptSighting(SeaBass, Centre, 1);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            obs.AcceptCatch(Capture("cap-1", SeaBass), East, 1);
            obs.AcceptRecording("rec-2", SeaBass, East, 2);

            var reloaded = new SpeciesObservationAuthority(Cells());
            for (var i = 0; i < obs.Observations.Count; i++) reloaded.TryApply(obs.Observations[i]);

            foreach (var id in new[] { "rec-1", "rec-2" })
            {
                Assert.That(obs.TryGetRecordingContext(id, out var before), Is.True);
                Assert.That(reloaded.TryGetRecordingContext(id, out var after), Is.True);
                AssertContext(after, before.Kind, before.RegionId, before.CellId, before.DepthBandId,
                    before.FirstRecordingOfSubject);
            }
        }

        [Test]
        public void Replay_SameRecordingId_GivesSameContext()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            obs.TryGetRecordingContext("rec-1", out var before);

            Assert.That(obs.AcceptRecording("rec-1", SeaBass, East, 3), Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            Assert.That(obs.TryGetRecordingContext("rec-1", out var after), Is.True);
            AssertContext(after, before.Kind, before.RegionId, before.CellId, before.DepthBandId, true);
        }

        [Test]
        public void Context_UsesObservationBand_NotCellsLaterBand()
        {
            var cells = Cells(); // nothing explored: the cell's band is still Unclassified
            var obs = new SpeciesObservationAuthority(cells);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);

            cells.Tick(new FakeSource().Set(Centre)); // the cell learns its band afterwards
            Assert.That(cells.TryGetCell(Centre, out _, out var bandNow), Is.True);
            Assert.That(bandNow, Is.EqualTo(DepthBandIds.Shallow));

            Assert.That(obs.TryGetRecordingContext("rec-1", out var c), Is.True);
            Assert.That(c.DepthBandId, Is.EqualTo(DepthBandIds.Unclassified));
        }

        [Test]
        public void UnknownOrBlankId_IsFalseAndEmpty()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);

            foreach (var id in new[] { null, "", " ", "rec-2", "REC-1" })
            {
                Assert.That(obs.TryGetRecordingContext(id, out var c), Is.False, id ?? "null");
                AssertEmpty(c);
            }
        }

        [Test]
        public void CatchOrSightingId_IsNotARecording()
        {
            var obs = Explored(out _);
            obs.AcceptSighting(SeaBass, Centre, 1);
            obs.AcceptCatch(Capture("cap-1", SeaBass), Centre, 1);

            Assert.That(obs.TryGetRecordingContext("cap-1", out var a), Is.False);
            AssertEmpty(a);
            Assert.That(obs.TryGetRecordingContext(SpeciesObservationAuthority.SightingId(SeaBass), out var b), Is.False);
            AssertEmpty(b);
        }

        [Test]
        public void OutOfRegionRecording_HasNoSpeciesContext()
        {
            var obs = Explored(out _);
            Assert.That(obs.AcceptRecording("rec-1", SeaBass, Outside, 1), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.TryGetRecordingContext("rec-1", out var c), Is.False);
            AssertEmpty(c);
        }

        [Test]
        public void RecordingContextRead_DoesNotAllocate()
        {
            var obs = Explored(out _);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            obs.AcceptRecording("rec-2", SeaBass, Centre, 1);
            obs.TryGetRecordingContext("rec-1", out _); // warm up (JIT)
            obs.TryGetRecordingContext("missing", out _);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var firsts = 0;
            for (var i = 0; i < 10000; i++)
            {
                if (obs.TryGetRecordingContext((i & 1) == 0 ? "rec-1" : "rec-2", out var c) && c.FirstRecordingOfSubject) firsts++;
                obs.TryGetRecordingContext("missing", out _);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes (firsts {firsts})");
        }

        // ---- place ----

        [Test]
        public void Place_EventInRegion_HasCellAndBand_NotFirst()
        {
            Explored(out var cells);
            Assert.That(cells.TryGetPlaceContext(RecordingSubjectKind.Event, East, out var c), Is.True);
            AssertContext(c, RecordingSubjectKind.Event, ExplorationIds.NearRegionId, Id(1, 0), DepthBandIds.Shallow, false);
        }

        [Test]
        public void Place_RejectedSpecies_KindSpecies_NotFirst()
        {
            Explored(out var cells);
            Assert.That(cells.TryGetPlaceContext(RecordingSubjectKind.Species, Centre, out var c), Is.True);
            AssertContext(c, RecordingSubjectKind.Species, ExplorationIds.NearRegionId, Id(0, 0), DepthBandIds.Shallow, false);
        }

        [Test]
        public void Place_OutOfRegion_IsFalseEmptyKeepsKind()
        {
            Explored(out var cells);
            Assert.That(cells.TryGetPlaceContext(RecordingSubjectKind.Event, Outside, out var a), Is.False);
            AssertContext(a, RecordingSubjectKind.Event, "", "", "", false);

            var nan = new Vector3(float.NaN, 4f, float.NaN);
            Assert.That(cells.TryGetPlaceContext(RecordingSubjectKind.Species, nan, out var b), Is.False);
            AssertContext(b, RecordingSubjectKind.Species, "", "", "", false);
        }
    }
}
