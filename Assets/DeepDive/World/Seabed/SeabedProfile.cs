using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // The natural sea bed of DiveTestArea as plain maths (#219): one source for the terrain mesh the editor setup
    // writes, the host's fish bounds and the tests, so the three cannot drift apart. No scene, no physics query.
    //
    // Depth profile along z (surface y = 8; bands: shallow y 8..0, reef y 0..-12, deep y -12..-27):
    //   z -15..-4.14  y 5.2         sand apron, level with the top of the wade and ramp toes
    //   z -4.14..2.5  y 5.2 -> 0.5  the beach slope
    //   z 2.5..15     y 0.5 -> 0.2  shallow sand
    //   z 15..25      y 0.2 -> -8   reef slope
    //   z 25..36      y -8          reef shelf
    //   z 36..56.5    y -8 -> -26   deep slope
    //   z 56.5..65    y -26         basin (34 m, 1 m inside the 35 m edge; nothing reachable is deeper)
    public static class SeabedProfile
    {
        public const float MinFloorY = -26f;
        public const float BerthApronY = 3.5f;
        public const float ToeTopY = 5.2f;

        // A kit piece that carries a collider: where it stands and the box it gets, from ReefKitPiece.ColliderHint
        // scaled to the placement. A positive Lift raises the box off the bed (the arch's top span).
        public readonly struct Solid
        {
            public readonly string KitId;
            public readonly float Scale, X, Z, Lift;
            public readonly Vector3 Size;

            public Solid(string kitId, float scale, float x, float z, Vector3 size, float lift = 0f)
            {
                KitId = kitId;
                Scale = scale;
                X = x;
                Z = z;
                Size = size;
                Lift = lift;
            }

            // Sunk so a sloped bed leaves no gap under the piece.
            public Bounds Box
            {
                get
                {
                    var bottom = FloorHeight(X, Z) - BedOverlap + Lift;
                    return new Bounds(new Vector3(X, bottom + Size.y * 0.5f, Z), Size);
                }
            }
        }

        private const float BedOverlap = 0.3f;

        // (z, y) of the centre line.
        private static readonly Vector2[] Profile =
        {
            new Vector2(-15f, ToeTopY), new Vector2(-4.14f, ToeTopY), new Vector2(2.5f, 0.5f), new Vector2(15f, 0.2f),
            new Vector2(25f, -8f), new Vector2(36f, -8f), new Vector2(56.5f, MinFloorY), new Vector2(65f, MinFloorY)
        };

        // Rock grown out of the floor where the three reef cubes stood (x, z, radius, height).
        private static readonly Vector4[] Outcrops =
        {
            new Vector4(-8f, 24f, 4.5f, 3.5f), new Vector4(-5f, 35f, 3.2f, 3f), new Vector4(-12f, 31f, 3f, 2f),
            new Vector4(10f, 22f, 3f, 2.5f)
        };

        private const string Boulder = "reef-rock-boulder", Branching = "reef-coral-branching", Arch = "reef-rock-arch";

        public static readonly IReadOnlyList<Solid> Solids = new[]
        {
            new Solid(Boulder, 2f, -10.5f, 21f, new Vector3(3.6f, 1.2f, 2.6f)),
            new Solid(Boulder, 1.5f, -5.2f, 26.5f, new Vector3(2.7f, 0.9f, 2f)),
            new Solid(Branching, 1f, -2.8f, 28.5f, new Vector3(0.9f, 1f, 0.9f)),
            new Solid(Branching, 1f, -11f, 25.5f, new Vector3(0.9f, 1f, 0.9f)),
            new Solid(Arch, 1.5f, -9.6f, 30f, new Vector3(1.8f, 3.5f, 1.8f)),
            new Solid(Arch, 1.5f, -4.4f, 30f, new Vector3(1.8f, 3.5f, 1.8f)),
            new Solid(Arch, 1.5f, -7f, 30f, new Vector3(7f, 0.8f, 1.8f), 2.7f),
            new Solid(Boulder, 1.5f, -13f, 37f, new Vector3(2.7f, 0.9f, 2f)),
            new Solid(Branching, 1f, -2.8f, 33f, new Vector3(0.9f, 1f, 0.9f)),
            new Solid(Boulder, 2.5f, -4.3f, 39.5f, new Vector3(4.5f, 1.5f, 3.25f)),
            new Solid(Boulder, 2f, 9.5f, 44f, new Vector3(3.6f, 1.2f, 2.6f)),
            new Solid(Boulder, 2.5f, 12f, 61f, new Vector3(4.5f, 1.5f, 3.25f)),
            new Solid(Boulder, 1f, 10f, 50f, new Vector3(1.8f, 0.6f, 1.3f)),
            new Solid(Boulder, 1f, -12.5f, -8f, new Vector3(1.8f, 0.6f, 1.3f)),
            new Solid(Boulder, 1f, 12.5f, -3f, new Vector3(1.8f, 0.6f, 1.3f))
        };

        public static float FloorHeight(float x, float z)
        {
            var y = Profile[Profile.Length - 1].y;
            for (var i = 0; i + 1 < Profile.Length; i++)
            {
                if (z > Profile[i + 1].x) continue;
                var t = Mathf.InverseLerp(Profile[i].x, Profile[i + 1].x, z);
                // Eases the kinks without changing the end heights.
                t = t * t * (3f - 2f * t) * 0.35f + t * 0.65f;
                y = Mathf.Lerp(Profile[i].y, Profile[i + 1].y, t);
                break;
            }

            // The berth keeps its depth: east of the ramp the apron stops lower.
            y = Mathf.Min(y, Mathf.Lerp(ToeTopY, BerthApronY, Mathf.InverseLerp(3f, 7f, x)));

            // Sand swells, small over the shallow floor where the fish envelope sits close above.
            var amplitude = z < 15f ? 0.05f : z < 25f ? 0.1f : 0.3f;
            y += (Mathf.Sin(x * 0.61f + z * 0.23f) * 0.6f + Mathf.Sin(x * 0.17f - z * 0.47f + 1.3f) * 0.4f) * amplitude;

            foreach (var outcrop in Outcrops)
            {
                var d = new Vector2(x - outcrop.x, z - outcrop.y).magnitude / outcrop.z;
                if (d < 1f) y += outcrop.w * (1f - d * d) * (1f - d * d);
            }

            return Mathf.Max(y, MinFloorY);
        }
    }
}
