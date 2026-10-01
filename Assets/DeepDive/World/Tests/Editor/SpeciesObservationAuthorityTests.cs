using System;
using System.Collections.Generic;
using System.Reflection;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // Species observation: first verified evidence counts, the same ObservationId never counts twice
    // (retry, replay, reload, day close), ids come from the event (captureId / recordingId / host
    // sighting id) and never from a tick or position, and each observation carries the observer's
    // coarse cell and that cell's band - never a creature position.
    public class SpeciesObservationAuthorityTests
    {
        private const string SeaBass = "sea_bass";
        private const string Mullet = "red_mullet";

        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        private static readonly Vector3 Centre = new Vector3(1f, 4f, 1f); // cell gx0 gz0, shallow

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

        // Cells with the centre cell already explored (band known), the usual state when a fish is met.
        private static SpeciesObservationAuthority Explored(out ExplorationCellAuthority cells)
        {
            cells = Cells();
            cells.Tick(new FakeSource().Set(Centre));
            return new SpeciesObservationAuthority(cells);
        }

        private static string Id(int gx, int gz) => ExplorationIds.CellId(ExplorationIds.NearRegionId, gx, gz);

        private static CaptureResult Capture(string captureId, string speciesId) =>
            new CaptureResult(captureId, "dive-1", speciesId, 1200, 42UL);

        // ---- first observation / evidence ----

        [Test]
        public void FirstSighting_IsCounted_WithTheObserversCellAndBand()
        {
            var obs = Explored(out _);
            Assert.That(obs.AcceptSighting(SeaBass, Centre, 3), Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));

            Assert.That(obs.Observations.Count, Is.EqualTo(1));
            var o = obs.Observations[0];
            Assert.That(o.ObservationId, Is.EqualTo("sighting:sea_bass"));
            Assert.That(o.SpeciesId, Is.EqualTo(SeaBass));
            Assert.That(o.Evidence, Is.EqualTo(SpeciesEvidence.Sighted));
            Assert.That(o.RegionId, Is.EqualTo(ExplorationIds.NearRegionId));
            Assert.That(o.CellId, Is.EqualTo(Id(0, 0)));
            Assert.That(o.DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
            Assert.That(o.DayNumber, Is.EqualTo(3));

            Assert.That(obs.TryGetSpecies(SeaBass, out var s), Is.True);
            Assert.That((s.Sighted, s.Recorded, s.Caught), Is.EqualTo((true, false, false)));
            Assert.That(s.ObservedCellIds, Is.EqualTo(new[] { Id(0, 0) }));
            Assert.That(s.ObservedDepthBandIds, Is.EqualTo(new[] { DepthBandIds.Shallow }));
            Assert.That(s.ObservationIds, Is.EqualTo(new[] { "sighting:sea_bass" }));
        }

        [Test]
        public void SeeingTheSameSpeciesAgain_IsNotASecondObservation()
        {
            var obs = Explored(out _);
            obs.AcceptSighting(SeaBass, Centre, 1);
            var revision = obs.Revision;

            // Another fish of the species, elsewhere, another day, many ticks.
            for (var i = 0; i < 5; i++)
                Assert.That(obs.AcceptSighting(SeaBass, new Vector3(-12f, 3f, 9f), 2 + i),
                    Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));

            Assert.That(obs.Observations.Count, Is.EqualTo(1));
            Assert.That(obs.Revision, Is.EqualTo(revision));
            obs.TryGetSpecies(SeaBass, out var s);
            Assert.That(s.ObservedCellIds, Is.EqualTo(new[] { Id(0, 0) }), "no habitat from a non-observation");
        }

        [Test]
        public void Catch_UsesTheCaptureId_Recording_UsesTheRecordingId()
        {
            var obs = Explored(out _);
            Assert.That(obs.AcceptCatch(Capture("cap-7", SeaBass), Centre, 1), Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));
            Assert.That(obs.AcceptRecording("rec-3", SeaBass, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));

            Assert.That(obs.Observations[0].ObservationId, Is.EqualTo("cap-7"));
            Assert.That(obs.Observations[0].Evidence, Is.EqualTo(SpeciesEvidence.Caught));
            Assert.That(obs.Observations[1].ObservationId, Is.EqualTo("rec-3"));
            Assert.That(obs.Observations[1].Evidence, Is.EqualTo(SpeciesEvidence.Recorded));

            obs.TryGetSpecies(SeaBass, out var s);
            Assert.That((s.Sighted, s.Recorded, s.Caught), Is.EqualTo((false, true, true)));
            Assert.That(s.Has(SpeciesEvidence.Caught), Is.True);
        }

        [Test]
        public void SecondCatchOfTheSameSpecies_IsCounted_ButGivesNoNewEvidence()
        {
            var obs = Explored(out var cells);
            obs.AcceptCatch(Capture("cap-1", SeaBass), Centre, 1);
            cells.Tick(new FakeSource().Set(new Vector3(-12f, 3f, 9f)));

            Assert.That(obs.AcceptCatch(Capture("cap-2", SeaBass), new Vector3(-12f, 3f, 9f), 2),
                Is.EqualTo(SpeciesObservationOutcome.Counted));
            obs.TryGetSpecies(SeaBass, out var s);
            Assert.That(s.ObservationIds, Is.EqualTo(new[] { "cap-1", "cap-2" }));
            Assert.That(s.ObservedCellIds, Is.EqualTo(new[] { Id(0, 0), Id(-3, 1) }), "habitat grows");
            Assert.That(s.ObservedDepthBandIds, Is.EqualTo(new[] { DepthBandIds.Shallow }), "bands stay distinct");
        }

        // ---- idempotency / replay / reload ----

        [Test]
        public void SameEventProcessedAgain_IsCountedOnce()
        {
            var obs = Explored(out _);
            var capture = Capture("cap-1", SeaBass);
            obs.AcceptCatch(capture, Centre, 1);
            obs.AcceptRecording("rec-1", SeaBass, Centre, 1);
            var revision = obs.Revision;

            // A retry of the capture from somewhere else, a replay of the recording, the day close
            // re-reading both stored observations.
            Assert.That(obs.AcceptCatch(capture, new Vector3(-12f, 3f, 9f), 2), Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            Assert.That(obs.AcceptRecording("rec-1", SeaBass, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            foreach (var o in new List<SpeciesObservation>(obs.Observations))
                Assert.That(obs.TryApply(o), Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));

            Assert.That(obs.Observations.Count, Is.EqualTo(2));
            Assert.That(obs.Revision, Is.EqualTo(revision));
            obs.TryGetSpecies(SeaBass, out var s);
            Assert.That(s.ObservationIds, Is.EqualTo(new[] { "cap-1", "rec-1" }));
        }

        [Test]
        public void Reload_ReplayingStoredObservations_RebuildsTheSameState_AndKeepsTheIds()
        {
            var original = Explored(out _);
            original.AcceptSighting(SeaBass, Centre, 1);
            original.AcceptCatch(Capture("cap-1", SeaBass), Centre, 1);
            original.AcceptRecording("rec-1", Mullet, new Vector3(-12f, -5f, 9f), 2); // 13 m, band ""
            var stored = new List<SpeciesObservation>(original.Observations);

            // A fresh host after reload: cells not explored yet - stored cell/band must win, not a
            // re-derivation from today's state.
            var reloaded = new SpeciesObservationAuthority(Cells());
            foreach (var o in stored)
                Assert.That(reloaded.TryApply(o), Is.Not.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
            foreach (var o in stored)
                Assert.That(reloaded.TryApply(o), Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted), "second load");

            Assert.That(reloaded.Observations, Is.EqualTo(stored));
            AssertSameSpecies(original.Snapshot(), reloaded.Snapshot());

            // The live sighting after reload re-derives the SAME id, so it is recognised, not re-counted.
            Assert.That(reloaded.AcceptSighting(SeaBass, new Vector3(-12f, 3f, 9f), 9),
                Is.EqualTo(SpeciesObservationOutcome.AlreadyCounted));
        }

        [Test]
        public void SightingId_ComesFromTheSpecies_NotFromTickDayOrPosition()
        {
            var a = Explored(out _);
            var b = new SpeciesObservationAuthority(Cells());
            a.AcceptSighting(SeaBass, Centre, 1);
            b.AcceptSighting(SeaBass, new Vector3(-12f, 3f, 9f), 7);

            Assert.That(a.Observations[0].ObservationId, Is.EqualTo(b.Observations[0].ObservationId));
            Assert.That(a.Observations[0].ObservationId, Is.EqualTo(SpeciesObservationAuthority.SightingId(SeaBass)));
            Assert.That(SpeciesObservationAuthority.SightingId(Mullet), Is.Not.EqualTo(SpeciesObservationAuthority.SightingId(SeaBass)));
        }

        [Test]
        public void CatchId_IsTheCaptureId_WhereverAndWheneverItIsAccepted()
        {
            var a = Explored(out _);
            var b = new SpeciesObservationAuthority(Cells());
            a.AcceptCatch(Capture("cap-9", SeaBass), Centre, 1);
            b.AcceptCatch(Capture("cap-9", SeaBass), new Vector3(-12f, 3f, 9f), 4);
            Assert.That(a.Observations[0].ObservationId, Is.EqualTo("cap-9"));
            Assert.That(b.Observations[0].ObservationId, Is.EqualTo("cap-9"));
        }

        // ---- cell / band ----

        [Test]
        public void Observation_TakesTheCellsBand_AtAcceptance_AndKeepsIt()
        {
            var cells = Cells();
            var obs = new SpeciesObservationAuthority(cells);

            // The observer's cell has not been ticked yet: stable cell id, band Unclassified.
            obs.AcceptSighting(SeaBass, new Vector3(7f, 3f, -8f), 1);
            Assert.That(obs.Observations[0].CellId, Is.EqualTo(Id(1, -2)));
            Assert.That(obs.Observations[0].DepthBandId, Is.EqualTo(DepthBandIds.Unclassified));

            // The cell learns its band afterwards; the stored observation does not change.
            cells.Tick(new FakeSource().Set(new Vector3(7f, 3f, -8f)));
            Assert.That(obs.Observations[0].DepthBandId, Is.EqualTo(DepthBandIds.Unclassified));

            obs.AcceptCatch(Capture("cap-1", SeaBass), new Vector3(7f, 3f, -8f), 1);
            Assert.That(obs.Observations[1].DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
        }

        [Test]
        public void Observation_DeepCellInsideTheRegion_IsCounted_WithUnclassifiedBand()
        {
            // No band is not an invalid region: 13 m inside the region is a valid cell, so it counts.
            var cells = Cells();
            cells.Tick(new FakeSource().Set(new Vector3(0f, -5f, 0f))); // 13 m
            var obs = new SpeciesObservationAuthority(cells);
            Assert.That(obs.AcceptSighting(SeaBass, new Vector3(0f, -5f, 0f), 1),
                Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));
            Assert.That(obs.Observations[0].CellId, Is.EqualTo(Id(0, 0)));
            Assert.That(obs.Observations[0].DepthBandId, Is.EqualTo(DepthBandIds.Unclassified));
            obs.TryGetSpecies(SeaBass, out var s);
            Assert.That(s.ObservedDepthBandIds, Is.Empty, "unclassified is not a habitat band");
        }

        [Test]
        public void OutOfRegionObservationIsRejected()
        {
            var obs = Explored(out _);
            var outside = new Vector3(40f, 4f, 0f);
            Assert.That(obs.AcceptRecording("rec-1", Mullet, outside, 1), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.AcceptCatch(Capture("cap-1", Mullet), outside, 1), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.AcceptSighting(Mullet, new Vector3(0f, 4f, -15.5f), 1), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.AcceptSighting(Mullet, new Vector3(float.NaN, 4f, 0f), 1), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));

            Assert.That(obs.Observations, Is.Empty);
            Assert.That(obs.TryGetSpecies(Mullet, out _), Is.False);
            Assert.That(obs.IsCounted("rec-1"), Is.False, "a rejected event is not remembered as counted");

            // The same events, once the observer is inside, are counted normally.
            Assert.That(obs.AcceptRecording("rec-1", Mullet, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));
            Assert.That(obs.AcceptSighting(Mullet, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.CountedNewEvidence));
        }

        [Test]
        public void StoredObservation_WithNoCellOrAnotherRegion_IsRejectedOnReplay()
        {
            var obs = new SpeciesObservationAuthority(Cells());
            Assert.That(obs.TryApply(new SpeciesObservation("rec-1", Mullet, SpeciesEvidence.Recorded,
                ExplorationIds.NearRegionId, "", "", 1)), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.TryApply(new SpeciesObservation("rec-2", Mullet, SpeciesEvidence.Recorded,
                "region-far-9", Id(0, 0), "", 1)), Is.EqualTo(SpeciesObservationOutcome.OutOfRegion));
            Assert.That(obs.Observations, Is.Empty);
        }

        [Test]
        public void NoPositionIsEverPublished()
        {
            // The contract types an observation leaves through carry no vector and no float at all.
            foreach (var type in new[] { typeof(SpeciesObservation), typeof(SpeciesDiscoveryState), typeof(ExplorationCellState) })
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(Vector3)), $"{type.Name}.{field.Name}");
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(Vector2)), $"{type.Name}.{field.Name}");
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(float)), $"{type.Name}.{field.Name}");
            }
        }

        // ---- invalid input ----

        [Test]
        public void InvalidInput_IsRefused_AndChangesNothing()
        {
            var obs = Explored(out _);
            Assert.That(obs.AcceptSighting(null, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.Invalid));
            Assert.That(obs.AcceptSighting("  ", Centre, 1), Is.EqualTo(SpeciesObservationOutcome.Invalid));
            Assert.That(obs.AcceptCatch(Capture(null, SeaBass), Centre, 1), Is.EqualTo(SpeciesObservationOutcome.Invalid));
            Assert.That(obs.AcceptCatch(Capture("cap-1", ""), Centre, 1), Is.EqualTo(SpeciesObservationOutcome.Invalid));
            Assert.That(obs.AcceptRecording(" ", SeaBass, Centre, 1), Is.EqualTo(SpeciesObservationOutcome.Invalid));
            Assert.That(obs.TryApply(new SpeciesObservation("o-1", SeaBass, SpeciesEvidence.Caught, "r", "c", " ", 1)),
                Is.EqualTo(SpeciesObservationOutcome.Invalid), "whitespace band");
            Assert.That(obs.TryApply(new SpeciesObservation("o-1", SeaBass, (SpeciesEvidence)9, "r", "c", "", 1)),
                Is.EqualTo(SpeciesObservationOutcome.Invalid), "unknown evidence");
            Assert.That(obs.TryApply(default), Is.EqualTo(SpeciesObservationOutcome.Invalid));

            Assert.That(obs.Observations, Is.Empty);
            Assert.That(obs.Snapshot().Species, Is.Empty);
        }

        // ---- read model / flow ----

        [Test]
        public void ReadModel_CombinesCellsAndSpecies_RevisionRisesOnEither()
        {
            var cells = Cells();
            IExplorationReadModel read = new SpeciesObservationAuthority(cells);
            var obs = (SpeciesObservationAuthority)read;
            var r0 = read.Revision;

            cells.Tick(new FakeSource().Set(Centre));
            var r1 = read.Revision;
            Assert.That(r1, Is.GreaterThan(r0), "cell discovery");

            obs.AcceptSighting(Mullet, Centre, 1);
            obs.AcceptSighting(SeaBass, Centre, 1);
            Assert.That(read.Revision, Is.GreaterThan(r1), "observation");

            var s = read.Snapshot();
            Assert.That(s.Revision, Is.EqualTo(read.Revision));
            Assert.That(s.Cells.Count, Is.EqualTo(36));
            Assert.That(s.Species.Count, Is.EqualTo(2));
            Assert.That(s.Species[0].SpeciesId, Is.EqualTo(Mullet), "sorted by id, not by first seen");
            Assert.That(s.Species[1].SpeciesId, Is.EqualTo(SeaBass));
            Assert.That(read.Snapshot().Species, Is.SameAs(s.Species), "reused until the revision moves");
        }

        [Test]
        public void Flow_ExploreThenObserve_WithAFakeSource()
        {
            var cells = Cells();
            var obs = new SpeciesObservationAuthority(cells);
            var source = new FakeSource();
            var events = new List<string>();

            // Two divers swim; one meets a sea bass in the reef-ward cell, catches one later, the
            // other films a mullet. The day close then re-reads everything.
            for (var x = -14f; x <= 14f; x += 2f)
                cells.Tick(source.Set(new Vector3(x, 4f, -12f), new Vector3(-x, 2f, 12f)), events);

            var diverA = new Vector3(8f, 4f, -12f);
            var diverB = new Vector3(-8f, 2f, 12f);
            obs.AcceptSighting(SeaBass, diverA, 1);
            obs.AcceptSighting(SeaBass, diverA, 1); // next tick, same fish
            obs.AcceptRecording("rec-1", Mullet, diverB, 1);
            obs.AcceptCatch(Capture("cap-1", SeaBass), diverA, 1);
            foreach (var o in new List<SpeciesObservation>(obs.Observations)) obs.TryApply(o);

            Assert.That(events.Count, Is.EqualTo(12));
            Assert.That(obs.Observations.Count, Is.EqualTo(3));
            obs.TryGetSpecies(SeaBass, out var bass);
            Assert.That((bass.Sighted, bass.Recorded, bass.Caught), Is.EqualTo((true, false, true)));
            Assert.That(bass.ObservedCellIds, Is.EqualTo(new[] { Id(1, -3) }));
            obs.TryGetSpecies(Mullet, out var mullet);
            Assert.That(mullet.ObservedCellIds, Is.EqualTo(new[] { Id(-2, 2) }));
            Assert.That(mullet.ObservedDepthBandIds, Is.EqualTo(new[] { DepthBandIds.Shallow }));
        }

        [Test]
        public void RepeatedAndReplayedEvents_DoNotAllocate()
        {
            var obs = Explored(out _);
            var capture = Capture("cap-1", SeaBass);
            obs.AcceptSighting(SeaBass, Centre, 1);
            obs.AcceptCatch(capture, Centre, 1);
            var stored = obs.Observations[1];
            obs.Snapshot();

            // Warm up (JIT) every path measured below.
            obs.AcceptSighting(SeaBass, Centre, 1);
            obs.AcceptCatch(capture, Centre, 1);
            obs.TryApply(stored);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var repeats = 0;
            for (var i = 0; i < 10000; i++)
            {
                if (obs.AcceptSighting(SeaBass, Centre, 1) == SpeciesObservationOutcome.AlreadyCounted) repeats++;
                if (obs.AcceptCatch(capture, Centre, 1) == SpeciesObservationOutcome.AlreadyCounted) repeats++;
                if (obs.TryApply(stored) == SpeciesObservationOutcome.AlreadyCounted) repeats++;
                obs.Snapshot();
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes");
            Assert.That(repeats, Is.EqualTo(30000));
        }

        private static void AssertSameSpecies(ExplorationSnapshot a, ExplorationSnapshot b)
        {
            Assert.That(b.Species.Count, Is.EqualTo(a.Species.Count));
            for (var i = 0; i < a.Species.Count; i++)
            {
                var x = a.Species[i];
                var y = b.Species[i];
                Assert.That(y.SpeciesId, Is.EqualTo(x.SpeciesId));
                Assert.That((y.Sighted, y.Recorded, y.Caught), Is.EqualTo((x.Sighted, x.Recorded, x.Caught)));
                Assert.That(y.ObservedCellIds, Is.EqualTo(x.ObservedCellIds));
                Assert.That(y.ObservedDepthBandIds, Is.EqualTo(x.ObservedDepthBandIds));
                Assert.That(y.ObservationIds, Is.EqualTo(x.ObservationIds));
            }
        }
    }
}
