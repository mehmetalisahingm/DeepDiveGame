using System;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.World.Editor
{
    // P4.8-B (#188): authors the repair-site anchors on the beach, as decided in #186/#188 (S1):
    // the broken boat-1 lies on a cradle on dry Beach_Platform instead of at the water berth,
    // where only two people fit and the pier end closes (docs/reports/P48-REPAIR-AREA-188.md).
    //
    // Places only. Every anchor is an empty transform with a WorldAnchor (no collider, renderer or
    // NetworkObject); the broken hull, its slots and its collider are drawn by
    // Composition's BoatRepairNetworkBinding at the cradle pose. Acquisition, reservation and
    // completion stay with the existing repair/economy authority, and the repaired boat-1 still
    // floats at route-near-1 wp0: the cradle is a presentation pose, not a vehicle pose.
    //
    // Same rules as the other World setups: never destroys anything, creates a missing piece and
    // retunes an existing one in place, byte-idempotent (ApplyAndVerify).
    public static class DiveTestAreaRepairCradleSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string RootName = "P48_RepairAnchors";

        // S1 (#188 decision, Mehmet 19 Oct): beach east, hull parallel to the shore, stern west.
        public static readonly Vector3 CradlePosition = new Vector3(3.6f, 8.4f, -12.6f);
        public const float CradleYaw = 90f;

        // Work points in the cradle's frame (x across the hull, +x = south at yaw 90; z along it, stern at
        // -z). The slots sit across the stern (PR #190: local x -0.65 / 0 / 0.65, z -2.15). Work i serves
        // slot i: 0 and 1 behind the stern, 2 on the south side by the stern; 3 is the fourth crew place
        // further along the south side, still in reach of slot 2. Eye-to-slot <= 1.4 m (reach 2.5),
        // >= 1.06 m apart (0.86 m padded capsule + 0.2 m), 0.45-0.5 m off the 2.4 x 5 m hull rim. Nothing
        // stands behind the south half of the stern: that is where the town gate meets the sand (0, -13.5).
        public static readonly Vector3[] WorkLocal =
        {
            new Vector3(-1.06f, 0f, -3.0f),
            new Vector3(0f, 0f, -3.0f),
            new Vector3(1.65f, 0f, -1.6f),
            new Vector3(1.65f, 0f, -0.4f)
        };

        // #192: dry, outside the SwimVolume, free, >= 1.31 m off every corridor, walked clean from the
        // work points, the cradle and on to dock-town-1.
        public static readonly Vector3 DropPosition = new Vector3(7.5f, 8.4f, -14.5f);

        public const float WorkerSpacing = 1.06f;
        public const float RimClearance = 0.45f;

        public static Vector3 WorkPosition(int index) =>
            CradlePosition + Quaternion.Euler(0f, CradleYaw, 0f) * WorkLocal[index];

        [MenuItem("DeepDive/P4/Dive test area: repair cradle anchors (#188)")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            }
            root.layer = 0;
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            if (root.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException("P48_CRADLE_NETWORKED object=" + RootName);

            Ensure(root.transform, RepairAnchorIds.Cradle, CradlePosition, CradleYaw);
            for (var i = 0; i < RepairAnchorIds.WorkCount; i++)
                Ensure(root.transform, RepairAnchorIds.Work(i), WorkPosition(i), CradleYaw);
            Ensure(root.transform, RepairAnchorIds.Drop, DropPosition, 0f);
            Physics.SyncTransforms();

            VerifyCradleOnDryBeach();
            VerifyStandable();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P48_CRADLE_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log($"P48_CRADLE_READY cradle={CradlePosition} yaw={CradleYaw} drop={DropPosition} " +
                      "work=" + string.Join(" ", Enumerable.Range(0, RepairAnchorIds.WorkCount).Select(i => WorkPosition(i).ToString())));
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P48_CRADLE_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P48_CRADLE_SETUP_IDEMPOTENT");
        }

        private static void Ensure(Transform root, string id, Vector3 position, float yaw)
        {
            var child = root.Find(id);
            if (child == null)
            {
                child = new GameObject(id).transform;
                child.SetParent(root, false);
            }
            child.gameObject.layer = 0;
            child.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            child.localScale = Vector3.one;
            var anchor = child.GetComponent<WorldAnchor>() ?? child.gameObject.AddComponent<WorldAnchor>();
            anchor.Configure(id);
            EditorUtility.SetDirty(anchor);
            if (child.GetComponents<Component>().Length != 2)
                throw new InvalidOperationException($"P48_CRADLE_ANCHOR_NOT_BARE id={id} reason=an anchor is a transform and a WorldAnchor only");
        }

        // --- Author-time guards ----------------------------------------------------------------

        // The whole hull footprint lies on dry Beach_Platform, outside the SwimVolume, and nothing
        // solid but the beach itself stands inside the low hull box the binding gives it.
        private static void VerifyCradleOnDryBeach()
        {
            if (!BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size))
                throw new InvalidOperationException("P48_CRADLE_NO_ROWBOAT_SIZE");
            var rotation = Quaternion.Euler(0f, CradleYaw, 0f);
            for (var a = -size.Width * 0.5f; a <= size.Width * 0.5f + 1e-4f; a += 0.25f)
            for (var b = -size.Length * 0.5f; b <= size.Length * 0.5f + 1e-4f; b += 0.25f)
            {
                var q = CradlePosition + rotation * new Vector3(a, 0f, b);
                var hit = Physics.RaycastAll(new Vector3(q.x, 12f, q.z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => !RelocatedAtRunTime(h.collider)).OrderBy(h => h.distance).FirstOrDefault();
                if (hit.collider == null || hit.collider.name != DiveTestAreaBeachSetup.PlatformName || Mathf.Abs(hit.point.y - CradlePosition.y) > 0.01f)
                    throw new InvalidOperationException($"P48_CRADLE_NOT_ON_BEACH at={q} hit={(hit.collider != null ? hit.collider.name : "none")}");
                if (InSwimVolume(hit.point + Vector3.up * 0.05f))
                    throw new InvalidOperationException($"P48_CRADLE_IN_WATER at={hit.point}");
            }
            var solids = Physics.OverlapBox(CradlePosition + Vector3.up * 0.25f, new Vector3(size.Width * 0.5f, 0.2f, size.Length * 0.5f), rotation,
                ~0, QueryTriggerInteraction.Ignore).Where(c => c.name != DiveTestAreaBeachSetup.PlatformName && !RelocatedAtRunTime(c)).Select(c => c.name).ToArray();
            if (solids.Length > 0)
                throw new InvalidOperationException("P48_CRADLE_OVERLAPS " + string.Join(",", solids));
        }

        // Every work point and the drop: a real-size diver capsule fits on dry ground there, the
        // work points keep the rim clearance and the worker spacing, the drop is outside the work band.
        private static void VerifyStandable()
        {
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var points = Enumerable.Range(0, RepairAnchorIds.WorkCount).Select(WorkPosition).Append(DropPosition).ToArray();
            for (var i = 0; i < points.Length; i++)
            {
                var feet = points[i];
                if (!Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 1f, ~0, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(ground.point.y - feet.y) > 0.01f || InSwimVolume(feet + Vector3.up * 0.05f))
                    throw new InvalidOperationException($"P48_CRADLE_POINT_NOT_DRY index={i} at={feet}");
                var blockers = Physics.OverlapCapsule(feet + Vector3.up * (0.35f + 0.08f), feet + Vector3.up * (1.8f - 0.35f), 0.35f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !RelocatedAtRunTime(c)).Select(c => c.name).ToArray();
                if (blockers.Length > 0)
                    throw new InvalidOperationException($"P48_CRADLE_POINT_BLOCKED index={i} at={feet} by={string.Join(",", blockers)}");
                var local = Quaternion.Inverse(Quaternion.Euler(0f, CradleYaw, 0f)) * (feet - CradlePosition);
                var rim = new Vector2(Mathf.Max(0f, Mathf.Abs(local.x) - size.Width * 0.5f),
                    Mathf.Max(0f, Mathf.Abs(local.z) - size.Length * 0.5f)).magnitude;
                if (i < RepairAnchorIds.WorkCount && rim < RimClearance - 1e-3f)
                    throw new InvalidOperationException($"P48_CRADLE_WORK_ON_RIM index={i} rim={rim}");
                for (var j = 0; j < i && i < RepairAnchorIds.WorkCount; j++)
                    if (Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(points[j].x, points[j].z)) < WorkerSpacing - 1e-3f)
                        throw new InvalidOperationException($"P48_CRADLE_WORKERS_TOO_CLOSE a={j} b={i}");
            }
        }

        // The P3 service NPCs still stand on this beach in the saved scene; ContinuousTownLayout moves their
        // ServicePointAnchors to the storefronts on every load. P48RepairCradleTests checks the cradle against
        // the built town.
        private static bool RelocatedAtRunTime(Collider c) => c.GetComponentInParent<ServicePointAnchor>(true) != null;

        private static bool InSwimVolume(Vector3 point) =>
            UnityEngine.Object.FindObjectsByType<SwimVolume>(FindObjectsSortMode.None)
                .Select(s => s.GetComponent<BoxCollider>()).Any(b => b != null && b.bounds.Contains(point));

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"P48_CRADLE_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
