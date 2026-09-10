using System;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.World
{
    // One live fish. Health, the death decision and the resulting CaptureResult are owned here
    // (docs/plan/CONTRACTS.md: "Canlilar ve dunya | Utku | ... AI, vurulma/olum, av nesnesi").
    //
    // P2-A finds this component by raycasting from the diver on the host and calls
    // TryApplyHarpoonHit directly (NetworkPlayer.FindTarget<IHarpoonTarget>), which is why no
    // target id travels in HarpoonHit: the resolved component is the target.
    //
    // Skeleton slice: no swimming/flee AI, no catch object spawn and no despawn yet. Death
    // produces the CaptureResult and raises Died; consuming it is the next slice.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class FishActor : NetworkBehaviour, IHarpoonTarget
    {
        [SerializeField] private SpeciesDefinition species;

        // Replicated for client-side hit feedback later; the host is the only writer.
        private readonly NetworkVariable<float> health = new NetworkVariable<float>();

        private FishHealth state;
        private int weightGrams;

        // Host-side notification. Raised once, only when a valid capture could be produced.
        public event Action<FishActor, CaptureResult> Died;

        public SpeciesDefinition Species => species;
        public float Health => health.Value;
        public int WeightGrams => weightGrams;
        public bool IsDead => state != null && state.IsDead;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            if (species == null)
            {
                Debug.LogError($"P2_FISH_INVALID object={name} reason=species is not assigned", this);
                return;
            }
            if (!species.IsValid(out var error))
            {
                Debug.LogError($"P2_FISH_INVALID object={name} reason={error}", this);
                return;
            }
            state = new FishHealth(species.MaxHealth);
            // Rolled once at spawn so the weight does not depend on when the fish dies.
            weightGrams = species.RollWeightGrams(new System.Random());
            health.Value = state.Health;
        }

        public PlayerActionResult TryApplyHarpoonHit(HarpoonHit hit)
        {
            // IsSpawned is checked first: reading IsServer off an unspawned behaviour depends on
            // a live NetworkManager. Health is host-authoritative, so a client never applies one.
            if (!IsSpawned || !IsServer) return PlayerActionResult.Rejected;
            if (state == null) return PlayerActionResult.InvalidTarget;

            switch (state.ApplyDamage(hit.PlayerId, hit.RequestId, hit.Damage))
            {
                case FishHitOutcome.Duplicate:
                    return PlayerActionResult.DuplicateRequest;
                case FishHitOutcome.AlreadyDead:
                    return PlayerActionResult.InvalidTarget;
                case FishHitOutcome.Ignored:
                    return PlayerActionResult.Rejected;
                case FishHitOutcome.Applied:
                    health.Value = state.Health;
                    return PlayerActionResult.Accepted;
                case FishHitOutcome.Killed:
                    health.Value = state.Health;
                    Die(hit.PlayerId);
                    return PlayerActionResult.Accepted;
                default:
                    return PlayerActionResult.Rejected;
            }
        }

        private void Die(PlayerId killer)
        {
            // The catch object does not exist yet, so the fish's own network object id stands in
            // for catchObjectId. It becomes the spawned catch's id in the next slice.
            if (!CaptureBuilder.TryCreate(DiveContext.Source, species.SpeciesId, weightGrams,
                    NetworkObjectId, out var capture))
            {
                // Killed outside a live dive: nothing Mert's bag could accept, so no capture is
                // invented. The fish is still dead; it just leaves nothing to pick up.
                Debug.LogWarning($"P2_FISH_DEAD_NO_DIVE object={name} species={species.SpeciesId} killer={killer}", this);
                return;
            }

            Debug.Log($"P2_FISH_DEAD capture={capture.CaptureId} dive={capture.DiveId} " +
                      $"species={capture.SpeciesId} grams={capture.WeightGrams} killer={killer}");
            Died?.Invoke(this, capture);
        }
    }
}
