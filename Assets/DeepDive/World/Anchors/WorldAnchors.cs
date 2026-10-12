using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World
{
    // The pure selection rule behind WorldAnchors.TryFind: exactly one ordinal match or nothing.
    // A duplicated id is an authoring error, and picking "the first" would make the answer depend
    // on hierarchy order, so it fails closed instead.
    public static class WorldAnchorRules
    {
        public static bool TrySelectUnique(IReadOnlyList<string> candidateIds, string id, out int index)
        {
            index = -1;
            if (candidateIds == null || string.IsNullOrWhiteSpace(id)) return false;
            for (var i = 0; i < candidateIds.Count; i++)
            {
                if (!string.Equals(candidateIds[i], id, StringComparison.Ordinal)) continue;
                if (index >= 0)
                {
                    index = -1;
                    return false;
                }
                index = i;
            }
            return index >= 0;
        }
    }

    // Scene shell for anchor lookups. Reads every loaded scene and only active anchors; it never
    // looks at scene names, so the same lookup works in the integrated build and in an additive test.
    public static class WorldAnchors
    {
        public static bool TryFind(string id, out WorldAnchor anchor)
        {
            anchor = null;
            if (string.IsNullOrWhiteSpace(id)) return false;

            var found = new List<WorldAnchor>();
            for (var s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                // GetComponentsInChildren always searches the object it is called on, even an
                // inactive root, so activity is checked explicitly.
                foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<WorldAnchor>(false))
                    if (candidate.gameObject.activeInHierarchy) found.Add(candidate);
            }

            var ids = new string[found.Count];
            for (var i = 0; i < found.Count; i++) ids[i] = found[i].Id;
            if (!WorldAnchorRules.TrySelectUnique(ids, id, out var index)) return false;
            anchor = found[index];
            return true;
        }

        public static bool TryGetPose(string id, out Pose pose)
        {
            pose = default;
            if (!TryFind(id, out var anchor)) return false;
            pose = new Pose(anchor.transform.position, anchor.transform.rotation);
            return true;
        }
    }
}
