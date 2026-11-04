using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.World.Editor
{
    // #219: replaces DiveTestArea's flat floor boxes with one natural sea bed, built from SeabedProfile so the mesh,
    // the fish bounds and the tests read the same heights. Same region, same SwimVolume, same water line.
    //
    // Like the other dive area setups it destroys nothing: the old floor pieces are switched off, because the older
    // setups still read their material and retune them. The coast slabs, fills and toes, the walls, the routes and the
    // anchorages are not touched, so every walkable surface stays what it was. Byte-idempotent (ApplyAndVerify).
    public static class DiveTestAreaSeabedSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string MeshFolder = "Assets/DeepDive/World/Art/Seabed";

        public const string ShallowName = "Seabed_Shallow";
        public const string ReefName = "Seabed_Reef";
        public const string DeepName = "Seabed_Deep";

        public static readonly string[] OldFloor =
        {
            "SeaFloor", DiveTestAreaReefDeepSetup.ReefDropOffName, DiveTestAreaReefDeepSetup.ReefShelfName,
            DiveTestAreaReefDeepSetup.ReefHeadPrefix + "0", DiveTestAreaReefDeepSetup.ReefHeadPrefix + "1",
            DiveTestAreaReefDeepSetup.ReefHeadPrefix + "2", DiveTestAreaReefDeepSetup.DeepDropOffName,
            DiveTestAreaReefDeepSetup.DeepBasinName
        };

        private const int MinX = -15, MaxX = 15;
        private const float NormalStep = 0.5f;

        // Whole metres of z, and each chunk ends where the next starts, so the seams share their heights.
        private static readonly (string Name, int FromZ, int ToZ)[] Chunks =
        {
            (ShallowName, -15, 15), (ReefName, 15, 36), (DeepName, 36, 65)
        };

        [MenuItem("DeepDive/P4.11/Dive test area: natural sea bed")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var oldFloor = OldFloor.Select(name => RequireRoot(scene, name)).ToArray();
            var material = oldFloor[0].GetComponent<Renderer>().sharedMaterial;
            if (material == null) throw new InvalidOperationException("P4_SEABED_NO_MATERIAL piece=SeaFloor");

            foreach (var (name, fromZ, toZ) in Chunks) EnsureChunk(scene, name, fromZ, toZ, material);
            foreach (var piece in oldFloor) piece.SetActive(false);
            Physics.SyncTransforms();
            VerifyBed(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P4_SEABED_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log("P4_SEABED_READY chunks=" + string.Join(",", Chunks.Select(c => c.Name)));
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P4_SEABED_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P4_SEABED_SETUP_IDEMPOTENT");
        }

        private static void EnsureChunk(Scene scene, string name, int fromZ, int toZ, Material material)
        {
            var mesh = EnsureMesh(name, fromZ, toZ);
            var piece = FindRoot(scene, name);
            if (piece == null)
            {
                piece = new GameObject(name);
                SceneManager.MoveGameObjectToScene(piece, scene);
            }

            piece.layer = 0;
            piece.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            piece.transform.localScale = Vector3.one;
            Ensure<MeshFilter>(piece).sharedMesh = mesh;
            Ensure<MeshRenderer>(piece).sharedMaterial = material;
            var collider = Ensure<MeshCollider>(piece);
            collider.sharedMesh = mesh;
            collider.convex = false;
            if (piece.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException($"P4_SEABED_NETWORKED object={name}");
        }

        // Rewrites the mesh in place so a repeat run keeps the asset and the scene's reference to it.
        private static Mesh EnsureMesh(string name, int fromZ, int toZ)
        {
            var path = $"{MeshFolder}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var created = mesh == null;
            if (created)
            {
                Directory.CreateDirectory(MeshFolder);
                mesh = new Mesh { name = name };
            }

            var columns = MaxX - MinX + 1;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (var z = fromZ; z <= toZ; z++)
            for (var x = MinX; x <= MaxX; x++)
            {
                vertices.Add(new Vector3(x, SeabedProfile.FloorHeight(x, z), z));
                normals.Add(FloorNormal(x, z));
            }

            for (var row = 0; row < toZ - fromZ; row++)
            for (var column = 0; column < columns - 1; column++)
            {
                int a = row * columns + column, b = a + 1, c = a + columns, d = c + 1;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // From the heights rather than the triangles, so the shared rows of two chunks shade as one surface.
        private static Vector3 FloorNormal(float x, float z)
        {
            var dx = SeabedProfile.FloorHeight(x + NormalStep, z) - SeabedProfile.FloorHeight(x - NormalStep, z);
            var dz = SeabedProfile.FloorHeight(x, z + NormalStep) - SeabedProfile.FloorHeight(x, z - NormalStep);
            return new Vector3(-dx, NormalStep * 2f, -dz).normalized;
        }

        // What a repeat of the old boxes would have to get wrong: a column with no floor, or a floor off the profile.
        private static void VerifyBed(Scene scene)
        {
            var pieces = Chunks.Select(c => RequireRoot(scene, c.Name).GetComponent<MeshCollider>()).ToArray();
            var worst = 0f;
            for (var x = MinX + 0.25f; x < MaxX; x += 0.5f)
            for (var z = -14.75f; z < 65f; z += 0.5f)
            {
                var hit = default(RaycastHit);
                var found = pieces.Any(p => p.Raycast(new Ray(new Vector3(x, 12f, z), Vector3.down), out hit, 60f));
                if (!found) throw new InvalidOperationException($"P4_SEABED_HOLE x={x} z={z}");
                var expected = SeabedProfile.FloorHeight(x, z);
                var error = Mathf.Abs(hit.point.y - expected);
                worst = Mathf.Max(worst, error);
                if (error > MaxMeshError)
                    throw new InvalidOperationException($"P4_SEABED_OFF_PROFILE x={x} z={z} mesh={hit.point.y} profile={expected}");
            }

            Debug.Log($"P4_SEABED_MESH_ERROR worst={worst:0.000}");
        }

        // A 1 m grid cuts the outcrops' curves. Stays under the fish bounds' FloorMargin, so a fish on the profile's
        // limit is still above the mesh.
        public const float MaxMeshError = HabitatWaterBounds.FloorMargin - 0.1f;

        private static T Ensure<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            var root = FindRoot(scene, name);
            if (root == null) throw new InvalidOperationException($"P4_SEABED_MISSING name={name}");
            return root;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException($"P4_SEABED_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
