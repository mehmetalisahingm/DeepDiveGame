using System;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Network
{
    // P4.4-A (#121): one networked mirror around the transient boss encounter authority.
    // Existing NetworkPlayer harpoon raycasts already target IHarpoonTarget, so no second combat path exists.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BossEncounterActor : NetworkBehaviour, IHarpoonTarget
    {
        [SerializeField] private string bossId = DeepProgressionIds.BossId;
        [SerializeField] private string encounterId = "encounter-deep-1";
        [SerializeField] private float maxHealth = 24f;

        private readonly NetworkVariable<byte> phase = new NetworkVariable<byte>();
        private readonly NetworkVariable<float> health = new NetworkVariable<float>();
        private readonly NetworkVariable<int> revision = new NetworkVariable<int>();

        private BossEncounterAuthority authority;
        private ulong completionSequence = 0xB055000000000000UL;

        public string BossId => bossId;
        public string EncounterId => encounterId;
        public BossEncounterPhase Phase => (BossEncounterPhase)phase.Value;
        public float Health => health.Value;
        public int Revision => revision.Value;
        public float MaxHealth => maxHealth;
        public BossEncounterSnapshot Snapshot => new BossEncounterSnapshot(Phase, Health, MaxHealth, Revision);

        public event Action<BossEncounterActor> Completed;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EnsureAuthority();
                RefreshProgressionServer();
            }
            BossEncounterRuntime.Bind(this);
        }

        public override void OnNetworkDespawn()
        {
            BossEncounterRuntime.Unbind(this);
            if (IsServer && authority != null) authority.Abort();
            authority = null;
        }

        private void Update()
        {
            if (IsSpawned && IsServer)
                RefreshProgressionServer();
        }

        public PlayerActionResult TryActivateServer(string requestedEncounterId)
        {
            if (!IsSpawned || !IsServer)
                return PlayerActionResult.InvalidState;

            EnsureAuthority();
            RefreshProgressionServer();
            var result = authority.TryActivate(requestedEncounterId);
            Mirror();
            return result;
        }

        public bool AbortServer()
        {
            if (!IsSpawned || !IsServer || authority == null)
                return false;
            var changed = authority.Abort();
            if (changed) Mirror();
            return changed;
        }

        public PlayerActionResult TryApplyHarpoonHit(HarpoonHit hit)
        {
            if (!IsSpawned || !IsServer)
                return PlayerActionResult.Rejected;

            EnsureAuthority();
            RefreshProgressionServer();
            var result = authority.TryApplyHarpoonHit(hit);
            if (result != PlayerActionResult.Accepted)
                return result;

            if (authority.DefeatedPendingPersistence)
            {
                var completion = DeepProgressionEvidence.TryCompleteBoss(
                    bossId, encounterId, ++completionSequence);

                if (completion.Accepted ||
                    string.Equals(completion.ReasonCode, "AlreadyProcessed", StringComparison.Ordinal))
                {
                    if (authority.MarkCompletionPersisted())
                        Completed?.Invoke(this);
                }
                else
                {
                    // Save/unbound/progression failure: keep encounter active and make completion retryable.
                    authority.RestoreAfterFailedCompletion();
                }
            }

            Mirror();
            return result;
        }

        private void EnsureAuthority()
        {
            if (authority != null) return;
            authority = new BossEncounterAuthority(bossId, encounterId, maxHealth);
        }

        private void RefreshProgressionServer()
        {
            if (!IsServer || authority == null) return;
            var changed = authority.RefreshProgression(
                BossProgression.IsAvailable(bossId),
                BossProgression.IsCompleted(bossId));
            if (changed) Mirror();
        }

        private void Mirror()
        {
            if (!IsServer || authority == null) return;
            phase.Value = (byte)authority.Phase;
            health.Value = authority.Health;
            revision.Value = authority.Revision;
        }
    }

    public readonly struct BossEncounterSnapshot
    {
        public readonly BossEncounterPhase Phase;
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly int Revision;

        public BossEncounterSnapshot(BossEncounterPhase phase, float health, float maxHealth, int revision)
        {
            Phase = phase;
            Health = health;
            MaxHealth = maxHealth;
            Revision = revision;
        }

        public bool IsVisible => Phase == BossEncounterPhase.Available || Phase == BossEncounterPhase.Active;
    }

    // One host-side encounter seam shared by the network carrier and Utku's local world target.
    // The default is fail-closed; a scene/session binding must explicitly bind the delegates.
    public static class BossEncounterRuntime
    {
        private static object owner;
        private static Func<string, PlayerActionResult> activate;
        private static Func<HarpoonHit, PlayerActionResult> hit;
        private static Func<bool> abort;
        private static Func<BossEncounterSnapshot> snapshot;

        public static bool IsBound => owner != null;
        public static BossEncounterSnapshot Snapshot =>
            snapshot != null ? snapshot() : new BossEncounterSnapshot(BossEncounterPhase.Locked, 0f, 0f, 0);

        internal static void Bind(BossEncounterActor actor)
        {
            if (actor == null) return;
            Bind(actor, actor.TryActivateServer, actor.TryApplyHarpoonHit, actor.AbortServer, () => actor.Snapshot);
        }

        public static void Bind(object nextOwner,
            Func<string, PlayerActionResult> activateHandler,
            Func<HarpoonHit, PlayerActionResult> hitHandler,
            Func<bool> abortHandler,
            Func<BossEncounterSnapshot> snapshotProvider)
        {
            if (nextOwner == null || activateHandler == null || hitHandler == null || abortHandler == null || snapshotProvider == null)
                return;
            owner = nextOwner;
            activate = activateHandler;
            hit = hitHandler;
            abort = abortHandler;
            snapshot = snapshotProvider;
        }

        internal static void Unbind(BossEncounterActor actor) => Unbind((object)actor);

        public static void Unbind(object priorOwner)
        {
            if (!ReferenceEquals(owner, priorOwner)) return;
            owner = null;
            activate = null;
            hit = null;
            abort = null;
            snapshot = null;
        }

        public static PlayerActionResult TryActivate(string encounterId) =>
            activate != null ? activate(encounterId) : PlayerActionResult.InvalidState;

        public static PlayerActionResult TryHit(HarpoonHit harpoonHit) =>
            hit != null ? hit(harpoonHit) : PlayerActionResult.InvalidState;

        public static bool Abort() => abort != null && abort();
    }
}
