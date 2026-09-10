using System;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.World
{
    // The collectable dead fish. P2-A raycasts from the diver on the host and calls TryPickup
    // on the resolved component (NetworkPlayer.FindTarget<ICatchPickupTarget>), so this object
    // is the pickup target itself.
    //
    // It shares its NetworkObject with the FishActor that produced it: the dead fish becomes
    // the catch in place. That keeps CatchObjectId a real, already-existing network object id
    // at the moment the capture is built, and needs no entry in the shared network prefab list.
    // If the team later wants a separate catch prefab, only the caller of TryBecomeCatch
    // changes; the pickup rules below stay as they are.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CatchObject : NetworkBehaviour, ICatchPickupTarget
    {
        private readonly CatchState state = new CatchState();

        // Replicated so clients can show the catch as collectable; the host is the only writer.
        private readonly NetworkVariable<bool> available = new NetworkVariable<bool>();

        // Host-side notification, raised only after a bag has accepted the catch.
        public event Action<CatchObject, CaptureResult> Claimed;

        public bool IsAvailable => available.Value;
        public bool IsClaimed => state.IsClaimed;
        public CaptureResult Capture => state.Capture;

        // Called by FishActor on death. Builds the capture with this object's own network id so
        // CatchObjectId points at the thing a diver can actually pick up.
        public bool TryBecomeCatch(string speciesId, int weightGrams, out CaptureResult capture)
        {
            capture = default;
            if (!IsSpawned || !IsServer) return false;
            if (!CaptureBuilder.TryCreate(DiveContext.Source, speciesId, weightGrams, NetworkObjectId, out capture))
                return false;
            if (!state.Hold(capture)) return false;

            available.Value = true;
            return true;
        }

        public PlayerActionResult TryPickup(PlayerId playerId, ulong requestId)
        {
            // IsSpawned is checked first: reading IsServer off an unspawned behaviour depends on
            // a live NetworkManager. Only the host decides whether a catch leaves the ground.
            if (!IsSpawned || !IsServer) return PlayerActionResult.Rejected;

            var result = state.TryClaim(CatchClaim.Sink, playerId, requestId, out var consume);
            if (consume) Consume(playerId);
            else if (result != PlayerActionResult.Accepted)
                Debug.Log($"P2_CATCH_REFUSED capture={state.Capture.CaptureId} player={playerId} result={result}");
            return result;
        }

        // Reached only when the bag already accepted the catch, so nothing is lost by removing
        // it here. A refused pickup never gets this far and the catch stays on the ground.
        private void Consume(PlayerId player)
        {
            var capture = state.Capture;
            available.Value = false;
            Debug.Log($"P2_CATCH_TAKEN capture={capture.CaptureId} dive={capture.DiveId} " +
                      $"species={capture.SpeciesId} grams={capture.WeightGrams} player={player}");
            Claimed?.Invoke(this, capture);
            if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }
    }
}
