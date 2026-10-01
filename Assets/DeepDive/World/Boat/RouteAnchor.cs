using UnityEngine;

namespace DeepDive.World
{
    // Where one end of the near route physically is, and which end it is. That is the whole
    // component - the same shape BoatPartAnchor has, for the same reasons.
    //
    // It carries no trip state on purpose. Whether a boat is docked or anchored, who is aboard
    // and what an arrival means are Mert's BoatTripState; the movement that physically reaches
    // an anchor is Mehmet's and reports itself through BoatRouteProgress.ReportArrival. A
    // "boat is here" flag on this component would be a second copy of the trip phase that the
    // trip manager does not know about - the duplicate authority CONTRACTS.md forbids.
    //
    // No NetworkObject and no NetworkBehaviour: an anchor is authored scenery, identical on
    // every machine, so there is nothing to replicate.
    [DisallowMultipleComponent]
    public sealed class RouteAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId = "";
        [SerializeField] private Transform boardingPoint;

        public string AnchorId => anchorId;

        public Vector3 WorldPosition => transform.position;

        // The ids are constants, never retyped as literals: a typo here would leave an anchor
        // that no route could match, and the trip would look like a missing anchor instead of a
        // misspelt one.
        public bool IsContractAnchor => DiveRouteAnchors.IsAnchor(anchorId);

        // Where a player stands (at the dock) or floats (at the anchorage) to board. It is a
        // separate point because the two ends differ: the dock anchor already sits on the jetty
        // deck where a player walks, while the sea anchor IS the hull's own position, so boarding
        // there has to happen beside the hull rather than inside it.
        //
        // Left unset it falls back to the anchor itself, which is what the dock wants. That
        // fallback is why the boarding spot needs no id of its own, and so no new string reaches
        // the frozen contract or the save file.
        public Vector3 BoardingPosition =>
            boardingPoint != null ? boardingPoint.position : transform.position;

        // Editor setup only, the same way BoatPartAnchor.Configure and ServicePointAnchor.Configure
        // are. The scene script is the one caller.
        public void Configure(string id, Transform boarding = null)
        {
            anchorId = id ?? string.Empty;
            boardingPoint = boarding;
        }

        // An empty id is an anchor that has not been authored yet and stays quiet. A non-empty id
        // that is not one of the two is a typo, and it is worth a hard error: the anchor will sit
        // in the scene looking correct while no route can ever reach it.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(anchorId) || DiveRouteAnchors.IsAnchor(anchorId)) return;
            Debug.LogError(
                $"P3_ROUTE_UNKNOWN_ANCHOR object={name} anchorId={anchorId} " +
                "reason=must be one of DiveRouteAnchors.All", this);
        }
    }
}
