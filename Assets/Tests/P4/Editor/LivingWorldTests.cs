using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Living;
using DeepDive.Media;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // P4.5-C (#132): daily orders/sponsors, the visible home/town development and the light roles, on the REAL EconomyManager, the real campaign
    // save file and the real channel authority. The world seam (night / unrecorded species) is a test double only because Utku's data (#131)
    // does not exist yet.
    public sealed class LivingWorldTests
    {
        private static readonly PlayerId Host = new PlayerId(0), Guest = new PlayerId(1), Guest2 = new PlayerId(2);

        private sealed class Money : ILivingMoney
        {
            private readonly EconomyManager economy;
            public Money(EconomyManager economy) => this.economy = economy;
            public int Balance => economy.SharedBalance;
            public bool TryCredit(string rewardId, int amount) => economy.TryCreditReward(rewardId, amount);
            public void ReleaseCredit(string rewardId, int amount) => economy.ReleaseReward(rewardId, amount);
            public bool TrySpend(int amount) => economy.TrySpend(amount);
            public void RefundSpend(int amount) => economy.RefundSpend(amount);
            public void AnnounceSpend(string spendId, int amount) => economy.AnnounceSpend(spendId, amount);
        }

        private sealed class FakeWorld : IOrderWorld
        {
            public bool Night, Unrecorded = true;
            public bool NightCaptureAvailable => Night;
            public bool UnrecordedSpeciesRemain => Unrecorded;
        }

        private sealed class Rights : ChannelAuthority.IRights
        {
            public bool TryClaimForChannel(string recordingId) => true;
            public void ReleaseChannelClaim(string recordingId) { }
            public bool CreditChannelIncome(string settleId, int amount) => true;
        }

        private GameObject root;
        private EconomyManager economy;
        private EconomySaveStore store;
        private LivingWorldAuthority living;
        private readonly FakeWorld world = new FakeWorld();
        private string path;
        private ulong request = 1;
        private int dealCounter;
        private int boardSeed = 7;

        [SetUp]
        public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P45-living", Guid.NewGuid().ToString("N") + ".json");
            root = new GameObject("living");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
            Assert.IsTrue(economy.TryRestore(new EconomySaveData { SharedBalance = 0 }));
            economy.SetPersistenceHandler(store.SaveNow);
            living = new LivingWorldAuthority(new Money(economy), store.SaveNow);
            store.Living = living;
            world.Night = false; world.Unrecorded = true;   // one fixture instance serves every test
            OrderWorld.Bind(world);
        }

        [TearDown]
        public void Cleanup()
        {
            OrderWorld.Unbind(world);
            DevelopmentEffects.Unbind(null);
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private void Fund(int amount)
        {
            var data = economy.ExportSaveData("c", "c");
            data.SharedBalance = amount;
            Assert.IsTrue(economy.TryRestore(data));
        }

        private static IReadOnlyList<SoldCatch> Catches(params (string species, int grams)[] items)
        {
            var list = new List<SoldCatch>();
            foreach (var i in items) list.Add(new SoldCatch(i.species, i.grams));
            return list;
        }

        private string Deal() => "deal-" + (++dealCounter);

        private void ForceBoard(string orderId, string sponsorId)
        {
            // Search (seed, day) until the wanted pair appears: the pick is deterministic, so this is stable.
            for (var seed = 1; seed < 40; seed++)
                for (var day = 1; day < 60; day++)
                {
                    var fresh = new LivingWorldAuthority(new Money(economy));
                    fresh.EnsureDay(day, seed);
                    if (fresh.Board.Order.TemplateId == orderId && fresh.Board.Sponsor.TemplateId == sponsorId)
                    {
                        boardSeed = seed;
                        Assert.IsTrue(living.EnsureDay(day, seed));
                        return;
                    }
                }
            Assert.Fail("no day produced " + orderId + " + " + sponsorId);
        }

        private static ClipManifest Clip(int quality, bool first, RecordingSubjectKind kind = RecordingSubjectKind.Species, string id = "clip-a") =>
            new ClipManifest(id, "rec-" + id, "dive-1", 1, Host, kind == RecordingSubjectKind.Event ? "event_bioluminescence" : "sea_bass", quality, 12f,
                new string('a', 64), 1000, true, true, new RecordingWorldContext(kind, "region-near-1", "region-near-1:gx0:gz0", DepthBandIds.Shallow, first));

        // ---- the daily board ------------------------------------------------------------------------------------------

        [Test]
        public void EachDayHasOneFishOrderAndOneSponsorPickedDeterministicallyAndNeverReRolled()
        {
            Assert.IsTrue(living.EnsureDay(1, 42));
            var board = living.Board;
            Assert.AreEqual(1, board.Day);
            Assert.IsTrue(board.Order.HasContract);
            Assert.IsTrue(board.Sponsor.HasContract);
            Assert.AreEqual(ContractStatus.Active, board.Order.Status);
            Assert.AreEqual(ContractKind.FishOrder, Kind(board.Order));
            Assert.AreEqual(ContractKind.VideoSponsor, Kind(board.Sponsor));

            Assert.IsFalse(living.EnsureDay(1, 42), "the same day is never rolled twice");
            Assert.IsFalse(living.EnsureDay(1, 999), "not even with a different seed");
            Assert.AreEqual(board.Order.TemplateId, living.Board.Order.TemplateId);

            var other = new LivingWorldAuthority(new Money(economy));
            other.EnsureDay(1, 42);
            Assert.AreEqual(board.Order.TemplateId, other.Board.Order.TemplateId, "(day, seed) decide the pick");
            Assert.AreEqual(board.Sponsor.TemplateId, other.Board.Sponsor.TemplateId);
        }

        private static ContractKind Kind(in ContractState state)
        {
            ContractCatalog.TryGet(state.TemplateId, out var t);
            return t.Kind;
        }

        [Test]
        public void ANewDayExpiresTheOldContractsAndNeverRepeatsYesterdaysTemplates()
        {
            living.EnsureDay(1, 3);
            var day1Order = living.Board.Order.TemplateId;
            var day1Sponsor = living.Board.Sponsor.TemplateId;
            for (var day = 2; day < 30; day++)
            {
                var prevOrder = living.Board.Order.TemplateId;
                var prevSponsor = living.Board.Sponsor.TemplateId;
                Assert.IsTrue(living.EnsureDay(day, 3));
                Assert.AreNotEqual(prevOrder, living.Board.Order.TemplateId, "day " + day);
                Assert.AreNotEqual(prevSponsor, living.Board.Sponsor.TemplateId, "day " + day);
                Assert.AreEqual(ContractStatus.Active, living.Board.Order.Status, "a fresh day starts active and empty");
                Assert.AreEqual(0, living.Board.Order.Progress);
            }
            Assert.IsNotNull(day1Order); Assert.IsNotNull(day1Sponsor);
        }

        [Test]
        public void ContractsTheWorldCannotServeAreNeverGenerated()
        {
            var seen = new HashSet<string>();
            for (var day = 1; day < 120; day++)
            {
                var fresh = new LivingWorldAuthority(new Money(economy));
                fresh.EnsureDay(day, day * 5);
                seen.Add(fresh.Board.Sponsor.TemplateId);
            }
            CollectionAssert.DoesNotContain(seen, ContractIds.SponsorNight, "no night capture data: the night sponsor stays dormant");
            Assert.AreEqual(3, seen.Count, "the three available sponsors all come up");

            world.Unrecorded = false;
            seen.Clear();
            for (var day = 1; day < 120; day++)
            {
                var fresh = new LivingWorldAuthority(new Money(economy));
                fresh.EnsureDay(day, day * 5);
                seen.Add(fresh.Board.Sponsor.TemplateId);
            }
            CollectionAssert.DoesNotContain(seen, ContractIds.SponsorNewSpecies, "no unrecorded species left: that sponsor is not asked for");

            world.Night = true;
            seen.Clear();
            for (var day = 1; day < 200; day++)
            {
                var fresh = new LivingWorldAuthority(new Money(economy));
                fresh.EnsureDay(day, day * 5);
                seen.Add(fresh.Board.Sponsor.TemplateId);
            }
            CollectionAssert.Contains(seen, ContractIds.SponsorNight, "once the world has night capture the dormant template is eligible");
        }

        // ---- fish orders ------------------------------------------------------------------------------------------------

        [Test]
        public void AFishOrderAdvancesOnlyThroughSettledHandInsAndPaysExactlyOnce()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorEvent);
            var before = economy.SharedBalance;

            living.OnCatchesSold(Deal(), Catches(("sea_bass", 800)));
            Assert.AreEqual(1, living.Board.Order.Progress);
            Assert.AreEqual(ContractStatus.Active, living.Board.Order.Status);
            Assert.AreEqual(before, economy.SharedBalance, "no money before the order is met");

            var finishing = Deal();
            living.OnCatchesSold(finishing, Catches(("sea_bass", 900)));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);
            Assert.AreEqual(2, living.Board.Order.Progress);
            ContractCatalog.TryGet(ContractIds.OrderQuick, out var quick);
            Assert.AreEqual(before + quick.Reward, economy.SharedBalance, "paid once");

            living.OnCatchesSold(finishing, Catches(("sea_bass", 900)));   // the very same hand-in again
            living.OnCatchesSold(Deal(), Catches(("sea_bass", 900), ("sea_bass", 900)));   // a later one: the order is done
            Assert.AreEqual(before + quick.Reward, economy.SharedBalance, "no second payment");
            Assert.AreEqual(2, living.Board.Order.Progress);
        }

        [Test]
        public void TheSpeciesOrderCountsOnlyThatSpeciesAndTheWeightOrderCountsGrams()
        {
            ForceBoard(ContractIds.OrderBass, ContractIds.SponsorQuality);
            living.OnCatchesSold(Deal(), Catches(("other_fish", 500), ("other_fish", 500)));
            Assert.AreEqual(0, living.Board.Order.Progress, "another species does not count");
            living.OnCatchesSold(Deal(), Catches(("sea_bass", 100), ("sea_bass", 100), ("other_fish", 100)));
            Assert.AreEqual(2, living.Board.Order.Progress);
            living.OnCatchesSold(Deal(), Catches(("sea_bass", 100), ("sea_bass", 100), ("sea_bass", 100)));
            Assert.AreEqual(4, living.Board.Order.Progress, "progress never passes the target");
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);

            ForceBoardNextDay(ContractIds.OrderHeavy);
            living.OnCatchesSold(Deal(), Catches(("sea_bass", 1000), ("sea_bass", 1200)));
            Assert.AreEqual(2200, living.Board.Order.Progress);
            living.OnCatchesSold(Deal(), Catches(("sea_bass", 900)));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);
            Assert.AreEqual(3000, living.Board.Order.Progress);
        }

        private void ForceBoardNextDay(string orderId)
        {
            var from = living.Board.Day + 1;
            for (var day = from; day < from + 400; day++)
            {
                var probe = new LivingWorldAuthority(new Money(economy));
                probe.EnsureDay(day, boardSeed);
                if (probe.Board.Order.TemplateId == orderId && living.Board.Order.TemplateId != orderId)
                {
                    Assert.IsTrue(living.EnsureDay(day, boardSeed));
                    if (living.Board.Order.TemplateId == orderId) return;
                }
            }
            Assert.Fail("could not reach " + orderId);
        }

        // ---- video sponsors ---------------------------------------------------------------------------------------------

        [Test]
        public void ASponsorNeedsTheRightVerifiedClipAndPaysOnce()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorQuality);
            var before = economy.SharedBalance;
            living.OnPublication("pub-low", Clip(2, false));
            Assert.AreEqual(ContractStatus.Active, living.Board.Sponsor.Status, "quality 2 is below the sponsor's bar");

            living.OnPublication("pub-high", Clip(3, false, id: "clip-b"));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Sponsor.Status);
            ContractCatalog.TryGet(ContractIds.SponsorQuality, out var quality);
            Assert.AreEqual(before + quality.Reward, economy.SharedBalance);
            living.OnPublication("pub-high", Clip(4, false, id: "clip-b"));
            living.OnPublication("pub-other", Clip(4, false, id: "clip-c"));
            Assert.AreEqual(before + quality.Reward, economy.SharedBalance, "a replay or a second clip pays nothing more");
        }

        [Test]
        public void TheNewSpeciesAndEventSponsorsReadTheHostsWorldContextOfTheClip()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorNewSpecies);
            living.OnPublication("pub-1", Clip(4, false));
            Assert.AreEqual(ContractStatus.Active, living.Board.Sponsor.Status, "not the first recording of its species");
            living.OnPublication("pub-2", Clip(1, true, id: "clip-b"));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Sponsor.Status);

            // next time the event sponsor comes up
            for (var day = living.Board.Day + 1; day < 600; day++)
            {
                var probe = new LivingWorldAuthority(new Money(economy));
                probe.EnsureDay(day, boardSeed);
                if (probe.Board.Sponsor.TemplateId == ContractIds.SponsorEvent && living.EnsureDay(day, boardSeed) && living.Board.Sponsor.TemplateId == ContractIds.SponsorEvent) break;
            }
            Assert.AreEqual(ContractIds.SponsorEvent, living.Board.Sponsor.TemplateId);
            living.OnPublication("pub-3", Clip(4, true, id: "clip-c"));
            Assert.AreEqual(ContractStatus.Active, living.Board.Sponsor.Status, "a species clip is not an event clip");
            living.OnPublication("pub-4", Clip(1, false, RecordingSubjectKind.Event, "clip-d"));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Sponsor.Status);
        }

        [Test]
        public void TheRealChannelAuthorityFeedsTheSponsorOnlyThroughAnAcceptedPublication()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorQuality);
            var channel = new ChannelAuthority();
            channel.Configure(new Rights(), () => living.Board.Day, () => false);
            channel.OnPublished += (publication, clip) => living.OnPublication(publication.PublicationId, clip);

            var clip = Clip(3, false);
            Assert.AreEqual(ClipArchiveOutcome.Added, channel.Submit(clip));
            Assert.AreEqual(ContractStatus.Active, living.Board.Sponsor.Status, "archiving a clip is not publishing it");
            Assert.AreEqual("NotOwner", channel.TryPublish(Guest, clip.ClipId, "x", request++).ReasonCode);
            Assert.AreEqual(ContractStatus.Active, living.Board.Sponsor.Status, "a refused publication moves nothing");

            var before = economy.SharedBalance;
            Assert.IsTrue(channel.TryPublish(Host, clip.ClipId, "Levrek", request++).Accepted);
            Assert.AreEqual(ContractStatus.Completed, living.Board.Sponsor.Status);
            Assert.Greater(economy.SharedBalance, before);
            var paid = economy.SharedBalance;
            Assert.AreEqual("PublicationAlreadyQueued", channel.TryPublish(Host, clip.ClipId, "yeniden adlandirma", request++).ReasonCode, "renaming is not a new publication");
            Assert.AreEqual(paid, economy.SharedBalance);
        }

        // ---- economy hooks ------------------------------------------------------------------------------------------------

        private void SeedPending(params (string id, int grams)[] items)
        {
            var data = economy.ExportSaveData("c", "c");
            foreach (var i in items)
                data.PendingTurnIns.Add(new PendingTurnInSave
                {
                    ItemId = i.id, Kind = (byte)TurnInKind.Catch, SourceDiveId = "dive-1", SubjectId = "sea_bass",
                    WeightGrams = i.grams, SharedEscrow = true, Revision = 1
                });
            Assert.IsTrue(economy.TryRestore(data));
        }

        [Test]
        public void ASettledFishHandInRaisesOneEventWithItsSpeciesAndWeightAndAReplayRaisesNone()
        {
            var events = new List<(string deal, int count, int grams)>();
            economy.OnCatchesSold += (deal, catches) =>
            {
                var g = 0;
                foreach (var c in catches) { Assert.AreEqual("sea_bass", c.SpeciesId); g += c.WeightGrams; }
                events.Add((deal, catches.Count, g));
            };
            SeedPending(("c1", 700), ("c2", 800));
            var id = request++;
            Assert.IsTrue(economy.TrySellCatches(Host, id).Accepted);
            Assert.IsTrue(economy.TrySellCatches(Host, id).Accepted, "replayed request");
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(2, events[0].count);
            Assert.AreEqual(1500, events[0].grams);
            Assert.IsFalse(economy.TrySellCatches(Host, request++).Accepted, "nothing left to sell");
            Assert.AreEqual(1, events.Count);
        }

        [Test]
        public void TheEconomyAndTheOrderTogetherPayTheSaleAndTheBonusOnceEach()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorEvent);
            economy.OnCatchesSold += (deal, catches) => living.OnCatchesSold(deal, catches);
            SeedPending(("c1", 700), ("c2", 800));
            var start = economy.SharedBalance;
            Assert.IsTrue(economy.TrySellCatches(Host, request++).Accepted);
            ContractCatalog.TryGet(ContractIds.OrderQuick, out var quick);
            Assert.AreEqual(start + 240 + quick.Reward, economy.SharedBalance, "two catches at 120 plus the order bonus");
            Assert.IsTrue(economy.IsRewardPaid(ContractIds.RewardId(living.Board.Day, ContractIds.OrderQuick)));
        }

        // ---- development ---------------------------------------------------------------------------------------------------

        [Test]
        public void DevelopmentIsAOneOffPaymentFromTheSharedBalanceAndNeverNegative()
        {
            Fund(100);
            Assert.AreEqual("InsufficientFunds", living.TryBuildDevelopment(Host, DevelopmentIds.TownFisher, request++).ReasonCode);
            Assert.AreEqual(100, economy.SharedBalance);
            Assert.AreEqual("InvalidTarget", living.TryBuildDevelopment(Host, "town-nothing", request++).ReasonCode);

            Fund(1000);
            var id = request++;
            Assert.IsTrue(living.TryBuildDevelopment(Guest, DevelopmentIds.TownFisher, id).Accepted, "any player at the PC may build for the campaign");
            DevelopmentCatalog.TryGet(DevelopmentIds.TownFisher, out var fisher);
            Assert.AreEqual(1000 - fisher.Price, economy.SharedBalance);
            Assert.IsTrue(living.Development.Owns(DevelopmentIds.TownFisher));

            Assert.IsTrue(living.TryBuildDevelopment(Guest, DevelopmentIds.TownFisher, id).Accepted, "replayed request answers from the cache");
            Assert.AreEqual("AlreadyProcessed", living.TryBuildDevelopment(Host, DevelopmentIds.TownFisher, request++).ReasonCode);
            Assert.AreEqual(1000 - fisher.Price, economy.SharedBalance, "one charge");
        }

        [Test]
        public void AFailedWriteUndoesTheDevelopmentAndTheChargeAndTheRequestCanBeRetried()
        {
            Fund(2000);
            var works = false;
            living = new LivingWorldAuthority(new Money(economy), () => works);
            var id = request++;
            Assert.AreEqual("SaveFailed", living.TryBuildDevelopment(Host, DevelopmentIds.Home2, id).ReasonCode);
            Assert.AreEqual(2000, economy.SharedBalance);
            Assert.IsFalse(living.Development.Owns(DevelopmentIds.Home2));
            works = true;
            Assert.IsTrue(living.TryBuildDevelopment(Host, DevelopmentIds.Home2, id).Accepted);
            DevelopmentCatalog.TryGet(DevelopmentIds.Home2, out var home);
            Assert.AreEqual(2000 - home.Price, economy.SharedBalance);
        }

        [Test]
        public void EachImprovementChangesTheSystemItIsMeantToChange()
        {
            var state = new List<string>();
            Func<DevelopmentState> provider = () => new DevelopmentState(state, 0);
            DevelopmentEffects.Bind(provider);
            try
            {
                // base values
                Assert.AreEqual(EconomyManager.StorageCapacityItems, EconomyManager.StorageCapacity);
                DevelopmentCatalog.TryGet(DevelopmentIds.TownDock, out _);
                VehicleCatalog.TryGet(VehicleIds.Motorboat, out var motor);
                Assert.AreEqual(VehicleCatalog.MotorboatPrice, EconomyManager.VehiclePrice(motor));
                SeedPending(("c1", 700));
                Assert.AreEqual("RequirementMissing", economy.TryPurchase(Host, DevelopmentCatalog.ShopUnlockedEquipmentId, request++).ReasonCode, "Tup III is not stocked yet");

                // home level 2: bigger shared storage
                state.Add(DevelopmentIds.Home2);
                Assert.AreEqual(EconomyManager.StorageCapacityItems + DevelopmentCatalog.HomeStorageBonusSlots, EconomyManager.StorageCapacity);

                // dock: vehicles cost 10% less (rounded down), the catalog price itself is unchanged
                state.Add(DevelopmentIds.TownDock);
                Assert.AreEqual(VehicleCatalog.MotorboatPrice - VehicleCatalog.MotorboatPrice / 10, EconomyManager.VehiclePrice(motor));
                Assert.AreEqual(VehicleCatalog.MotorboatPrice, motor.Price);

                // shop: the new tube tier can be bought (and gives the next level to Mehmet's strongest-level rule)
                state.Add(DevelopmentIds.TownShop);
                Fund(5000);
                Assert.IsTrue(economy.TryPurchase(Host, DevelopmentCatalog.ShopUnlockedEquipmentId, request++).Accepted);
                Assert.IsTrue(economy.TryGetEquipmentDefinition(DevelopmentCatalog.ShopUnlockedEquipmentId, out var tube3));
                Assert.AreEqual("tube", tube3.Slot);
                Assert.AreEqual(3, tube3.Level);

                // fisherman: the same fish sells for 10% more
                state.Add(DevelopmentIds.TownFisher);
                SeedPending(("c1", 700));
                var after = economy.SharedBalance;
                Assert.IsTrue(economy.TrySellCatches(Host, request++).Accepted);
                Assert.AreEqual(after + 120 + 12, economy.SharedBalance, "120 + 10%");
            }
            finally { DevelopmentEffects.Unbind(provider); }
            Assert.AreEqual(EconomyManager.StorageCapacityItems, EconomyManager.StorageCapacity, "unbound = base values again");
        }

        [Test]
        public void TheLargerStorageIsReallyUsableAndASaveWithMoreThanTheBaseStaysIntact()
        {
            var state = new List<string> { DevelopmentIds.Home2 };
            Func<DevelopmentState> provider = () => new DevelopmentState(state, 0);
            DevelopmentEffects.Bind(provider);
            try
            {
                var data = economy.ExportSaveData("c", "c");
                var total = EconomyManager.StorageCapacityItems + 5;
                for (var i = 0; i < total; i++)
                    data.StoredItems.Add(new PendingTurnInSave { ItemId = "s" + i, Kind = (byte)TurnInKind.Catch, SubjectId = "sea_bass", WeightGrams = 100, SharedEscrow = true, Revision = 1 });
                Assert.IsTrue(economy.TryRestore(data));
                Assert.AreEqual(total, economy.StoredCount, "a restore keeps what a level-2 home held, even before the development is bound");
            }
            finally { DevelopmentEffects.Unbind(provider); }
        }

        // ---- roles ------------------------------------------------------------------------------------------------------------

        [Test]
        public void ARoleIsFreeOnePerPlayerAndChangingItNeverStacksAnything()
        {
            var before = economy.SharedBalance;
            var revision = living.RoleRevision;
            Assert.AreEqual(CrewRole.None, living.RoleOf(Host));
            Assert.IsTrue(living.TrySelectRole(Host, CrewRole.Hunter, request++).Accepted);
            Assert.AreEqual(CrewRole.Hunter, living.RoleOf(Host));
            Assert.AreEqual(before, economy.SharedBalance, "choosing a role costs nothing");
            Assert.AreEqual(revision + 1, living.RoleRevision);

            Assert.AreEqual("AlreadyProcessed", living.TrySelectRole(Host, CrewRole.Hunter, request++).ReasonCode);
            Assert.AreEqual(revision + 1, living.RoleRevision, "a no-change request does not trigger a recompute");

            Assert.IsTrue(living.TrySelectRole(Host, CrewRole.Carrier, request++).Accepted);
            Assert.AreEqual(CrewRole.Carrier, living.RoleOf(Host), "one role per player: the new one REPLACES the old");
            Assert.IsTrue(living.TrySelectRole(Guest, CrewRole.Carrier, request++).Accepted, "two players may share a role");
            Assert.AreEqual(CrewRole.Carrier, living.RoleOf(Host));
            Assert.IsTrue(living.TrySelectRole(Host, CrewRole.None, request++).Accepted, "no role is a valid choice: nothing is locked behind a role");
            Assert.AreEqual(CrewRole.None, living.RoleOf(Host));
            Assert.AreEqual("InvalidTarget", living.TrySelectRole(Host, (CrewRole)9, request++).ReasonCode);
        }

        [Test]
        public void ReplayedRoleRequestsAreIdempotentAndALeavingPlayerTakesTheRoleAway()
        {
            var id = request++;
            Assert.IsTrue(living.TrySelectRole(Guest, CrewRole.Explorer, id).Accepted);
            var revision = living.RoleRevision;
            Assert.IsTrue(living.TrySelectRole(Guest, CrewRole.Explorer, id).Accepted, "same request id: cached answer");
            Assert.AreEqual(revision, living.RoleRevision);

            living.ForgetPlayer(Guest);
            Assert.AreEqual(CrewRole.None, living.RoleOf(Guest), "a rejoining guest starts without a role: no inherited or doubled bonus");
            Assert.AreEqual(revision + 1, living.RoleRevision);
            living.ForgetPlayer(Host);
            Assert.AreEqual(revision + 1, living.RoleRevision, "the host's role is not dropped by a (non-)disconnect call");
        }

        [Test]
        public void OnlyTheHostsRoleIsSavedAndTheEffectLayerSeesTheAuthoritysRoles()
        {
            living.TrySelectRole(Host, CrewRole.CameraOperator, request++);
            living.TrySelectRole(Guest, CrewRole.Hunter, request++);
            Assert.AreEqual(CrewRole.CameraOperator, living.Roles[Host]);
            Assert.AreEqual(CrewRole.Hunter, living.Roles[Guest]);
            Assert.IsFalse(living.Roles.ContainsKey(Guest2));
            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
            Assert.AreEqual((byte)CrewRole.CameraOperator, disk.Living.HostRole, "D06: only the host's role is written");
        }

        // ---- save / load ------------------------------------------------------------------------------------------------------

        private sealed class Rig : IDisposable
        {
            public readonly GameObject Root;
            public readonly EconomyManager Economy;
            public readonly EconomySaveStore Store;
            public readonly LivingWorldAuthority Living;

            public Rig(string path)
            {
                Root = new GameObject("living-rig");
                Root.AddComponent<InventoryManager>();
                Economy = Root.AddComponent<EconomyManager>();
                Store = Root.AddComponent<EconomySaveStore>();
                Store.SetPathForTests(path);
                Assert.IsTrue(Store.LoadNow());
                Economy.SetPersistenceHandler(Store.SaveNow);
                Living = new LivingWorldAuthority(new Money(Economy), Store.SaveNow);
                Store.Living = Living;
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }

        [Test]
        public void EverythingSurvivesASaveAndReopenWithoutPayingOrBuyingTwice()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorQuality);
            Fund(3000);
            living.OnCatchesSold("deal-A", Catches(("sea_bass", 800)));
            living.OnCatchesSold("deal-B", Catches(("sea_bass", 800)));   // completes the order
            living.OnPublication("pub-1", Clip(4, false));
            Assert.IsTrue(living.TryBuildDevelopment(Host, DevelopmentIds.Home2, request++).Accepted);
            Assert.IsTrue(living.TrySelectRole(Host, CrewRole.Explorer, request++).Accepted);
            var balance = economy.SharedBalance;
            var day = living.Board.Day;

            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
            Assert.AreEqual(EconomySaveData.CurrentSchemaVersion, disk.SchemaVersion);
            Assert.IsTrue(disk.HasLiving);
            Assert.AreEqual(2, disk.RewardIds.Count, "the order and the sponsor reward ids are in the economy's own record");

            using (var reopened = new Rig(path))
            {
                var board = reopened.Living.Board;
                Assert.AreEqual(day, board.Day);
                Assert.AreEqual(ContractStatus.Completed, board.Order.Status);
                Assert.AreEqual(ContractStatus.Completed, board.Sponsor.Status);
                Assert.IsTrue(reopened.Living.Development.Owns(DevelopmentIds.Home2));
                Assert.AreEqual(CrewRole.Explorer, reopened.Living.RoleOf(Host));
                Assert.AreEqual(balance, reopened.Economy.SharedBalance);

                // replays after the reopen change nothing
                Assert.IsFalse(reopened.Living.EnsureDay(day, boardSeed), "the day is not rolled again");
                reopened.Living.OnCatchesSold("deal-A", Catches(("sea_bass", 800)));
                reopened.Living.OnPublication("pub-1", Clip(4, false));
                Assert.AreEqual("AlreadyProcessed", reopened.Living.TryBuildDevelopment(Host, DevelopmentIds.Home2, request++).ReasonCode);
                Assert.AreEqual(balance, reopened.Economy.SharedBalance, "no second reward, no second charge");
            }
        }

        [Test]
        public void ANextDayAfterTheReopenRollsOnceAndTheEvidenceKeepsYesterdayFromCountingAgain()
        {
            living.EnsureDay(5, 11);
            living.OnCatchesSold("deal-old", Catches(("sea_bass", 3000)));
            using (var reopened = new Rig(path))
            {
                Assert.IsTrue(reopened.Living.EnsureDay(6, 11));
                var order = reopened.Living.Board.Order;
                reopened.Living.OnCatchesSold("deal-old", Catches(("sea_bass", 3000), ("sea_bass", 3000)));
                Assert.AreEqual(0, reopened.Living.Board.Order.Progress, "yesterday's hand-in cannot feed today's order");
                Assert.AreEqual(order.TemplateId, reopened.Living.Board.Order.TemplateId);
            }
        }

        [Test]
        public void AnOlderFileWithoutTheLivingRecordOpensEmpty()
        {
            var legacy = new EconomySaveData { SchemaVersion = 7, SharedBalance = 55 };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(legacy));
            using (var rig = new Rig(path))
            {
                Assert.AreEqual(0, rig.Living.Board.Day);
                Assert.IsFalse(rig.Living.Board.Order.HasContract);
                Assert.AreEqual(0, rig.Living.Development.OwnedIds.Count);
                Assert.AreEqual(CrewRole.None, rig.Living.RoleOf(Host));
                Assert.AreEqual(55, rig.Economy.SharedBalance);
                Assert.IsTrue(rig.Living.EnsureDay(1, 1), "the first day simply starts");
            }
        }

        [Test]
        public void ATamperedRecordCannotHandOutWhatTheRulesWouldNot()
        {
            var data = new LivingWorldSaveData
            {
                BoardDay = 3,
                OrderTemplateId = ContractIds.SponsorQuality,    // a sponsor in the order slot
                OrderStatus = 2, OrderProgress = 5,
                SponsorTemplateId = "sponsor-forged",
                LastOrderTemplateId = "nope",
                DevelopmentIds = new List<string> { DevelopmentIds.TownDock, "town-forged", DevelopmentIds.TownDock },
                UsedEvidenceIds = new List<string> { "a", " ", "a", "b" },
                HostRole = 77
            };
            Assert.IsTrue(living.RestoreLiving(data));
            Assert.IsFalse(living.Board.Order.HasContract, "wrong-kind template: slot dropped");
            Assert.IsFalse(living.Board.Sponsor.HasContract, "unknown template: slot dropped");
            CollectionAssert.AreEqual(new[] { DevelopmentIds.TownDock }, living.Development.OwnedIds);
            Assert.AreEqual(CrewRole.None, living.RoleOf(Host), "an invalid role byte is no role");

            // progress outside the template's range is clamped; a full one is a completed one
            living.RestoreLiving(new LivingWorldSaveData { BoardDay = 2, OrderTemplateId = ContractIds.OrderQuick, OrderStatus = 1, OrderProgress = 99 });
            Assert.AreEqual(2, living.Board.Order.Progress);
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);
            living.RestoreLiving(new LivingWorldSaveData { BoardDay = 2, OrderTemplateId = ContractIds.OrderQuick, OrderStatus = 1, OrderProgress = -9 });
            Assert.AreEqual(0, living.Board.Order.Progress);
            Assert.AreEqual(ContractStatus.Active, living.Board.Order.Status);

            // a version this build does not know is the empty default
            living.RestoreLiving(new LivingWorldSaveData { Version = 9, BoardDay = 4, DevelopmentIds = new List<string> { DevelopmentIds.Home2 } });
            Assert.AreEqual(0, living.Board.Day);
            Assert.AreEqual(0, living.Development.OwnedIds.Count);
        }

        [Test]
        public void ATamperedRewardAlreadyPaidInTheEconomyIsNotPaidAgain()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorEvent);
            var rewardId = ContractIds.RewardId(living.Board.Day, ContractIds.OrderQuick);
            Assert.IsTrue(economy.TryCreditReward(rewardId, 80));   // paid already (the file lost the living record)
            var balance = economy.SharedBalance;
            living.OnCatchesSold("deal-1", Catches(("sea_bass", 100), ("sea_bass", 100)));
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);
            Assert.AreEqual(balance, economy.SharedBalance, "the reward id is remembered by the economy too: one payment, ever");
        }

        [Test]
        public void ARewardWhoseWriteFailsIsUndoneAndCanStillCompleteLater()
        {
            ForceBoard(ContractIds.OrderQuick, ContractIds.SponsorEvent);
            var works = true;
            living = new LivingWorldAuthority(new Money(economy), () => works);
            ForceBoardOn(living, ContractIds.OrderQuick);
            var balance = economy.SharedBalance;
            living.OnCatchesSold("deal-1", Catches(("sea_bass", 100)));
            works = false;
            living.OnCatchesSold("deal-2", Catches(("sea_bass", 100)));   // would complete: the write fails
            Assert.AreEqual(ContractStatus.Active, living.Board.Order.Status);
            Assert.AreEqual(1, living.Board.Order.Progress);
            Assert.AreEqual(balance, economy.SharedBalance, "no money without a saved completion");
            works = true;
            living.OnCatchesSold("deal-2", Catches(("sea_bass", 100)));   // the same hand-in event again once the disk recovered
            Assert.AreEqual(ContractStatus.Completed, living.Board.Order.Status);
            Assert.AreEqual(balance + 80, economy.SharedBalance);
        }

        private void ForceBoardOn(LivingWorldAuthority target, string orderId)
        {
            for (var day = 1; day < 400; day++)
            {
                var fresh = new LivingWorldAuthority(new Money(economy));
                fresh.EnsureDay(day, 7);
                if (fresh.Board.Order.TemplateId == orderId && target.EnsureDay(day, 7) && target.Board.Order.TemplateId == orderId) return;
            }
            Assert.Fail("could not reach " + orderId);
        }
    }
}
