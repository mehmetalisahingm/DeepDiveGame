using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // Swim rules for one fish: wander near home, run from the nearest diver, never leave the
    // water. Pure state object like FishHealth, so the maths is tested without NGO or a scene.
    // The host is the only caller (docs/plan/CONTRACTS.md: results-bearing decisions are the
    // host's); clients just receive the replicated transform.
    public sealed class FishMotion
    {
        private const float ArriveDistance = 0.6f;
        private const float MinLookAhead = 0.75f;
        private const float RepathSeconds = 4f;
        private const float VerticalWanderFactor = 0.5f;
        private const int WanderAttempts = 8;
        private const float Epsilon = 0.0001f;

        private readonly SwimTuning tuning;
        private readonly System.Random random;
        private readonly Vector3 home;
        private Vector3 wanderTarget;
        private float repathTimer;

        public FishMotion(SwimTuning tuning, Vector3 home, System.Random random)
        {
            this.tuning = tuning;
            this.home = IsFinite(home) ? home : Vector3.zero;
            this.random = random ?? new System.Random();
            wanderTarget = this.home;
        }

        public Vector3 Home => home;
        public Vector3 WanderTarget => wanderTarget;
        public bool IsFleeing { get; private set; }

        // Returns where the fish should be after deltaTime. Never returns a point outside the
        // water: when the only way forward would leave it, the fish holds position instead.
        public Vector3 Step(Vector3 position, IReadOnlyList<Vector3> threats, float deltaTime, IWaterBounds water)
        {
            if (!IsFinite(position) || !IsFinite(deltaTime) || deltaTime <= 0f) return position;

            IsFleeing = TryFindNearestThreat(position, threats, tuning.FleeRadius, out var threat);
            var speed = IsFleeing ? tuning.FleeSpeed : tuning.SwimSpeed;
            var direction = IsFleeing ? FleeDirection(position, threat) : WanderDirection(position, deltaTime, water);
            if (direction == Vector3.zero) return position;

            // Turn before touching the wall rather than after: probe a point ahead and steer
            // home if it is already outside. This is what keeps the fish off the boundary.
            var lookAhead = Mathf.Max(MinLookAhead, speed * deltaTime * 4f);
            if (water != null && !water.Contains(position + direction * lookAhead))
            {
                direction = Direction(position, home);
                if (direction == Vector3.zero) return position;
            }

            var next = position + direction * (speed * deltaTime);
            if (water != null && !water.Contains(next)) return position;
            return next;
        }

        // Nearest diver inside the radius. Ties do not matter; any nearest one is a fine escape
        // reference. Non-finite entries are skipped rather than poisoning the comparison.
        public static bool TryFindNearestThreat(Vector3 position, IReadOnlyList<Vector3> threats,
            float radius, out Vector3 nearest)
        {
            nearest = Vector3.zero;
            if (threats == null || threats.Count == 0 || !IsFinite(radius) || radius <= 0f) return false;

            var bestDistance = radius * radius;
            var found = false;
            for (var i = 0; i < threats.Count; i++)
            {
                var threat = threats[i];
                if (!IsFinite(threat)) continue;
                var distance = (threat - position).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                nearest = threat;
                found = true;
            }
            return found;
        }

        // Straight away from the diver. A diver standing exactly on the fish would give a zero
        // vector, so fall back to a fixed heading instead of dividing by zero.
        public static Vector3 FleeDirection(Vector3 position, Vector3 threat)
        {
            var away = Direction(threat, position);
            return away == Vector3.zero ? Vector3.forward : away;
        }

        private Vector3 WanderDirection(Vector3 position, float deltaTime, IWaterBounds water)
        {
            repathTimer -= deltaTime;
            if (repathTimer <= 0f || (wanderTarget - position).sqrMagnitude <= ArriveDistance * ArriveDistance)
                PickWanderTarget(water);
            return Direction(position, wanderTarget);
        }

        // Keeps wandering inside both the home radius and the water. Falls back to home, which
        // is where the fish was placed, so a bad roll never parks the target on dry land.
        private void PickWanderTarget(IWaterBounds water)
        {
            repathTimer = RepathSeconds;
            for (var attempt = 0; attempt < WanderAttempts; attempt++)
            {
                var candidate = home + RandomOffset() * tuning.WanderRadius;
                if (water == null || water.Contains(candidate))
                {
                    wanderTarget = candidate;
                    return;
                }
            }
            wanderTarget = home;
        }

        private Vector3 RandomOffset()
        {
            // Rejection sampling for an even spread inside the sphere; flattened vertically so
            // fish drift sideways instead of bobbing up and down.
            for (var attempt = 0; attempt < WanderAttempts; attempt++)
            {
                var offset = new Vector3(NextUnit(), NextUnit() * VerticalWanderFactor, NextUnit());
                if (offset.sqrMagnitude <= 1f) return offset;
            }
            return new Vector3(NextUnit(), 0f, NextUnit()).normalized * 0.5f;
        }

        private float NextUnit() => (float)(random.NextDouble() * 2d - 1d);

        private static Vector3 Direction(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            return delta.sqrMagnitude <= Epsilon * Epsilon ? Vector3.zero : delta.normalized;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
