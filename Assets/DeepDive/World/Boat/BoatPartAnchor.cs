using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // Where one free boat repair part lies on the coast, and which part it is. That is the
    // whole component (docs/plan/CONTRACTS.md P3.2-C note 6: "Utku'nun dunya anchor'lari tam bu
    // kimlikleri kullanmali").
    //
    // It holds no claim state on purpose. Whether a part has been taken, what that does to
    // BoatRepairState, and whether it was found or bought are Mert's, and the call that starts
    // that - BoatPartClaim.TryClaimFound - belongs to Mehmet's interaction layer. Putting a
    // "collected" flag here would create a second copy of progress that the save file does not
    // know about, which is exactly the duplicate authority CONTRACTS.md forbids.
    //
    // What structurally keeps that state out of World:
    //   - DeepDive.World does not reference DeepDive.Economy or DeepDive.Inventory, so
    //     EconomyManager and InventoryManager cannot even be named from here.
    //   - No NetworkObject and no NetworkBehaviour, so there is nothing to replicate.
    //   - One serialized field, a string. An EditMode test pins that count.
    //
    // The one thing the assembly graph does not block is BoatPartClaim itself, which lives in
    // Core.Contracts and is therefore visible here. It is left to discipline and to the test
    // that scans this assembly's sources for the name; that is stated rather than dressed up
    // as a compiler guarantee.
    [DisallowMultipleComponent]
    public sealed class BoatPartAnchor : MonoBehaviour, IBoatPartPickup
    {
        [SerializeField] private string partId = "";

        public string PartId => partId;

        public Vector3 WorldPosition => transform.position;

        // The ids are Mehmet's constants, never retyped as literals: a typo here would leave a
        // part that no save could ever match, and the repair would look like a missing anchor.
        public bool IsContractPart => BoatRepairParts.IsPart(partId);

        // Editor setup only, the same way ServicePointAnchor.Configure is.
        public void Configure(string id)
        {
            partId = id ?? string.Empty;
        }

        // An empty id is a part that has not been authored yet and stays quiet. A non-empty id
        // that is not one of the three is a typo, and it is worth a hard error: the anchor will
        // still sit in the scene looking correct while being unclaimable.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(partId) || BoatRepairParts.IsPart(partId)) return;
            Debug.LogError(
                $"P3_BOATPART_UNKNOWN_ID object={name} partId={partId} " +
                "reason=must be one of BoatRepairParts.All", this);
        }
    }
}
