using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Composition
{
    // P4.11/#219: render-only micro-relief following the EXISTING authoritative
    // arena -> reef -> deep floor. No terrain collider, NGO object or save state.
    // It does not replace the underlying flat/slope collider. Physical terrain
    // work and actual CharacterController/AI clearance remain #219/#164.
    public sealed class NaturalSeabedContourMesh : MonoBehaviour
    {
        public readonly struct Band
        {
            public readonly string Name;
            public readonly float StartZ, EndZ, StartY, EndY;

            public Band(string name, float startZ, float endZ, float startY, float endY)
            {
                Name = name; StartZ = startZ; EndZ = endZ;
                StartY = startY; EndY = endY;
            }
        }

        // Mirrors the already-authored DiveTestAreaReefDeepSetup floor profile.
        // No alternate swim volume, new ground collision or change of depth band.
        public static readonly Band[] Bands =
        {
            new Band("ReefApproach", 15f, 19f, 0f, -8f),
            new Band("ReefShelf", 19f, 40f, -8f, -8f),
            new Band("DeepApproach", 40f, 46f, -8f, -26f),
            new Band("DeepBasin", 46f, 65f, -26f, -26f)
        };

        public const int WidthSegments = 30;
        public const float MinX = -15f;
        public const float MaxX = 15f;
        private Mesh ownedMesh;

        public static float HeightAt(Band band, float x, float z)
        {
            var t = Mathf.InverseLerp(band.StartZ, band.EndZ, z);
            var floor = Mathf.Lerp(band.StartY, band.EndY, t);
            // Edges are aligned with the old slope/floor surfaces: never expose
            // a step at segment boundaries or a ridge into the arena walls.
            var edgeFade = Mathf.Clamp01(Mathf.Min(x - MinX, MaxX - x) * 0.5f) *
                           Mathf.Clamp01(Mathf.Min(z - band.StartZ, band.EndZ - z) * 0.8f);
            var wave = Mathf.Sin(x * 1.31f + z * 0.71f) *
                       Mathf.Sin(x * 0.64f - z * 1.46f);
            var ripple = (0.075f + 0.060f * wave) * edgeFade;
            return floor + 0.018f + ripple;
        }

        public static Mesh Generate(Band band)
        {
            if (band.EndZ <= band.StartZ)
                throw new ArgumentException("Seabed band must have positive length");
            var zSegments = Mathf.Max(2, Mathf.CeilToInt(band.EndZ - band.StartZ));
            var rowSize = zSegments + 1;
            var vertices = new Vector3[(WidthSegments + 1) * rowSize];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[WidthSegments * zSegments * 6];
            for (var xi = 0; xi <= WidthSegments; xi++)
            {
                var x = Mathf.Lerp(MinX, MaxX, xi / (float)WidthSegments);
                for (var zi = 0; zi <= zSegments; zi++)
                {
                    var z = Mathf.Lerp(band.StartZ, band.EndZ, zi / (float)zSegments);
                    var index = xi * rowSize + zi;
                    vertices[index] = new Vector3(x, HeightAt(band, x, z), z);
                    uv[index] = new Vector2(x * 0.25f, z * 0.25f);
                    if (xi == WidthSegments || zi == zSegments) continue;
                    var b = index + rowSize;
                    var c = index + 1;
                    var dest = (xi * zSegments + zi) * 6;
                    // z cross x => upward normal on both shelf and slopes.
                    triangles[dest] = index; triangles[dest + 1] = c; triangles[dest + 2] = b;
                    triangles[dest + 3] = b; triangles[dest + 4] = c; triangles[dest + 5] = b + 1;
                }
            }
            var mesh = new Mesh { name = "P411_Seabed_" + band.Name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Configure(Band band, Material floorMaterial)
        {
            if (floorMaterial == null)
                throw new ArgumentNullException(nameof(floorMaterial));
            if (ownedMesh != null)
                throw new InvalidOperationException("Seabed segment configured twice");
            ownedMesh = Generate(band);
            gameObject.AddComponent<MeshFilter>().sharedMesh = ownedMesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = floorMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void OnDestroy()
        {
            if (ownedMesh == null) return;
            if (Application.isPlaying) Destroy(ownedMesh);
            else DestroyImmediate(ownedMesh);
            ownedMesh = null;
        }
    }
}
