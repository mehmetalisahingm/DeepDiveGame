using System;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Inventory;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.4-A/B integration: the persistent session object carries the ONE transient boss state.
    // World targets are ordinary local colliders; only the host can activate/hit this authority.
    // Clients receive a read-only snapshot through a named NGO message, so no second boss NetworkObject/prefab is required.
    [DisallowMultipleComponent]
    public sealed class BossEncounterSessionBinding : MonoBehaviour
    {
        public const string StateMessage = "deepdive/p4/boss-state-v1";
        public const string EncounterId = "encounter-deep-1";
        public const float DefaultMaxHealth = 24f;

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private BossEncounterAuthority authority;
        private BossEncounterSnapshot mirror =
            new BossEncounterSnapshot(BossEncounterPhase.Locked, 0f, DefaultMaxHealth, 0);
        private int lastBroadcastRevision = int.MinValue;
        private int receivedRevision = -1;
        private double nextBroadcast;
        private ulong completionSequence = 0xB055100000000000UL;
        private InventoryManager inventory;
        private string pendingDefeatDiveId = string.Empty;

        public BossEncounterSnapshot Snapshot => mirror;
        private bool IsHost => adapter != null && manager != null && manager.IsListening && adapter.IsAuthority;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<BossEncounterSessionBinding>() == null)
                session.gameObject.AddComponent<BossEncounterSessionBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null)
            {
                enabled = false;
                return;
            }

            inventory = GetComponent<InventoryManager>();
            if (inventory != null) inventory.OnDiveSummaryReady += DiveSummaryReady;
            BossEncounterRuntime.Bind(this, TryActivate, TryHit, Abort, () => mirror);
        }

        private void Update()
        {
            EnsureMessages();
            if (manager == null || !manager.IsListening)
            {
                ResetTransient();
                return;
            }

            if (!IsHost) return;
            EnsureAuthority();
            authority.RefreshProgression(
                BossProgression.IsAvailable(DeepProgressionIds.BossId),
                BossProgression.IsCompleted(DeepProgressionIds.BossId));
            MirrorAuthority();

            var now = Time.unscaledTimeAsDouble;
            if (mirror.Revision != lastBroadcastRevision || now >= nextBroadcast)
            {
                nextBroadcast = now + 0.25;
                Broadcast();
            }
        }

        private void EnsureMessages()
        {
            var current = manager != null ? manager.CustomMessagingManager : null;
            if (current == messages) return;
            UnregisterMessages();
            messages = current;
            if (messages != null)
                messages.RegisterNamedMessageHandler(StateMessage, StateReceived);
        }

        private void UnregisterMessages()
        {
            if (messages == null) return;
            messages.UnregisterNamedMessageHandler(StateMessage);
            messages = null;
        }

        private void EnsureAuthority()
        {
            if (authority != null) return;
            authority = new BossEncounterAuthority(DeepProgressionIds.BossId, EncounterId, DefaultMaxHealth);
            authority.RefreshProgression(
                BossProgression.IsAvailable(DeepProgressionIds.BossId),
                BossProgression.IsCompleted(DeepProgressionIds.BossId));
            MirrorAuthority();
        }

        private PlayerActionResult TryActivate(string requestedEncounterId)
        {
            if (!IsHost) return PlayerActionResult.InvalidState;
            EnsureAuthority();
            authority.RefreshProgression(
                BossProgression.IsAvailable(DeepProgressionIds.BossId),
                BossProgression.IsCompleted(DeepProgressionIds.BossId));
            var result = authority.TryActivate(requestedEncounterId);
            if (result == PlayerActionResult.Accepted) pendingDefeatDiveId = string.Empty;
            MirrorAuthority();
            Broadcast();
            return result;
        }

        private PlayerActionResult TryHit(HarpoonHit hit)
        {
            if (!IsHost) return PlayerActionResult.InvalidState;
            EnsureAuthority();
            authority.RefreshProgression(
                BossProgression.IsAvailable(DeepProgressionIds.BossId),
                BossProgression.IsCompleted(DeepProgressionIds.BossId));

            var result = authority.TryApplyHarpoonHit(hit);
            if (result != PlayerActionResult.Accepted)
            {
                MirrorAuthority();
                return result;
            }

            // Lethal damage stays transient until InventoryManager emits the real safe-return DiveSummary.
            // Host loss / failed return therefore cannot persist a boss clear.
            if (authority.DefeatedPendingPersistence)
                pendingDefeatDiveId = adapter != null ? adapter.Session.State.DiveId : string.Empty;

            MirrorAuthority();
            Broadcast();
            return result;
        }

        private void DiveSummaryReady(DiveSummary summary)
        {
            if (!IsHost || authority == null || !authority.DefeatedPendingPersistence ||
                string.IsNullOrWhiteSpace(pendingDefeatDiveId) ||
                !string.Equals(summary.DiveId, pendingDefeatDiveId, StringComparison.Ordinal))
                return;

            if (summary.SafelyReturned == null || summary.SafelyReturned.Count == 0)
            {
                authority.Abort();
            }
            else
            {
                var durableEncounterId = EncounterId + ":" + (summary.CheckpointId ?? string.Empty);
                var completion = DeepProgressionEvidence.TryCompleteBoss(
                    DeepProgressionIds.BossId, durableEncounterId, ++completionSequence);
                if (completion.Accepted ||
                    string.Equals(completion.ReasonCode, "AlreadyProcessed", StringComparison.Ordinal))
                    authority.MarkCompletionPersisted();
                else
                    authority.Abort();
            }

            pendingDefeatDiveId = string.Empty;
            MirrorAuthority();
            Broadcast();
        }

        private bool Abort()
        {
            if (!IsHost || authority == null) return false;
            pendingDefeatDiveId = string.Empty;
            var changed = authority.Abort();
            if (changed)
            {
                MirrorAuthority();
                Broadcast();
            }
            return changed;
        }

        private void MirrorAuthority()
        {
            if (authority == null) return;
            mirror = new BossEncounterSnapshot(authority.Phase, authority.Health, authority.MaxHealth, authority.Revision);
        }

        private void Broadcast()
        {
            if (!IsHost || messages == null) return;
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe((byte)mirror.Phase);
            writer.WriteValueSafe(mirror.Health);
            writer.WriteValueSafe(mirror.MaxHealth);
            writer.WriteValueSafe(mirror.Revision);
            foreach (var clientId in manager.ConnectedClientsIds)
                if (clientId != NetworkManager.ServerClientId)
                    messages.SendNamedMessage(StateMessage, clientId, writer);
            lastBroadcastRevision = mirror.Revision;
        }

        private void StateReceived(ulong sender, FastBufferReader reader)
        {
            if (IsHost || sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out byte phaseValue);
            reader.ReadValueSafe(out float health);
            reader.ReadValueSafe(out float maxHealth);
            reader.ReadValueSafe(out int revision);
            if (phaseValue > (byte)BossEncounterPhase.Completed || revision < receivedRevision ||
                float.IsNaN(health) || float.IsInfinity(health) || float.IsNaN(maxHealth) ||
                float.IsInfinity(maxHealth) || maxHealth <= 0f || health < 0f || health > maxHealth)
                return;
            receivedRevision = revision;
            mirror = new BossEncounterSnapshot((BossEncounterPhase)phaseValue, health, maxHealth, revision);
        }

        private void ResetTransient()
        {
            if (authority != null) authority.Abort();
            authority = null;
            pendingDefeatDiveId = string.Empty;
            receivedRevision = -1;
            lastBroadcastRevision = int.MinValue;
            mirror = new BossEncounterSnapshot(BossEncounterPhase.Locked, 0f, DefaultMaxHealth, 0);
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnDiveSummaryReady -= DiveSummaryReady;
            inventory = null;
            BossEncounterRuntime.Unbind(this);
            UnregisterMessages();
            ResetTransient();
        }

        private void OnDestroy()
        {
            BossEncounterRuntime.Unbind(this);
            UnregisterMessages();
        }
    }
}
