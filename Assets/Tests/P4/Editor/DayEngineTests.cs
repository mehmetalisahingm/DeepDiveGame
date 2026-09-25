using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #90 P4.1-C acceptance, one test per line of the issue's "Bitis kaniti" that a pure engine can
    // prove: solo sleep closes and the next day starts right, 2/4 players count only active ones, a
    // dropped/passive player never blocks, 00:00 closes once, the summary is produced once, a
    // failed write retries the SAME close, and a reload cannot advance the day twice.
    public class DayEngineTests
    {
        private static readonly PlayerId P0 = new PlayerId(0), P1 = new PlayerId(1), P2 = new PlayerId(2), P3 = new PlayerId(3);

        private sealed class Roster : IDayRoster
        {
            public readonly List<PlayerId> Active = new List<PlayerId>();
            public IReadOnlyList<PlayerId> ActivePlayers() => Active;
        }

        private sealed class Hooks : IDayCloseHooks
        {
            public int Settled, DivesClosed, Persisted, Mornings;
            public bool PersistOk = true;
            public DayDiveClosure Dives = DayDiveClosure.None;
            public DayEngine Engine;
            public readonly List<string> Order = new List<string>();
            public readonly List<DaySaveData> Written = new List<DaySaveData>();

            public void SettleAcceptedActions(string closeId) { Settled++; Order.Add("settle"); }
            public DayDiveClosure CloseOpenDives(string closeId) { DivesClosed++; Order.Add("dives"); return Dives; }
            public bool Persist()
            {
                Persisted++;
                Order.Add("persist");
                if (!PersistOk) return false;
                Written.Add(Engine.ExportDay());
                return true;
            }
            public void BeginMorning(int dayNumber) { Mornings++; Order.Add("morning"); }
        }

        private static (DayEngine engine, Roster roster, Hooks hooks) Make(params PlayerId[] active)
        {
            var roster = new Roster();
            roster.Active.AddRange(active);
            var hooks = new Hooks();
            var engine = new DayEngine();
            hooks.Engine = engine;
            engine.Configure(roster, hooks, 7);
            return (engine, roster, hooks);
        }

        [Test]
        public void SoloSleepClosesTheDayAndTomorrowStartsAtEight()
        {
            var (engine, _, hooks) = Make(P0);
            engine.Tick(60f);   // some day passes first
            Assert.Greater(engine.ClockMinute, DayIds.DayStartMinute);

            Assert.IsTrue(engine.TryEnterBed(P0, DayIds.Bed0, 1).Accepted);

            Assert.AreEqual(2, engine.DayNumber);
            Assert.AreEqual(DayPhase.Morning, engine.Phase);
            Assert.AreEqual(DayIds.DayStartMinute, engine.ClockMinute);
            Assert.AreEqual(0, engine.State.SleepingPlayers.Count, "beds are empty in the morning");
            Assert.AreEqual(DayCloseReason.EarlySleep, engine.LastSummary.Reason);
            Assert.AreEqual(1, hooks.Mornings);

            engine.CompleteMorning();
            Assert.AreEqual(DayPhase.Running, engine.Phase);
        }

        [Test]
        public void CloseRunsInTheFixedOrder()
        {
            var (engine, _, hooks) = Make(P0);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            CollectionAssert.AreEqual(new[] { "settle", "dives", "persist", "morning" }, hooks.Order);
        }

        [Test]
        public void TwoPlayersCloseOnlyWhenBothActivePlayersAreAsleep()
        {
            var (engine, _, _) = Make(P0, P1);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            Assert.AreEqual(1, engine.DayNumber);
            Assert.AreEqual(DayPhase.Running, engine.Phase, "one of two asleep is not ready");

            engine.TryEnterBed(P1, DayIds.Bed1, 1);
            Assert.AreEqual(2, engine.DayNumber);
        }

        [Test]
        public void FourPlayersNeedAllFour()
        {
            var (engine, _, _) = Make(P0, P1, P2, P3);
            for (var i = 0; i < 3; i++) engine.TryEnterBed(new PlayerId((ulong)i), DayIds.Beds[i], 1);
            Assert.AreEqual(1, engine.DayNumber);
            engine.TryEnterBed(P3, DayIds.Bed3, 1);
            Assert.AreEqual(2, engine.DayNumber);
        }

        [Test]
        public void AnAwakePlayerWhoDisconnectsOrGoesPassiveStopsBlockingTheGate()
        {
            var (engine, roster, _) = Make(P0, P1, P2);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            engine.TryEnterBed(P1, DayIds.Bed1, 1);
            Assert.AreEqual(1, engine.DayNumber, "P2 is awake and active");

            roster.Active.Remove(P2);   // dropped, or passive after running out of oxygen
            engine.NotifyRosterChanged();

            Assert.AreEqual(2, engine.DayNumber);
        }

        [Test]
        public void ASleeperWhoDisconnectsFreesTheBedAndNoLongerCounts()
        {
            var (engine, roster, _) = Make(P0, P1);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            roster.Active.Remove(P0);
            engine.NotifyRosterChanged();

            Assert.AreEqual(0, engine.State.SleepingPlayers.Count);
            Assert.AreEqual(1, engine.DayNumber, "P1 is still awake");
            Assert.IsTrue(engine.TryEnterBed(P1, DayIds.Bed0, 1).Accepted, "P0's bed is free again");
        }

        [Test]
        public void ThereIsNoDayForAnEmptyRoom()
        {
            var (engine, roster, _) = Make(P0);
            roster.Active.Clear();
            engine.NotifyRosterChanged();
            engine.Tick(100000f);

            Assert.AreEqual(1, engine.DayNumber);
            Assert.AreEqual(DayIds.DayStartMinute, engine.ClockMinute, "the clock does not run with nobody connected");
            Assert.AreEqual(DayPhase.Running, engine.Phase);
        }

        [Test]
        public void PlayersWhoAreNotActiveCannotUseABed()
        {
            var (engine, _, _) = Make(P0);
            var result = engine.TryEnterBed(P1, DayIds.Bed1, 1);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("NotActive", result.ReasonCode);
        }

        [Test]
        public void ABedHoldsOnePlayerAndAPlayerHoldsOneBed()
        {
            var (engine, _, _) = Make(P0, P1);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            Assert.AreEqual("BedTaken", engine.TryEnterBed(P1, DayIds.Bed0, 1).ReasonCode);
            Assert.AreEqual("AlreadyInBed", engine.TryEnterBed(P0, DayIds.Bed2, 2).ReasonCode);
            Assert.AreEqual("InvalidTarget", engine.TryEnterBed(P1, "bed-9", 2).ReasonCode);
        }

        [Test]
        public void LeavingTheBedBeforeTheCloseCancelsReadiness()
        {
            var (engine, _, _) = Make(P0, P1);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            Assert.IsTrue(engine.TryLeaveBed(P0, 2).Accepted);
            engine.TryEnterBed(P1, DayIds.Bed1, 1);
            Assert.AreEqual(1, engine.DayNumber, "P0 got up, so the room is not all asleep");
        }

        [Test]
        public void MidnightClosesWithoutAnySleepAndWarnsAtTenAndEleven()
        {
            var (engine, _, hooks) = Make(P0, P1);
            var warnings = new List<int>();
            engine.OnWarning += warnings.Add;

            engine.GameMinutesPerRealSecond = 1f;
            engine.Tick(DayIds.FirstWarningMinute - DayIds.DayStartMinute + 1);
            engine.Tick(DayIds.SecondWarningMinute - DayIds.FirstWarningMinute);
            CollectionAssert.AreEqual(new[] { DayIds.FirstWarningMinute, DayIds.SecondWarningMinute }, warnings);
            engine.Tick(0.5f);
            Assert.AreEqual(1, engine.DayNumber);

            engine.Tick(120f);
            Assert.AreEqual(2, engine.DayNumber);
            Assert.AreEqual(DayCloseReason.Midnight, engine.LastSummary.Reason);
            Assert.AreEqual(1, hooks.Persisted);
        }

        [Test]
        public void MidnightRunsOnceEvenIfTimeKeepsBeingFed()
        {
            var (engine, _, hooks) = Make(P0);
            engine.GameMinutesPerRealSecond = 1000f;
            engine.Tick(10f);
            engine.CompleteMorning();
            Assert.AreEqual(2, engine.DayNumber);
            Assert.AreEqual(1, hooks.Persisted);
            Assert.AreEqual(1, engine.History.Count);
        }

        [Test]
        public void ANewCloseCannotStartWhileOneIsAlreadyRunningOrDone()
        {
            var (engine, _, hooks) = Make(P0);
            hooks.PersistOk = false;                 // stays in Summary
            engine.BeginClose(DayCloseReason.Midnight);
            var again = engine.BeginClose(DayCloseReason.Midnight);

            Assert.AreEqual(DayIds.CloseId(1), again);
            Assert.AreEqual(1, hooks.Settled);
            Assert.AreEqual(1, hooks.DivesClosed, "dives are closed exactly once");
        }

        [Test]
        public void ARequestNamingAnEarlierDaysCloseNeverClosesTheDayThatIsRunningNow()
        {
            var (engine, _, hooks) = Make(P0);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            engine.CompleteMorning();
            Assert.AreEqual(2, engine.DayNumber);

            var returned = engine.BeginClose(DayCloseReason.Midnight, DayIds.CloseId(1));

            Assert.AreEqual(DayIds.CloseId(1), returned);
            Assert.AreEqual(2, engine.DayNumber, "day 2 is untouched");
            Assert.AreEqual(DayPhase.Running, engine.Phase);
            Assert.AreEqual(1, hooks.Persisted);
            Assert.AreEqual(1, engine.History.Count);

            Assert.AreEqual(DayIds.CloseId(2), engine.BeginClose(DayCloseReason.Midnight, DayIds.CloseId(2)));
            Assert.AreEqual(3, engine.DayNumber, "the matching close id does close today");
        }

        [Test]
        public void ClosingLocksNewTradeAndBeds()
        {
            var (engine, _, hooks) = Make(P0, P1);
            hooks.PersistOk = false;
            engine.BeginClose(DayCloseReason.Midnight);

            Assert.IsTrue(engine.IsLocked);
            Assert.AreEqual("DayClosing", engine.TryEnterBed(P0, DayIds.Bed0, 5).ReasonCode);
            Assert.AreEqual("DayClosing", engine.TryLeaveBed(P0, 6).ReasonCode);
        }

        [Test]
        public void ADayLockedByTheEngineIsVisibleThroughDayLock()
        {
            var manager = new GameObject("day").AddComponent<DayManager>();
            try
            {
                var roster = new Roster();
                roster.Active.Add(P0);
                var hooks = new Hooks { Engine = manager.Engine };
                manager.Configure(roster, hooks);
                Assert.IsFalse(DayLock.IsLocked);

                hooks.PersistOk = false;
                manager.Engine.BeginClose(DayCloseReason.Midnight);
                Assert.IsTrue(DayLock.IsLocked);

                hooks.PersistOk = true;
                Assert.IsTrue(manager.Engine.RetryClose());
                manager.Engine.CompleteMorning();
                Assert.IsFalse(DayLock.IsLocked);
            }
            finally
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Assert.IsFalse(DayLock.IsLocked, "shutdown unbinds the lock");
        }

        [Test]
        public void TheSummaryIsBuiltOnceFromTheLedgerAndTheDiveClosure()
        {
            var (engine, _, hooks) = Make(P0);
            hooks.Dives = new DayDiveClosure(1, new[] { "cap-lost-1" });
            engine.RecordSale("sale-1", 3, 4500, 360);
            engine.RecordExpense("exp-1", 100);
            engine.RecordDiscovery("sea_bass");
            engine.RecordProgress("boat-repaired");
            engine.TryEnterBed(P0, DayIds.Bed0, 1);

            var summary = engine.LastSummary;
            Assert.AreEqual(1, summary.DayNumber);
            Assert.AreEqual(3, summary.FishSold);
            Assert.AreEqual(4500, summary.FishSoldWeightGrams);
            Assert.AreEqual(360, summary.Income);
            Assert.AreEqual(100, summary.Expenses);
            Assert.AreEqual(260, summary.Net);
            CollectionAssert.AreEqual(new[] { "sea_bass" }, summary.DiscoveredSpeciesIds);
            Assert.AreEqual(1, summary.LostDivers);
            CollectionAssert.AreEqual(new[] { "cap-lost-1" }, summary.LostCaptureIds);
            CollectionAssert.AreEqual(new[] { "boat-repaired" }, summary.ProgressIds);
        }

        [Test]
        public void LedgerEventsAreIdempotentAndTheLedgerResetsEachDay()
        {
            var (engine, _, _) = Make(P0);
            Assert.IsTrue(engine.RecordSale("s", 1, 100, 50));
            Assert.IsFalse(engine.RecordSale("s", 1, 100, 50), "a replayed sale is not counted twice");
            Assert.IsFalse(engine.RecordSale("", 1, 1, 1));
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            engine.CompleteMorning();
            engine.TryEnterBed(P0, DayIds.Bed0, 2);

            Assert.AreEqual(50, engine.History[0].Income);
            Assert.AreEqual(0, engine.History[1].Income, "day 2 starts with an empty ledger");
        }

        [Test]
        public void ReplayingTheSameRequestNeitherClosesTwiceNorChangesTheAnswer()
        {
            var (engine, _, hooks) = Make(P0);
            var first = engine.TryEnterBed(P0, DayIds.Bed0, 42);
            engine.CompleteMorning();
            var replay = engine.TryEnterBed(P0, DayIds.Bed0, 42);

            Assert.AreEqual(first.Accepted, replay.Accepted);
            Assert.AreEqual(2, engine.DayNumber);
            Assert.AreEqual(1, hooks.Persisted);
            Assert.AreEqual(1, engine.History.Count);
            Assert.AreEqual(0, engine.State.SleepingPlayers.Count, "the replay did not put anyone back in bed");
        }

        [Test]
        public void AFailedWriteDoesNotOpenTomorrowAndRetriesTheSameClose()
        {
            var (engine, _, hooks) = Make(P0);
            hooks.PersistOk = false;
            engine.RecordSale("s", 2, 2000, 240);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);

            Assert.AreEqual(1, engine.DayNumber, "not advanced: the write failed");
            Assert.AreEqual(DayPhase.Summary, engine.Phase);
            Assert.IsTrue(engine.HasPendingClose);
            Assert.AreEqual(0, engine.History.Count);

            Assert.IsFalse(engine.RetryClose(), "still failing");
            hooks.PersistOk = true;
            Assert.IsTrue(engine.RetryClose());

            Assert.AreEqual(2, engine.DayNumber);
            Assert.AreEqual(1, hooks.Settled, "steps 1-3 ran once, not per retry");
            Assert.AreEqual(1, hooks.DivesClosed);
            Assert.AreEqual(1, engine.History.Count);
            Assert.AreEqual(240, engine.History[0].Income);
            Assert.AreEqual(DayIds.CloseId(1), engine.History[0].CloseId);
        }

        [Test]
        public void TheAtomicWriteAlreadyNamesTheNextDayTheSummaryAndTheCloseId()
        {
            var (engine, _, hooks) = Make(P0);
            engine.RecordSale("s", 1, 100, 50);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);

            var written = hooks.Written[0];
            Assert.AreEqual(2, written.DayNumber);
            Assert.AreEqual(DayIds.DayStartMinute, written.ClockMinute);
            CollectionAssert.Contains(written.ClosedCloseIds, DayIds.CloseId(1));
            Assert.AreEqual(1, written.SummaryHistory.Count);
            Assert.AreEqual(0, written.Ledger.Income, "the new day's ledger starts empty");
        }

        [Test]
        public void ReloadingAfterTheCloseDoesNotAdvanceTheDayAgain()
        {
            var (engine, _, hooks) = Make(P0);
            engine.TryEnterBed(P0, DayIds.Bed0, 1);
            var saved = hooks.Written[0];

            var (fresh, _, freshHooks) = Make(P0);
            Assert.IsTrue(fresh.RestoreDay(JsonRoundTrip(saved)));

            Assert.AreEqual(2, fresh.DayNumber);
            Assert.AreEqual(DayPhase.Running, fresh.Phase);
            Assert.AreEqual(DayIds.DayStartMinute, fresh.ClockMinute);
            Assert.AreEqual(1, fresh.History.Count);
            Assert.AreEqual(0, freshHooks.Persisted);
            Assert.AreEqual(DayIds.CloseId(2), fresh.BeginClose(DayCloseReason.Midnight),
                "the reloaded day 2 closes as close-day-2, never re-closing close-day-1");
            Assert.AreEqual(1, freshHooks.Persisted);
            Assert.AreEqual(3, fresh.DayNumber);
        }

        [Test]
        public void ReloadingBeforeTheWriteReturnsToTheLastCheckpointNotAHalfClosedDay()
        {
            var (engine, _, hooks) = Make(P0);
            engine.Tick(30f);
            var checkpoint = JsonRoundTrip(engine.ExportDay());
            hooks.PersistOk = false;
            engine.TryEnterBed(P0, DayIds.Bed0, 1);   // close started, write failed, then the host dies

            var (fresh, _, _) = Make(P0);
            fresh.RestoreDay(checkpoint);
            Assert.AreEqual(1, fresh.DayNumber);
            Assert.AreEqual(DayPhase.Running, fresh.Phase);
            Assert.AreEqual(0, fresh.History.Count);
        }

        [Test]
        public void ClockAndLedgerSurviveAMidDayReload()
        {
            var (engine, _, _) = Make(P0);
            engine.Tick(120f);
            engine.RecordSale("s", 2, 900, 240);
            engine.RecordDiscovery("sea_bass");
            var minute = engine.ClockMinute;
            var saved = JsonRoundTrip(engine.ExportDay());

            var (fresh, _, _) = Make(P0);
            Assert.IsTrue(fresh.RestoreDay(saved));
            Assert.AreEqual(minute, fresh.ClockMinute);
            Assert.IsFalse(fresh.RecordSale("s", 2, 900, 240), "the restored ledger still remembers the sale id: nothing is counted twice");
            fresh.TryEnterBed(P0, DayIds.Bed0, 1);
            Assert.AreEqual(240, fresh.LastSummary.Income);
            CollectionAssert.AreEqual(new[] { "sea_bass" }, fresh.LastSummary.DiscoveredSpeciesIds);
        }

        [Test]
        public void ARestoredDayDoesNotReAnnounceWarningsAlreadyPassed()
        {
            var (engine, _, _) = Make(P0);
            var data = new DaySaveData { DayNumber = 4, ClockMinute = DayIds.SecondWarningMinute + 5 };
            engine.RestoreDay(data);
            var warnings = 0;
            engine.OnWarning += _ => warnings++;
            engine.GameMinutesPerRealSecond = 1f;
            engine.Tick(1f);
            Assert.AreEqual(0, warnings);
        }

        [Test]
        public void InvalidSavedDaysAreRefused()
        {
            var (engine, _, _) = Make(P0);
            Assert.IsFalse(engine.RestoreDay(null));
            Assert.IsFalse(engine.RestoreDay(new DaySaveData { DayNumber = 0 }));
            Assert.IsFalse(engine.RestoreDay(new DaySaveData { ClockMinute = DayIds.DayEndMinute }));
            Assert.IsFalse(engine.RestoreDay(new DaySaveData { ClockMinute = 10 }));
        }

        [Test]
        public void WeatherSeedIsStablePerCampaignAndDayAndDiffersAcrossDays()
        {
            var (a, _, _) = Make(P0);
            var (b, _, _) = Make(P0);
            Assert.AreEqual(a.WeatherSeed, b.WeatherSeed);
            var day1 = a.WeatherSeed;
            a.TryEnterBed(P0, DayIds.Bed0, 1);
            Assert.AreNotEqual(day1, a.WeatherSeed);
        }

        [Test]
        public void SummaryHistoryIsBounded()
        {
            var (engine, _, _) = Make(P0);
            for (var i = 0; i < DayEngine.MaxSummaryHistory + 5; i++)
            {
                engine.TryEnterBed(P0, DayIds.Bed0, (ulong)i + 1);
                engine.CompleteMorning();
            }
            Assert.AreEqual(DayEngine.MaxSummaryHistory, engine.History.Count);
            Assert.AreEqual(DayEngine.MaxSummaryHistory, engine.ExportDay().SummaryHistory.Count);
        }

        private static DaySaveData JsonRoundTrip(DaySaveData data) =>
            JsonUtility.FromJson<DaySaveData>(JsonUtility.ToJson(data));
    }
}
