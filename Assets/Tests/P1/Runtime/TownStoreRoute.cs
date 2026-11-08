using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #249: the equipment shop is a real building with one door on the main street, so a smoke driver can no longer walk the straight line to the
    // shop's customer point. Next() answers "where do I steer now" for a walk that starts or ends inside the store: out through the door and the
    // aisle round the display table, or in the same way. Everything that does not touch the store comes back unchanged (target in, target out).
    // The legs are the ones ContinuousTownWalkTests and P4RouteAudit already prove walkable.
    public static class TownStoreRoute
    {
        private static Vector3 Outside => ContinuousTownLayout.EquipmentStoreDoorOutside;
        private static Vector3 Inside => ContinuousTownLayout.EquipmentStoreDoorInside;
        private static Vector3 AisleNear => ContinuousTownLayout.EquipmentStoreAisleNear;
        private static Vector3 AisleFar => ContinuousTownLayout.EquipmentStoreAisleFar;
        private static Vector3 NearFor(int lane) => (lane & 1) == 0 ? AisleNear : new Vector3(2f * Inside.x - AisleNear.x, AisleNear.y, AisleNear.z);
        private static Vector3 FarFor(int lane) => (lane & 1) == 0 ? AisleFar : new Vector3(2f * Inside.x - AisleFar.x, AisleFar.y, AisleFar.z);
        private static Vector3 StreetMiddle => new Vector3(0f, Outside.y, Outside.z);

        // Past this z (the counter side of the table) the walker needs no more detour: the table is 1 m deep and ends at AisleFar.z + 1.2.
        private static float SouthZone => AisleFar.z + 0.7f;
        private const float Half = 5.4f;   // half the store width plus the wall
        private static float Line => Inside.z + 1.8f;   // the store's front wall: north of it the walker is on the street in front of the store, south of it inside, or beside and behind the building

        // Slow down near a physical waypoint. Four headless Unity processes can
        // have long Update frames while the server continues fixed movement;
        // a full-speed command through that interval overshoots short turns.
        // This only changes smoke-driver steering, not the player speed,
        // authoritative movement, collision, or the 1.5 m QA failure threshold.
        public static float ApproachThrottle(float metres) => Mathf.Clamp01(Mathf.Max(0f, metres) / 2.5f);

        public static bool InStore(Vector3 p)
        {
            var back = ContinuousTownLayout.StorefrontPositions[0].z - 1.3f;
            return Mathf.Abs(p.x - Inside.x) <= Half && p.z >= back && p.z <= Line;
        }

        public static Vector3 Next(Vector3 position, Vector3 target, int lane = 0)
        {
            var from = InStore(position);
            var to = InStore(target);
            if (!from && !to)
            {
                // Outside to outside: only the walker leaving the door line towards the south (the home, the east side) has to clear the store first.
                if (position.z > Line && position.x < -1.5f && target.z < Line && target.x > Inside.x + Half - 0.3f) return Flat(StreetMiddle, position);   // up the street lane, not across the store corner
                return target;
            }
            if (to && !from)
            {
                if (position.z < Line)
                {
                    // South of the store line (the home side, or beside the building): on to the street in front of the door by the lane, never across the
                    // walls or the stalls. Behind the stalls first sideways to the lane.
                    if (position.z < -30.6f && Mathf.Abs(position.x) > 1.0f) return new Vector3(0f, position.y, position.z);
                    return Flat(StreetMiddle, position);
                }
                if (Mathf.Abs(position.x - Outside.x) > 0.6f && DistanceToPath(position, lane) > 0.9f) return Flat(Outside, position);
                return Along(position, target, true, lane);
            }
            if (from && to)
            {
                // Both in the store: straight when the table is not in the way (door side to a spot in front of the table, counter side to the
                // counter), otherwise round it, door side to counter side or back.
                if (!HitsTable(position, target)) return target;
                if (position.z <= SouthZone && target.z <= SouthZone) return target;
                if (position.z >= Inside.z - 0.1f && target.z >= Inside.z - 0.1f) return target;
                return Along(position, target, target.z < position.z, lane);
            }
            // Inside to outside: back along the aisle and out of the door, then on to the target.
            return Along(position, target, false, lane);
        }

        // The display table in the middle of the store (store frame: 2.4 x 1.0 m at x 0, z 3.0, facing the door), grown by a walker's body.
        private static bool HitsTable(Vector3 a, Vector3 b)
        {
            var centre = new Vector2(Inside.x, ContinuousTownLayout.StorefrontPositions[0].z + 4.15f);   // z -24.85
            var half = new Vector2(1.2f + 0.45f, 0.5f + 0.45f);
            for (var i = 0; i <= 24; i++)
            {
                var p = Vector3.Lerp(a, b, i / 24f);
                if (Mathf.Abs(p.x - centre.x) < half.x && Mathf.Abs(p.z - centre.y) < half.y) return true;
            }
            return false;
        }

        // The door path, entrance first: street in front of the door, just inside, the near and the far end of the aisle round the table.
        private static Vector3[] Path(int lane) => new[] { Outside, Inside, NearFor(lane), FarFor(lane) };

        private static float DistanceToPath(Vector3 p, int lane)
        {
            var path = Path(lane);
            var best = float.MaxValue;
            for (var i = 0; i + 1 < path.Length; i++) best = Mathf.Min(best, DistanceToSegment(p, path[i], path[i + 1]));
            return best;
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = new Vector2(b.x - a.x, b.z - a.z);
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(new Vector2(p.x - a.x, p.z - a.z), ab) / ab.sqrMagnitude);
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x + ab.x * t, a.z + ab.y * t));
        }

        // The next waypoint on the door path in the walking direction, from where the walker is now (stateless: the nearest segment decides, and a
        // point counts as reached when the walker is within 0.55 m of it).
        private static Vector3 Along(Vector3 position, Vector3 target, bool inwards, int lane)
        {
            var path = Path(lane);
            var far = FarFor(lane);
            if (inwards && position.z <= Inside.z - 0.1f && InStore(target) && !HitsTable(position, target)) return target;   // past the door: straight if the table is clear
            if (!inwards) System.Array.Reverse(path);
            if (inwards && position.z <= SouthZone) return target;
            if (!inwards && position.z <= SouthZone && ((lane & 1) == 0 ? position.x > far.x + 0.6f : position.x < far.x - 0.6f)) return Flat(far, position);   // counter side: to the aisle end first, then up the aisle
            var best = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i + 1 < path.Length; i++)
            {
                var d = DistanceToSegment(position, path[i], path[i + 1]);
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            var end = path[best + 1];
            // Keep the tighter clearance on entry (the display table is solid).
            // The exit needs a slightly wider corner window to prevent the
            // back-and-forth jitter observed with two real player capsules.
            var cornerReach = inwards ? 0.4f : 0.55f;
            if (new Vector2(position.x - end.x, position.z - end.z).magnitude < cornerReach)
            {
                if (best + 2 >= path.Length)
                    return inwards || !(target.z < Line && target.x > Inside.x + Half - 0.3f) ? target : Flat(StreetMiddle, position);   // out of the door: on south-east round the store
                end = path[best + 2];
            }
            return Flat(end, position);
        }

        // The store stocks several copies of one product (a shelf each side, the back wall), every copy an aim target for the same item. A driver
        // picks the copy nearest the door and stands one metre in front of it (a product's front faces the customer, i.e. -forward).
        public static ShopShelfAnchor Shelf(string itemId) =>
            Object.FindObjectsByType<ShopShelfAnchor>(FindObjectsSortMode.None).Where(s => s.ItemId == itemId)
                .OrderBy(s => new Vector2(s.transform.position.x - Inside.x, s.transform.position.z - Inside.z).sqrMagnitude).FirstOrDefault();

        public static Vector3 Stand(ShopShelfAnchor shelf)
        {
            var front = shelf.transform.forward; front.y = 0f;
            var stand = shelf.transform.position - front.normalized * 1.0f;
            stand.y = ContinuousTownLayout.WalkSurfaceY;
            return stand;
        }

        private static Vector3 Flat(Vector3 waypoint, Vector3 position) => new Vector3(waypoint.x, position.y, waypoint.z);
    }
}
