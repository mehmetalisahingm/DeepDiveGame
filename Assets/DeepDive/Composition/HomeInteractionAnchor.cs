using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Composition
{
    public enum HomeInteractionKind : byte
    {
        Bed = 1,
        Storage = 2
    }

    // Scene-side identity only. It owns no sleep/day/storage state; it just gives the
    // host-authoritative interaction layer a stable physical target and poses.
    [DisallowMultipleComponent]
    public sealed class HomeInteractionAnchor : MonoBehaviour
    {
        [SerializeField] private HomeInteractionKind kind = HomeInteractionKind.Bed;
        [SerializeField] private string bedId = "";
        [SerializeField] private Vector3 sleepLocalOffset = new Vector3(0f, 0.55f, 0f);
        [SerializeField] private Vector3 exitLocalOffset = new Vector3(0f, 0.15f, -1.45f);

        public HomeInteractionKind Kind => kind;
        public string BedId => bedId;
        public Vector3 WorldPosition => transform.position;
        public Pose SleepPose => new Pose(transform.TransformPoint(sleepLocalOffset), transform.rotation);
        public Pose ExitPose => new Pose(transform.TransformPoint(exitLocalOffset), transform.rotation);

        public void ConfigureBed(string stableBedId, Vector3 sleepOffset, Vector3 exitOffset)
        {
            kind = HomeInteractionKind.Bed;
            bedId = DayIds.IsBed(stableBedId) ? stableBedId : string.Empty;
            sleepLocalOffset = sleepOffset;
            exitLocalOffset = exitOffset;
        }

        public void ConfigureStorage()
        {
            kind = HomeInteractionKind.Storage;
            bedId = string.Empty;
        }

        public bool IsValid => kind == HomeInteractionKind.Storage || DayIds.IsBed(bedId);

        public static HomeInteractionAnchor FromCollider(Collider collider) =>
            collider != null ? collider.GetComponentInParent<HomeInteractionAnchor>() : null;
    }
}
