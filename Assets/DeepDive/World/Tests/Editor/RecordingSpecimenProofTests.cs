using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    // #254: a recording names the real animal it shows, and a catch only counts for that same animal, in that same
    // dive, if it came home. Rules only; the spawned chain is proved by the product day run.
    public class RecordingSpecimenProofTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        private const string Species = "grouper";
        private const string Dive = "dive-1";
        private const string SmallFish = "specimen-small";
        private const string BigFish = "specimen-big";
        private const int SmallGrams = 2100;
        private const int BigGrams = 5800;

        private sealed class FakeEconomy : IRecordingSink
        {
            public readonly List<RecordingResult> Claims = new List<RecordingResult>();
            public PlayerActionResult Result = PlayerActionResult.Accepted;

            public PlayerActionResult TryClaim(RecordingResult result)
            {
                Claims.Add(result);
                return Result;
            }
        }

        private static RecordingTake Take(PlayerId player, string specimen, int grams, int quality = 2,
            float score01 = 0.8f, string dive = Dive) =>
            new RecordingTake(dive, player, Species, quality, 5f, score01, specimen, grams);

        private static CaptureResult Catch(string captureId, string specimen, int grams, string dive = Dive) =>
            new CaptureResult(captureId, dive, Species, grams, 7, null, specimen);

        private static RecordingResult SettleOne(RecordingLedger ledger, params string[] preserved)
        {
            var economy = new FakeEconomy();
            ledger.Settle(economy, Dive, new[] { Alice, Bob }, preserved);
            Assert.AreEqual(1, economy.Claims.Count);
            return economy.Claims[0];
        }

        [Test]
        public void AnAnimalWithoutAnIdOrARealWeightProvesNothing()
        {
            Assert.AreEqual(SpecimenProofKind.None, default(RecordingSpecimenProof).Kind);
            Assert.AreEqual("", default(RecordingSpecimenProof).SpecimenId);
            Assert.AreEqual(SpecimenProofKind.None, RecordingSpecimenProof.Filmed("", SmallGrams).Kind);
            Assert.AreEqual(SpecimenProofKind.None, RecordingSpecimenProof.Filmed(SmallFish, 0).Kind);
            Assert.AreEqual(SpecimenProofKind.None, RecordingSpecimenProof.FilmedAndCaught("", 0, "capture").Kind);
            Assert.AreEqual("", RecordingSpecimenProof.FilmedAndCaught("", 0, "capture").CaptureId);
        }

        [Test]
        public void FilmWithoutACatchStaysFilmOnlyWithTheAnimalsOwnWeight()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams));

            var proof = SettleOne(ledger).Specimen;

            Assert.AreEqual(SpecimenProofKind.FilmOnly, proof.Kind);
            Assert.AreEqual(BigFish, proof.SpecimenId);
            Assert.AreEqual(BigGrams, proof.WeightGrams);
            Assert.AreEqual("", proof.CaptureId);
        }

        [Test]
        public void AGoodCloseUpOfASmallFishIsStillASmallFish()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams, quality: 1));
            ledger.Register(Take(Alice, SmallFish, SmallGrams, quality: 4, score01: 1f));

            var proof = SettleOne(ledger).Specimen;

            Assert.AreEqual(SmallFish, proof.SpecimenId);
            Assert.AreEqual(SmallGrams, proof.WeightGrams);
        }

        [Test]
        public void TheCatchOfTheFilmedAnimalProvesTheFilm_WhoeverShotIt()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams)); // nothing in the catch says who filmed

            var proof = SettleOne(ledger, "capture-big").Specimen;

            Assert.AreEqual(SpecimenProofKind.FilmAndCatch, proof.Kind);
            Assert.AreEqual("capture-big", proof.CaptureId);
            Assert.AreEqual(BigGrams, proof.WeightGrams);
        }

        [Test]
        public void TheOrderOfFilmAndCatchDoesNotMatter()
        {
            var ledger = new RecordingLedger();
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));
            ledger.Register(Take(Alice, BigFish, BigGrams));

            Assert.AreEqual(SpecimenProofKind.FilmAndCatch, SettleOne(ledger, "capture-big").Specimen.Kind);
        }

        [Test]
        public void ACatchOfAnotherFishOfTheSameSpeciesProvesNothing()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, SmallFish, SmallGrams));
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));

            var proof = SettleOne(ledger, "capture-big").Specimen;

            Assert.AreEqual(SpecimenProofKind.FilmOnly, proof.Kind);
            Assert.AreEqual(SmallGrams, proof.WeightGrams);
        }

        [Test]
        public void ACatchThatDidNotComeHomeProvesNothing()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));

            Assert.AreEqual(SpecimenProofKind.FilmOnly, SettleOne(ledger).Specimen.Kind);
        }

        [Test]
        public void ACatchFromAnotherDiveProvesNothing()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-old", BigFish, BigGrams, "dive-0"));

            Assert.AreEqual(SpecimenProofKind.FilmOnly, SettleOne(ledger, "capture-old").Specimen.Kind);
        }

        [Test]
        public void ACatchWithoutFilmIsReportedAndPaysNoRecording()
        {
            var ledger = new RecordingLedger();
            var economy = new FakeEconomy();
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-lost", SmallFish, SmallGrams));

            CollectionAssert.AreEqual(new[] { "capture-big" }, ledger.CatchesWithoutFilm(Dive, new[] { "capture-big" }));
            ledger.Settle(economy, Dive, new[] { Alice }, new[] { "capture-big" });
            Assert.IsEmpty(economy.Claims);
        }

        [Test]
        public void AFilmedCatchIsNotReportedAsUnfilmed()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));

            Assert.IsEmpty(ledger.CatchesWithoutFilm(Dive, new[] { "capture-big" }));
        }

        [Test]
        public void TheSameFishFilmedAgainProducesOneResult()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, BigFish, BigGrams, quality: 1));
            ledger.Register(Take(Alice, BigFish, BigGrams, quality: 3));
            ledger.Register(Take(Alice, BigFish, BigGrams, quality: 2));
            ledger.Register(Take(Bob, BigFish, BigGrams, quality: 2));

            var result = SettleOne(ledger);

            Assert.AreEqual(3, result.Quality);
            Assert.AreEqual(Alice, result.PlayerId);
        }

        [Test]
        public void ARefusedResultRetriesWithTheSameIdAndTheSameProof()
        {
            var ledger = new RecordingLedger();
            var economy = new FakeEconomy { Result = PlayerActionResult.InvalidState };
            ledger.Register(Take(Alice, BigFish, BigGrams));
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));

            ledger.Settle(economy, Dive, new[] { Alice }, new[] { "capture-big" });
            economy.Result = PlayerActionResult.Accepted;
            ledger.Settle(economy, Dive, new[] { Alice }, new[] { "capture-big" });

            Assert.AreEqual(2, economy.Claims.Count);
            Assert.AreEqual(economy.Claims[0].RecordingId, economy.Claims[1].RecordingId);
            Assert.AreEqual(SpecimenProofKind.FilmAndCatch, economy.Claims[1].Specimen.Kind);
        }

        [Test]
        public void AnEventRecordingNamesNoAnimal()
        {
            var ledger = new RecordingLedger();
            ledger.Register(new RecordingTake(Dive, Alice, "event_bioluminescence", 2, 5f, 0.8f));
            ledger.NoteCatch(new CaptureResult("capture-any", Dive, Species, BigGrams, 7));

            Assert.AreEqual(SpecimenProofKind.None, SettleOne(ledger, "capture-any").Specimen.Kind);
        }

        [Test]
        public void ResetForgetsTheCatches()
        {
            var ledger = new RecordingLedger();
            ledger.NoteCatch(Catch("capture-big", BigFish, BigGrams));
            ledger.Reset();
            ledger.Register(Take(Alice, BigFish, BigGrams));

            Assert.AreEqual(SpecimenProofKind.FilmOnly, SettleOne(ledger, "capture-big").Specimen.Kind);
        }

        [Test]
        public void TheCaptureCarriesItsAnimal()
        {
            var dive = new FakeDive();
            Assert.IsTrue(CaptureBuilder.TryCreate(dive, Species, BigGrams, 7, out var capture, BigFish));
            Assert.AreEqual(BigFish, capture.SpecimenId);

            Assert.IsTrue(CaptureBuilder.TryCreate(dive, Species, BigGrams, 7, out var older));
            Assert.AreEqual("", older.SpecimenId);
        }

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive => true;
            public string CurrentDiveId => Dive;
        }
    }
}
