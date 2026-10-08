using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Economy
{
    // P4.5-C: host-only decisions. The UI never reports a catch, quality, night condition or payment.
    // Source events must come from EconomyManager's committed turn-ins and ChannelAuthority publications.
    [Serializable]
    public sealed class LivingWorldSaveData
    {
        public int DayNumber;
        public string OrderId = "";
        public string SponsorId = "";
        public int OrderProgress;
        public int SponsorProgress;
        public bool OrderPaid;
        public bool SponsorPaid;
        public List<string> SeenCatchIds = new List<string>();
        public List<string> SeenPublicationIds = new List<string>();
        public List<string> PurchasedUpgradeIds = new List<string>();
        public List<string> PaidRewardIds = new List<string>();
        // D06: only the host has a stable player identity across game launches.
        public byte HostRole;
    }

    public static class LivingWorldCatalog
    {
        public const string HouseArchive = "home-archive";
        public const string HouseGallery = "home-gallery";
        public const string FishMarket = "town-fish-market";
        public const string EquipmentDisplay = "town-equipment";
        public const string HarborLights = "town-harbor";

        public static int UpgradeCost(string id) => id switch
        {
            HouseArchive => 280,
            HouseGallery => 460,
            FishMarket => 240,
            EquipmentDisplay => 330,
            HarborLights => 420,
            _ => -1
        };

        public static bool IsKnownUpgrade(string id) => UpgradeCost(id) >= 0;

        // Three small, accessible shore orders. World-gated alternatives can be introduced when
        // Utku's species/region eligibility feed is authoritative; no unreachable target is rolled now.
        public static readonly string[] OrderIds = { "shore-one", "shore-two", "shore-three" };
        public static int OrderQuantity(string id) =>
            id == OrderIds[0] ? 1 : id == OrderIds[1] ? 2 : id == OrderIds[2] ? 3 : 0;
        public static int OrderCredits(string id) =>
            id == OrderIds[0] ? 55 : id == OrderIds[1] ? 105 : id == OrderIds[2] ? 165 : 0;

        // Only host-verifiable media conditions. Night is a template but MUST be explicitly
        // enabled by Utku's trusted night-capture proof; current ClipManifest has no capture clock.
        public const string FirstSpecies = "first-species";
        public const string GoldRecording = "gold-recording";
        public const string NightRecording = "night-recording";
        public static int SponsorCredits(string id) => id switch
        {
            FirstSpecies => 120,
            GoldRecording => 175,
            NightRecording => 220,
            _ => 0
        };
    }

    public sealed class LivingWorldAuthority
    {
        private readonly EconomyManager economy;
        private readonly Func<bool> persist;
        private readonly Func<PlayerId, CrewRole, bool> applyRole;
        private readonly Func<string, bool> availableSponsor;
        private readonly Func<ClipManifest, bool> verifiedNightRecording;
        private readonly Dictionary<PlayerId, CrewRole> roles = new Dictionary<PlayerId, CrewRole>();
        private LivingWorldSaveData state = new LivingWorldSaveData();
        private readonly HashSet<string> seenCatches = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> seenPublications = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> upgrades = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> rewards = new HashSet<string>(StringComparer.Ordinal);

        public event Action Changed;
        public int Revision { get; private set; }
        public int DayNumber => state.DayNumber;
        public string OrderId => state.OrderId;
        public string SponsorId => state.SponsorId;
        public int OrderProgress => state.OrderProgress;
        public int SponsorProgress => state.SponsorProgress;
        public bool OrderPaid => state.OrderPaid;
        public bool SponsorPaid => state.SponsorPaid;
        public int HouseTier => (upgrades.Contains(LivingWorldCatalog.HouseArchive) ? 1 : 0) +
                                (upgrades.Contains(LivingWorldCatalog.HouseGallery) ? 1 : 0);
        public bool HasUpgrade(string id) => upgrades.Contains(id);
        public CrewRole RoleFor(PlayerId player) => roles.TryGetValue(player, out var role) ? role : CrewRole.None;

        public LivingWorldAuthority(EconomyManager economy, Func<bool> persist,
            Func<PlayerId, CrewRole, bool> applyRole = null,
            Func<string, bool> availableSponsor = null,
            Func<ClipManifest, bool> verifiedNightRecording = null)
        {
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.persist = persist ?? (() => true);
            this.applyRole = applyRole ?? ((_, __) => true);
            this.availableSponsor = availableSponsor ?? (id => id != LivingWorldCatalog.NightRecording);
            this.verifiedNightRecording = verifiedNightRecording ?? (_ => false);
        }

        public bool BeginDay(int dayNumber, string campaignId)
        {
            if (dayNumber < 1) return false;
            if (state.DayNumber == dayNumber) return true;
            // A fixed, platform-stable hash, not string.GetHashCode (which may randomize across runs).
            uint seed = 2166136261;
            foreach (var c in campaignId ?? "") seed = (seed ^ c) * 16777619;
            seed = (seed ^ (uint)dayNumber) * 16777619;
            var order = LivingWorldCatalog.OrderIds[(int)(seed % 3)];
            var sponsors = new[] { LivingWorldCatalog.FirstSpecies, LivingWorldCatalog.GoldRecording,
                LivingWorldCatalog.NightRecording };
            var chosen = "";
            for (var n = 0; n < sponsors.Length; n++)
            {
                var candidate = sponsors[(int)((seed + (uint)n) % (uint)sponsors.Length)];
                if (!availableSponsor(candidate)) continue;
                chosen = candidate;
                break;
            }
            if (chosen.Length == 0) return false; // never issue an unreachable objective

            var old = Export();
            state.DayNumber = dayNumber;
            state.OrderId = order;
            state.SponsorId = chosen;
            state.OrderProgress = 0;
            state.SponsorProgress = 0;
            state.OrderPaid = false;
            state.SponsorPaid = false;
            seenCatches.Clear();
            seenPublications.Clear();
            state.SeenCatchIds.Clear();
            state.SeenPublicationIds.Clear();
            if (!persist()) { Restore(old); return false; }
            Notify();
            return true;
        }

        public TransactionResult TrySelectRole(PlayerId player, CrewRole role, ulong requestId, bool townAllowed)
        {
            if (!townAllowed || DayLock.IsLocked) return Reject(requestId, "NotInTown");
            if (requestId == 0 || !CrewRoleEffectRules.IsValid(role)) return Reject(requestId, "InvalidRole");
            var previous = RoleFor(player);
            if (previous == role) return TransactionResult.Ok(requestId, Revision);
            if (!applyRole(player, role)) return Reject(requestId, "RoleUnavailable");
            roles[player] = role;
            if (player.Value == 0) state.HostRole = (byte)role;
            if (!persist())
            {
                roles[player] = previous;
                if (player.Value == 0) state.HostRole = (byte)previous;
                applyRole(player, previous);
                return Reject(requestId, "SaveFailed");
            }
            Notify();
            return TransactionResult.Ok(requestId, Revision);
        }

        public TransactionResult TryUpgrade(PlayerId player, string upgradeId, ulong requestId, bool townAllowed)
        {
            if (!townAllowed || DayLock.IsLocked) return Reject(requestId, "NotInTown");
            if (requestId == 0 || !LivingWorldCatalog.IsKnownUpgrade(upgradeId)) return Reject(requestId, "InvalidUpgrade");
            if (upgrades.Contains(upgradeId)) return Reject(requestId, "AlreadyOwned");
            if (upgradeId == LivingWorldCatalog.HouseGallery && !upgrades.Contains(LivingWorldCatalog.HouseArchive))
                return Reject(requestId, "RequiresArchive");
            var cost = LivingWorldCatalog.UpgradeCost(upgradeId);
            if (!economy.TryPurchaseLivingUpgrade("living-upgrade-" + upgradeId, cost,
                () => { upgrades.Add(upgradeId); state.PurchasedUpgradeIds.Add(upgradeId); ApplyBenefits(); },
                () => { upgrades.Remove(upgradeId); state.PurchasedUpgradeIds.Remove(upgradeId); ApplyBenefits(); }))
                return Reject(requestId, economy.SharedBalance < cost ? "NotEnoughCredits" : "SaveFailed");
            Notify();
            return TransactionResult.Ok(requestId, Revision);
        }

        // Caller: authoritative EconomyManager.OnVerifiedCatchesSold, only AFTER a committed NPC sale.
        // Evidence can never come from a client request or a UI increment.
        public void OnVerifiedCatchesSold(IReadOnlyList<PendingTurnInState> sold)
        {
            if (sold == null || state.DayNumber < 1 || state.OrderPaid) return;
            var previous = Export();
            foreach (var item in sold)
            {
                if (item.Kind != TurnInKind.Catch || item.SubjectId != "sea_bass" ||
                    string.IsNullOrWhiteSpace(item.ItemId) || !seenCatches.Add(item.ItemId)) continue;
                state.SeenCatchIds.Add(item.ItemId);
                state.OrderProgress++;
            }
            if (state.OrderProgress == previous.OrderProgress) return;
            if (state.OrderProgress >= LivingWorldCatalog.OrderQuantity(state.OrderId))
            {
                var rewardId = RewardId("order", state.OrderId);
                state.OrderPaid = true;
                if (!Reward(rewardId, LivingWorldCatalog.OrderCredits(state.OrderId)))
                { Restore(previous); return; }
            }
            else if (!persist()) { Restore(previous); return; }
            Notify();
        }

        // Caller: host's ChannelAuthority, using a genuine settled publication + its archived manifest.
        // Title changes, raw clip files and unsafely returned recordings are never eligible.
        public void OnVerifiedPublication(in ClipManifest clip, PublicationSave publication)
        {
            if (state.DayNumber < 1 || state.SponsorPaid || publication == null ||
                string.IsNullOrWhiteSpace(publication.SettledId) ||
                publication.QueuedDay != state.DayNumber || !clip.IsCommercialCandidate ||
                clip.ClipId != publication.ClipId || clip.RecordingId != publication.RecordingId ||
                clip.SubjectId != publication.SubjectId ||
                seenPublications.Contains(publication.PublicationId)) return;
            var old = Export();
            seenPublications.Add(publication.PublicationId);
            state.SeenPublicationIds.Add(publication.PublicationId);
            var qualifies = state.SponsorId == LivingWorldCatalog.FirstSpecies &&
                            clip.WorldContext.Kind == RecordingSubjectKind.Species &&
                            clip.WorldContext.FirstRecordingOfSubject ||
                            state.SponsorId == LivingWorldCatalog.GoldRecording && clip.Quality >= 3 ||
                            state.SponsorId == LivingWorldCatalog.NightRecording && verifiedNightRecording(clip);
            if (qualifies)
            {
                state.SponsorProgress = 1;
                state.SponsorPaid = true;
                if (!Reward(RewardId("sponsor", state.SponsorId), LivingWorldCatalog.SponsorCredits(state.SponsorId)))
                { Restore(old); return; }
            }
            else if (!persist()) { Restore(old); return; }
            Notify();
        }

        private string RewardId(string kind, string id) => "living-" + state.DayNumber + "-" + kind + "-" + id;
        private bool Reward(string id, int credits)
        {
            if (!rewards.Add(id)) return false;
            state.PaidRewardIds.Add(id);
            if (economy.TryCreditLivingReward(id, credits)) return true;
            rewards.Remove(id);
            state.PaidRewardIds.Remove(id);
            return false;
        }

        public LivingWorldSaveData Export() => new LivingWorldSaveData
        {
            DayNumber = state.DayNumber, OrderId = state.OrderId, SponsorId = state.SponsorId,
            OrderProgress = state.OrderProgress, SponsorProgress = state.SponsorProgress,
            OrderPaid = state.OrderPaid, SponsorPaid = state.SponsorPaid,
            HostRole = state.HostRole,
            SeenCatchIds = new List<string>(state.SeenCatchIds),
            SeenPublicationIds = new List<string>(state.SeenPublicationIds),
            PurchasedUpgradeIds = new List<string>(state.PurchasedUpgradeIds),
            PaidRewardIds = new List<string>(state.PaidRewardIds)
        };

        public bool Restore(LivingWorldSaveData loaded)
        {
            loaded ??= new LivingWorldSaveData();
            if (loaded.DayNumber < 0 || loaded.OrderProgress < 0 || loaded.SponsorProgress < 0 ||
                !CrewRoleEffectRules.IsValid((CrewRole)loaded.HostRole)) return false;
            if (loaded.DayNumber > 0 &&
                (LivingWorldCatalog.OrderQuantity(loaded.OrderId) == 0 ||
                 LivingWorldCatalog.SponsorCredits(loaded.SponsorId) == 0)) return false;
            var newUpgrades = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in loaded.PurchasedUpgradeIds ?? new List<string>())
                if (LivingWorldCatalog.IsKnownUpgrade(id)) newUpgrades.Add(id);
                else return false;
            if (newUpgrades.Contains(LivingWorldCatalog.HouseGallery) &&
                !newUpgrades.Contains(LivingWorldCatalog.HouseArchive)) return false;
            var catches = new HashSet<string>(loaded.SeenCatchIds ?? new List<string>(), StringComparer.Ordinal);
            var posts = new HashSet<string>(loaded.SeenPublicationIds ?? new List<string>(), StringComparer.Ordinal);
            var paid = new HashSet<string>(loaded.PaidRewardIds ?? new List<string>(), StringComparer.Ordinal);
            if (loaded.OrderProgress != catches.Count || loaded.SponsorProgress > 1 ||
                loaded.OrderPaid && loaded.OrderProgress < LivingWorldCatalog.OrderQuantity(loaded.OrderId) ||
                loaded.SponsorPaid && loaded.SponsorProgress != 1) return false;
            state = new LivingWorldSaveData
            {
                DayNumber = loaded.DayNumber, OrderId = loaded.OrderId ?? "", SponsorId = loaded.SponsorId ?? "",
                OrderProgress = loaded.OrderProgress, SponsorProgress = loaded.SponsorProgress,
                OrderPaid = loaded.OrderPaid, SponsorPaid = loaded.SponsorPaid,
                HostRole = loaded.HostRole,
                SeenCatchIds = new List<string>(catches), SeenPublicationIds = new List<string>(posts),
                PurchasedUpgradeIds = new List<string>(newUpgrades), PaidRewardIds = new List<string>(paid)
            };
            seenCatches.Clear(); foreach (var id in catches) seenCatches.Add(id);
            seenPublications.Clear(); foreach (var id in posts) seenPublications.Add(id);
            upgrades.Clear(); foreach (var id in newUpgrades) upgrades.Add(id);
            rewards.Clear(); foreach (var id in paid) rewards.Add(id);
            ApplyBenefits();
            roles.Clear(); roles[new PlayerId(0)] = (CrewRole)state.HostRole;
            Notify();
            return true;
        }

        private void ApplyBenefits() =>
            economy.ApplyLivingUpgradeBenefits(upgrades.Contains(LivingWorldCatalog.HouseArchive),
                upgrades.Contains(LivingWorldCatalog.FishMarket));

        private TransactionResult Reject(ulong requestId, string reason) =>
            TransactionResult.Reject(requestId, reason, Revision);
        private void Notify() { Revision++; Changed?.Invoke(); }
    }
}
