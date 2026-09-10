using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    // Death hands the fish over to the CatchObject on the same NetworkObject, which builds the
    // capture with its own id and takes over as the pickup target.
    //
    // Swimming runs on the host only and moves the transform; NetworkTransform replicates the
    // result, so a client never runs the AI and never decides where a fish is.
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(CatchObject))]
    public sealed class FishActor : NetworkBehaviour, IHarpoonTarget
    {
        [SerializeField] private SpeciesDefinition species;
        [Tooltip("Which colliders may count as a diver. Divers are recognised by their CharacterController.")]
        [SerializeField] private LayerMask threatLayers = ~0;

        // Replicated for client-side hit feedback later; the host is the only writer.
        private readonly NetworkVariable<float> health = new NetworkVariable<float>();

        private readonly List<Vector3> threats = new List<Vector3>();
        private readonly Collider[] threatBuffer = new Collider[16];

        private FishHealth state;
        private FishMotion motion;
        private SwimTuning tuning;
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
            var random = new System.Random();
            state = new FishHealth(species.MaxHealth);
            // Rolled once at spawn so the weight does not depend on when the fish dies.
            weightGrams = species.RollWeightGrams(random);
            tuning = species.Swim;
            // Where the fish was placed is the home it wanders around.
            motion = new FishMotion(tuning, transform.position, random);
            health.Value = state.Health;
        }

        // Host-only. A dead fish is a catch lying on the ground, so it stops swimming the
        // moment it dies and simply stays where it fell.
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || motion == null || state == null || state.IsDead) return;

            GatherThreats();
            var position = transform.position;
            var next = motion.Step(position, threats, Time.fixedDeltaTime, SwimVolumeBounds.Instance);
            if (next == position) return;

            var heading = next - position;
            transform.position = next;
            // Only yaw/pitch toward travel when there is a horizontal component; a straight
            // vertical heading has no usable look rotation against world up.
            var flat = new Vector3(heading.x, 0f, heading.z);
            if (flat.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
        }

        // Divers are found by their CharacterController rather than by Mehmet's NetworkPlayer
        // type, so World does not reach into another module's gameplay classes to see them.
        private void GatherThreats()
        {
            threats.Clear();
            if (tuning.FleeRadius <= 0f) return;
            var count = Physics.OverlapSphereNonAlloc(transform.position, tuning.FleeRadius,
                threatBuffer, threatLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = threatBuffer[i];
                if (hit == null) continue;
                var diver = hit.GetComponentInParent<CharacterController>();
                if (diver == null) continue;
                threats.Add(diver.transform.position);
            }
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
            var catchObject = GetComponent<CatchObject>();
            if (catchObject == null)
            {
                Debug.LogError($"P2_FISH_DEAD_NO_CATCH object={name} reason=CatchObject is missing", this);
                return;
            }

            // The capture is built by the catch object so CatchObjectId is the id of the thing a
            // diver can actually pick up. Fails when no dive is live: nothing Mert's bag could
            // accept, so no capture is invented. The fish is still dead, it just leaves nothing.
            if (!catchObject.TryBecomeCatch(species.SpeciesId, weightGrams, out var capture))
            {
                Debug.LogWarning($"P2_FISH_DEAD_NO_DIVE object={name} species={species.SpeciesId} killer={killer}", this);
                return;
            }

            Debug.Log($"P2_FISH_DEAD capture={capture.CaptureId} dive={capture.DiveId} " +
                      $"species={capture.SpeciesId} grams={capture.WeightGrams} " +
                      $"catchObject={capture.CatchObjectId} killer={killer}");
            Died?.Invoke(this, capture);
        }
    }
}
