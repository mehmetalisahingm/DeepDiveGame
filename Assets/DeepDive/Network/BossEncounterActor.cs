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

    // Host-side seam for Utku's #122 arena trigger. There is deliberately no client RPC here:
    // world code activates/aborts the one live encounter only on the authoritative process.
    public static class BossEncounterRuntime
    {
        public static BossEncounterActor Current { get; private set; }
        public static bool IsBound => Current != null;

        internal static void Bind(BossEncounterActor actor)
        {
            if (actor != null) Current = actor;
        }

        internal static void Unbind(BossEncounterActor actor)
        {
            if (ReferenceEquals(Current, actor)) Current = null;
        }

        public static PlayerActionResult TryActivate(string encounterId) =>
            Current != null
                ? Current.TryActivateServer(encounterId)
                : PlayerActionResult.InvalidState;

        public static bool Abort() => Current != null && Current.AbortServer();
    }
}
