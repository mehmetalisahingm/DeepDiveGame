using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    // #258: one-off read-only probe of the ramp -> beach-platform seam where a returning diver stalled at (6.00, 8.06, -10.58).
    // Prints the walkable height just north and just south of the seam across x, and the step between them.
    public static class P258RampSeamProbe
    {
        public const string OutputPath = "docs/reports/evidence/p258-boss-route/ramp-seam.csv";

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                Physics.SyncTransforms();
                var platform = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                    .First(c => c.name == "Beach_Platform").bounds;
                var seamZ = platform.max.z;
                var csv = new StringBuilder("x,seamZ,northY,northCollider,southY,southCollider,step\n");
                for (var x = 0f; x <= 11.01f; x += 0.25f)
                {
                    var north = Hit(x, seamZ + 0.3f, out var northName);
                    var south = Hit(x, seamZ - 0.3f, out var southName);
                    csv.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0:0.00},{1:0.00},{2:0.000},{3},{4:0.000},{5},{6:0.000}", x, seamZ, north, northName, south, southName, south - north));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, csv.ToString());
                Debug.Log("P258_RAMP_SEAM_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static float Hit(float x, float z, out string name)
        {
            name = "(none)";
            if (!Physics.Raycast(new Vector3(x, 20f, z), Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) return float.NaN;
            name = hit.collider.name;
            return hit.point.y;
        }
    }
}
