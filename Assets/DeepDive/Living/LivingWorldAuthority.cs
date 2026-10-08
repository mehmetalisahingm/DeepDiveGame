using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Living
{
    // The money side the living-world authority needs, without referencing the economy assembly. The economy implements it through an
    // adapter in Composition. Credit/Spend change the in-memory balance only; the authority persists the campaign file ONCE afterwards
    // (its own record and the balance are saved in the same file), and undoes both when that write fails.
    public interface ILivingMoney
    {
        int Balance { get; }
        bool TryCredit(string rewardId, int amount);
        void ReleaseCredit(string rewardId, int amount);
        bool TrySpend(int amount);
        void RefundSpend(int amount);
        void AnnounceSpend(string spendId, int amount);
    }

    // P4.5-C (#132): the host's single authority for the daily orders/sponsors, the visible home/town development and the light roles.
    // Pure C# like DayEngine/ChannelAuthority: every rule is testable with the real economy, the real save file and no scene.
    //
    //  Orders/sponsors: one fish order and one video sponsor per campaign day, picked deterministically from the day seed among the templates
    //   the world can serve (an unreachable/unavailable one is never generated). Progress comes ONLY from host-verified events (a fish
    //   hand-in settled by the economy, a publication accepted by the channel); each event is counted once by its evidence id; a
    //   completed contract pays once by a reward id that the economy also remembers. Nothing a client sends moves a contract.
    //  Development: four one-off, permanent purchases paid from the shared balance (no upkeep); replay/duplicate cannot buy or charge twice.
    //  Roles: free, one per player, changed in town; this class only RECORDS who has which role, Mehmet's layer derives the effect.
    public sealed class LivingWorldAuthority : ILivingPersistence
    {
        private const byte OpBuild = 1;
        private const byte OpRole = 2;
        private const int MaxEvidenceIds = 512;

        private readonly ILivingMoney money;
        private readonly Func<bool> persist;
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> processed =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();

        private struct Slot
        {
            public string TemplateId;
            public ContractStatus Status;
            public int Progress;
        }

        private int boardDay;
        private int lastSeed;
        private Slot order, sponsor;
        private string lastOrderId = "", lastSponsorId = "";
        private readonly List<string> usedEvidence = new List<string>();
        private readonly HashSet<string> usedEvidenceSet = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> development = new List<string>();
        private readonly Dictionary<PlayerId, PlayerRole> roles = new Dictionary<PlayerId, PlayerRole>();
        private int revision;
        private int roleRevision;

        public event Action OnChanged;

        public LivingWorldAuthority(ILivingMoney money, Func<bool> persist = null)
        {
            this.money = money ?? throw new ArgumentNullException(nameof(money));
            this.persist = persist;
        }

        private bool Persist() => persist == null || persist();

        public int Revision => revision;
        public int RoleRevision => roleRevision;

        // ---- the daily board ------------------------------------------------------------------------------------------

        public DailyBoardState Board => new DailyBoardState(boardDay, StateOf(order), StateOf(sponsor), revision);

        private static ContractState StateOf(in Slot slot)
        {
            if (string.IsNullOrEmpty(slot.TemplateId) || !ContractCatalog.TryGet(slot.TemplateId, out var template)) return default;
            return new ContractState(slot.TemplateId, slot.Status, slot.Progress, template.Target, template.Reward);
        }

        // Starts a new campaign day: yesterday's unfinished contracts expire without penalty and today's pair is picked. Idempotent for
        // the same day (a reload, a second call, a late joiner never re-rolls), and the pick depends only on (day, seed, what exists).
        // Returns true when the board changed and was written. A failed write rolls back; the next call retries.
        public bool EnsureDay(int day, int seed)
        {
            lastSeed = seed;
            if (day <= 0 || day == boardDay) return false;

            var snapshot = Snapshot(); var snapshotRevision = revision;
            if (!string.IsNullOrEmpty(order.TemplateId)) lastOrderId = order.TemplateId;
            if (!string.IsNullOrEmpty(sponsor.TemplateId)) lastSponsorId = sponsor.TemplateId;
            boardDay = day;
            order = Pick(ContractKind.FishOrder, day, seed, lastOrderId);
            sponsor = Pick(ContractKind.VideoSponsor, day, seed, lastSponsorId);
            revision++;
            if (!Persist())
            {
                Rollback(snapshot, snapshotRevision);
                return false;
            }
            OnChanged?.Invoke();
            return true;
        }

        private static Slot Pick(ContractKind kind, int day, int seed, string avoidTemplateId)
        {
            var eligible = new List<ContractTemplate>();
            for (var i = 0; i < ContractCatalog.All.Count; i++)
            {
                var t = ContractCatalog.All[i];
                if (t.Kind == kind && IsEligibleToday(t)) eligible.Add(t);
            }
            if (eligible.Count == 0) return new Slot { TemplateId = "", Status = ContractStatus.None };

            // Variety: not the same template two days in a row when there is a choice.
            if (eligible.Count > 1 && !string.IsNullOrEmpty(avoidTemplateId))
            {
                for (var i = 0; i < eligible.Count; i++)
                    if (eligible[i].Id == avoidTemplateId) { eligible.RemoveAt(i); break; }
            }
            unchecked
            {
                var index = (int)(Mix((uint)seed * 0x9E3779B1u + (uint)day * 0x85EBCA6Bu + (uint)kind * 0xC2B2AE35u) % (uint)eligible.Count);
                return new Slot { TemplateId = eligible[index].Id, Status = ContractStatus.Active, Progress = 0 };
            }
        }

        // A small integer finalizer so the two slots of one day are decorrelated (deterministic, no System.Random state to save).
        private static uint Mix(uint x)
        {
            unchecked
            {
                x ^= x >> 16; x *= 0x7feb352du;
                x ^= x >> 15; x *= 0x846ca68bu;
                x ^= x >> 16;
                return x;
            }
        }

        // The world decides what the crew can reach/see today (Utku's seam). Unbound: no night capture, and an unrecorded species is assumed.
        private static bool IsEligibleToday(in ContractTemplate template)
        {
            var world = OrderWorld.Current;
            if (template.RequiresNight && (world == null || !world.NightCaptureAvailable)) return false;
            if (template.Measure == ContractMeasure.PublishNewSpecies && world != null && !world.UnrecordedSpeciesRemain) return false;
            return true;
        }

        // A fish hand-in the economy settled. dealId is unique per hand-in: the same hand-in can never count twice.
        public void OnCatchesSold(string dealId, IReadOnlyList<SoldCatch> catches)
        {
            if (string.IsNullOrEmpty(dealId) || catches == null || catches.Count == 0) return;
            if (order.Status != ContractStatus.Active || usedEvidenceSet.Contains(dealId)) return;
            if (!ContractCatalog.TryGet(order.TemplateId, out var template)) return;

            var delta = 0;
            for (var i = 0; i < catches.Count; i++)
            {
                switch (template.Measure)
                {
                    case ContractMeasure.ItemsSold: delta += 1; break;
                    case ContractMeasure.SpeciesItemsSold: if (catches[i].SpeciesId == template.SpeciesId) delta += 1; break;
                    case ContractMeasure.GramsSold: delta += Math.Max(0, catches[i].WeightGrams); break;
                }
            }
            if (delta <= 0) return;
            Advance(ref order, template, delta, dealId);
        }

        // A publication the channel authority accepted (host). The clip manifest is the host's verified record of what was recorded.
        public void OnPublication(string publicationId, in ClipManifest clip)
        {
            if (string.IsNullOrEmpty(publicationId) || sponsor.Status != ContractStatus.Active || usedEvidenceSet.Contains(publicationId)) return;
            if (!ContractCatalog.TryGet(sponsor.TemplateId, out var template)) return;

            bool matches;
            switch (template.Measure)
            {
                case ContractMeasure.PublishNewSpecies: matches = clip.WorldContext.FirstRecordingOfSubject; break;
                case ContractMeasure.PublishEvent: matches = clip.WorldContext.Kind == RecordingSubjectKind.Event; break;
                case ContractMeasure.PublishQuality: matches = clip.Quality >= template.Target; break;
                default: matches = false; break;   // night: needs the world's night data, which this build does not have
            }
            if (!matches) return;
            Advance(ref sponsor, template, template.Target, publicationId);
        }

        private void Advance(ref Slot slot, in ContractTemplate template, int delta, string evidenceId)
        {
            var snapshot = Snapshot(); var snapshotRevision = revision;
            var before = slot;
            slot.Progress = Math.Min(template.Target, slot.Progress + delta);
            AddEvidence(evidenceId);
            var rewardId = string.Empty;
            if (slot.Progress >= template.Target)
            {
                slot.Status = ContractStatus.Completed;
                rewardId = ContractIds.RewardId(boardDay, template.Id);
                money.TryCredit(rewardId, template.Reward);   // false = this reward id was already paid: completed without a second payment
            }
            revision++;
            if (!Persist())
            {
                if (rewardId.Length > 0) money.ReleaseCredit(rewardId, template.Reward);
                Rollback(snapshot, snapshotRevision);
                return;
            }
            OnChanged?.Invoke();
        }

        private void AddEvidence(string evidenceId)
        {
            if (!usedEvidenceSet.Add(evidenceId)) return;
            usedEvidence.Add(evidenceId);
            while (usedEvidence.Count > MaxEvidenceIds)
            {
                usedEvidenceSet.Remove(usedEvidence[0]);
                usedEvidence.RemoveAt(0);
            }
        }

        // ---- home and town development --------------------------------------------------------------------------------

        public DevelopmentState Development => new DevelopmentState(new List<string>(development), revision);

        public TransactionResult TryBuildDevelopment(PlayerId player, string id, ulong requestId)
        {
            var key = (player, requestId, OpBuild);
            if (processed.TryGetValue(key, out var replayed)) return replayed;

            if (!DevelopmentCatalog.TryGet(id, out var definition)) return Cache(key, TransactionResult.Reject(requestId, "InvalidTarget", revision));
            if (development.Contains(id)) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);
            if (money.Balance < definition.Price) return TransactionResult.Reject(requestId, "InsufficientFunds", revision);
            if (!money.TrySpend(definition.Price)) return TransactionResult.Reject(requestId, "InsufficientFunds", revision);

            development.Add(id);
            revision++;
            if (!Persist())
            {
                development.Remove(id);
                money.RefundSpend(definition.Price);
                revision--;
                return TransactionResult.Reject(requestId, "SaveFailed", revision);   // not cached: retried once the disk recovers
            }
            money.AnnounceSpend("development-" + id, definition.Price);
            var result = Cache(key, TransactionResult.Ok(requestId, revision));
            OnChanged?.Invoke();
            return result;
        }

        // ---- roles ------------------------------------------------------------------------------------------------------

        public PlayerRole RoleOf(PlayerId player) => roles.TryGetValue(player, out var role) ? role : PlayerRole.None;

        // Free, one per player. The caller (composition) has already checked that the player is in town. Choosing the role one already has is
        // AlreadyProcessed; choosing None clears it. Only the host's role is written to the file (D06).
        public TransactionResult TrySelectRole(PlayerId player, PlayerRole role, ulong requestId)
        {
            var key = (player, requestId, OpRole);
            if (processed.TryGetValue(key, out var replayed)) return replayed;
            if (!RoleIds.IsValid(role)) return Cache(key, TransactionResult.Reject(requestId, "InvalidTarget", revision));
            if (RoleOf(player) == role) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);

            var previous = RoleOf(player);
            Apply(player, role);
            roleRevision++;
            revision++;
            if (player.Value == 0 && !Persist())
            {
                Apply(player, previous);
                roleRevision--;
                revision--;
                return TransactionResult.Reject(requestId, "SaveFailed", revision);
            }
            var result = Cache(key, TransactionResult.Ok(requestId, revision));
            OnChanged?.Invoke();
            return result;
        }

        private void Apply(PlayerId player, PlayerRole role)
        {
            if (role == PlayerRole.None) roles.Remove(player);
            else roles[player] = role;
        }

        // A player left: their (session-only) role goes with them, so a rejoin never inherits or doubles a bonus.
        public void ForgetPlayer(PlayerId player)
        {
            if (player.Value == 0 || !roles.Remove(player)) return;
            roleRevision++;
            revision++;
            OnChanged?.Invoke();
        }

        public IReadOnlyDictionary<PlayerId, PlayerRole> Roles => roles;

        private TransactionResult Cache((PlayerId, ulong, byte) key, TransactionResult result)
        {
            processed[key] = result;
            return result;
        }

        // ---- persistence --------------------------------------------------------------------------------------------------

        public LivingWorldSaveData ExportLiving() => Snapshot();

        // Fail-closed: only ids the catalogs know, once each, with progress inside the template's range. Anything else is dropped (a slot
        // whose template is unknown or of the wrong kind is simply empty), so a tampered file cannot hand out a contract, a development or a
        // completed state the rules would not have produced. An absent record (older file) is the empty default.
        public bool RestoreLiving(LivingWorldSaveData data)
        {
            processed.Clear();
            usedEvidence.Clear(); usedEvidenceSet.Clear();
            development.Clear();
            roles.Remove(new PlayerId(0));
            order = default; sponsor = default; boardDay = 0; lastOrderId = ""; lastSponsorId = "";
            if (data == null || data.Version < 1 || data.Version > 1)
            {
                revision++; roleRevision++;
                OnChanged?.Invoke();
                return true;
            }

            boardDay = Math.Max(0, data.BoardDay);
            order = RestoreSlot(data.OrderTemplateId, data.OrderStatus, data.OrderProgress, ContractKind.FishOrder);
            sponsor = RestoreSlot(data.SponsorTemplateId, data.SponsorStatus, data.SponsorProgress, ContractKind.VideoSponsor);
            lastOrderId = KnownTemplate(data.LastOrderTemplateId, ContractKind.FishOrder) ? data.LastOrderTemplateId : "";
            lastSponsorId = KnownTemplate(data.LastSponsorTemplateId, ContractKind.VideoSponsor) ? data.LastSponsorTemplateId : "";
            if (data.UsedEvidenceIds != null)
                foreach (var id in data.UsedEvidenceIds)
                    if (!string.IsNullOrWhiteSpace(id) && id.Length <= 256) AddEvidence(id);
            if (data.DevelopmentIds != null)
                foreach (var id in data.DevelopmentIds)
                    if (DevelopmentCatalog.TryGet(id, out _) && !development.Contains(id)) development.Add(id);
            var hostRole = (PlayerRole)data.HostRole;
            if (RoleIds.IsValid(hostRole) && hostRole != PlayerRole.None) roles[new PlayerId(0)] = hostRole;
            revision++; roleRevision++;
            OnChanged?.Invoke();
            return true;
        }

        private static bool KnownTemplate(string id, ContractKind kind) =>
            !string.IsNullOrEmpty(id) && ContractCatalog.TryGet(id, out var t) && t.Kind == kind;

        private static Slot RestoreSlot(string templateId, byte status, int progress, ContractKind kind)
        {
            if (!KnownTemplate(templateId, kind)) return default;
            ContractCatalog.TryGet(templateId, out var template);
            var s = status >= (byte)ContractStatus.Active && status <= (byte)ContractStatus.Expired ? (ContractStatus)status : ContractStatus.Active;
            var p = Math.Max(0, Math.Min(template.Target, progress));
            // A completed record must be a full one; a full one is completed (the rules never leave progress at target and Active).
            if (s == ContractStatus.Completed) p = template.Target;
            else if (p >= template.Target) s = ContractStatus.Completed;
            return new Slot { TemplateId = templateId, Status = s, Progress = p };
        }

        private LivingWorldSaveData Snapshot()
        {
            var data = new LivingWorldSaveData
            {
                Version = 1,
                BoardDay = boardDay,
                OrderTemplateId = order.TemplateId ?? "", OrderStatus = (byte)order.Status, OrderProgress = order.Progress,
                SponsorTemplateId = sponsor.TemplateId ?? "", SponsorStatus = (byte)sponsor.Status, SponsorProgress = sponsor.Progress,
                LastOrderTemplateId = lastOrderId, LastSponsorTemplateId = lastSponsorId,
                UsedEvidenceIds = new List<string>(usedEvidence),
                DevelopmentIds = new List<string>(development),
                HostRole = (byte)RoleOf(new PlayerId(0))
            };
            return data;
        }

        // Undo a failed write without events: only the board/evidence parts a step can touch.
        private void Rollback(LivingWorldSaveData snapshot, int snapshotRevision)
        {
            boardDay = snapshot.BoardDay;
            order = new Slot { TemplateId = snapshot.OrderTemplateId, Status = (ContractStatus)snapshot.OrderStatus, Progress = snapshot.OrderProgress };
            sponsor = new Slot { TemplateId = snapshot.SponsorTemplateId, Status = (ContractStatus)snapshot.SponsorStatus, Progress = snapshot.SponsorProgress };
            lastOrderId = snapshot.LastOrderTemplateId;
            lastSponsorId = snapshot.LastSponsorTemplateId;
            usedEvidence.Clear(); usedEvidenceSet.Clear();
            foreach (var id in snapshot.UsedEvidenceIds) { usedEvidence.Add(id); usedEvidenceSet.Add(id); }
            revision = snapshotRevision;
        }
    }
}
