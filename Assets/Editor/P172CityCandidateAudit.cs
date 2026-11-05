using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #172/#153: walks the proposed far-city layout and sizes the shop door with the real diver controller, in a
    // throwaway empty scene. Nothing is saved: no layout, scene, prefab or service position changes. The ground is
    // flat, so the times are locomotion only; turning, stopping, UI, network and frame time are not in them.
    public static class P172CityCandidateAudit
    {
        public const string OutputPath = "docs/reports/evidence/p172-city/city-candidate.json";

        private const float Dt = 0.02f;
        private const float GroundY = 8.4f;

        // Q03 candidate B (docs/reports/P4-Q03-SCALE.md), with the equipment shop now behind a door on the square.
        private static readonly Vector2 HomeSpawn = new Vector2(-1.5f, -337.5f);
        private static readonly Vector2[] HomeToSquare =
        {
            new Vector2(-4, -330), new Vector2(-4, -318), new Vector2(6, -295), new Vector2(-10, -265),
            new Vector2(-20, -232), new Vector2(-14, -200), new Vector2(0, -170), new Vector2(8, -140),
            new Vector2(8, -122)
        };
        private static readonly Vector2 ShopDoor = new Vector2(-2, -112);
        private static readonly Vector2 ShopShelf = new Vector2(-8, -113);
        private static readonly Vector2 ShopCash = new Vector2(-8, -109);
        private static readonly Vector2[] SquareToBuyer = { new Vector2(8, -108), new Vector2(-4, -96) };
        private static readonly Vector2[] BuyerToGate =
        {
            new Vector2(-4, -96), new Vector2(-6, -80), new Vector2(-11, -56), new Vector2(-6, -34),
            new Vector2(0, -15), new Vector2(0, -13.5f)
        };
        private static readonly Vector2[] GateToPier =
        {
            new Vector2(0, -13.5f), new Vector2(7, -13), new Vector2(9, -9), new Vector2(9, -3.7f)
        };

        [Serializable] public sealed class Walk { public string id; public float metres, seconds; public bool stuck; }
        [Serializable] public sealed class Door
        {
            public string test; public float width, depth; public bool allThrough; public float seconds; public int blockedFrames;
        }
        [Serializable] public sealed class Step { public float height; public bool climbed; }
        [Serializable] public sealed class Audit
        {
            public string note = "Empty throwaway scene, flat ground, real diver controller values. Not a played result.";
            public float radius, height, skin, stepOffset, slopeLimit, walkSpeed;
            public List<Walk> walks = new List<Walk>();
            public List<Door> doors = new List<Door>();
            public List<Step> steps = new List<Step>();
        }

        private static float radius, height, skin, stepOffset, slope, speed;

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            radius = diver.radius; height = diver.height; skin = diver.skinWidth;
            stepOffset = diver.stepOffset; slope = diver.slopeLimit;
            speed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue;
            var audit = new Audit
            {
                radius = radius, height = height, skin = skin, stepOffset = stepOffset, slopeLimit = slope, walkSpeed = speed
            };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                Box("Ground", new Vector3(0f, GroundY - 0.5f, -130f), new Vector3(200f, 1f, 480f));
                Physics.SyncTransforms();
                // A controller that hits nothing would pass every test below, so the geometry proves itself first.
                if (!Physics.Raycast(new Vector3(0f, GroundY + 2f, 60f), Vector3.down, 5f))
                    throw new InvalidOperationException("Ground is not in the physics scene.");
                WalkRoutes(audit);

                foreach (var depth in new[] { 0.3f, 4f })
                    foreach (var width in new[] { 0.6f, 0.8f, 0.9f, 1f, 1.2f, 1.5f, 1.8f, 2.2f, 2.6f, 3f })
                    {
                        audit.doors.Add(Crossing("fourEnter", width, depth, true));
                        audit.doors.Add(Crossing("twoInTwoOut", width, depth, false));
                    }
                foreach (var rise in new[] { 0.05f, 0.1f, 0.2f, 0.25f, 0.3f, 0.35f, 0.5f })
                    audit.steps.Add(new Step { height = rise, climbed = ClimbsStep(rise) });

                if (audit.steps.Last().climbed) throw new InvalidOperationException("A 0.5 m step was climbed: nothing collides.");
                if (audit.doors.Where(d => d.width < 0.65f).Any(d => d.allThrough))
                    throw new InvalidOperationException("A door narrower than the diver was passed: nothing collides.");
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P172_CITY_AUDIT_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void WalkRoutes(Audit audit)
        {
            var toSquare = new List<Vector2> { HomeSpawn };
            toSquare.AddRange(HomeToSquare);
            var shopVisit = new List<Vector2> { new Vector2(8, -122), ShopDoor, ShopShelf, ShopCash, ShopDoor, new Vector2(8, -108) };
            var homeToPier = toSquare.Concat(SquareToBuyer).Concat(BuyerToGate).Concat(GateToPier).ToList();
            var shopping = toSquare.Concat(shopVisit).Concat(new[] { SquareToBuyer[1] }).Concat(BuyerToGate).Concat(GateToPier).ToList();
            var sale = BuyerToGate.Concat(GateToPier).ToList();

            audit.walks.Add(Drive("home-spawn -> square (H part)", toSquare));
            audit.walks.Add(Drive("square -> shop door -> shelf -> cash -> door -> square", shopVisit));
            audit.walks.Add(Drive("buyer -> shore gate (C)", BuyerToGate.ToList()));
            audit.walks.Add(Drive("buyer -> pier (one way of a sale)", sale));
            audit.walks.Add(Drive("direct: home-spawn -> pier", homeToPier));
            audit.walks.Add(Drive("shopping: home-spawn -> shop inside -> pier", shopping));
        }

        private static Walk Drive(string id, List<Vector2> path)
        {
            var cc = Diver(path[0]);
            var walk = new Walk { id = id };
            try
            {
                var frames = 0;
                for (var i = 1; i < path.Count && !walk.stuck; i++)
                {
                    var guard = Mathf.CeilToInt(Vector2.Distance(path[i - 1], path[i]) / (speed * Dt)) * 3 + 100;
                    while (Vector2.Distance(XZ(cc), path[i]) > 0.05f)
                    {
                        if (--guard < 0) { walk.stuck = true; break; }
                        var before = XZ(cc);
                        MoveToward(cc, path[i]);
                        walk.metres += Vector2.Distance(before, XZ(cc));
                        frames++;
                    }
                }
                walk.seconds = frames * Dt;
            }
            finally { Object.DestroyImmediate(cc.gameObject); }
            return walk;
        }

        // A wall across z = 0 with one opening, and four divers. Each keeps to the right of the opening when they
        // meet someone: no stopping, no human give-way, so a pass here is cautious evidence and a fail may be a jam
        // a person could still talk through. Depth 0.3 is a door; depth 4 is an aisle between shelves.
        private static Door Crossing(string test, float width, float depth, bool allEnter)
        {
            const float z0 = 40f;
            var built = new List<GameObject>
            {
                Box("WallL", new Vector3(-width * 0.5f - 15f, GroundY + 1.5f, z0), new Vector3(30f, 3f, depth)),
                Box("WallR", new Vector3(width * 0.5f + 15f, GroundY + 1.5f, z0), new Vector3(30f, 3f, depth))
            };
            Physics.SyncTransforms();

            var divers = new List<(CharacterController Cc, Vector2[] Path)>();
            for (var i = 0; i < 4; i++)
            {
                var row = allEnter || i < 2;
                var side = i % 2 == 0 ? -1f : 1f;
                var lane = allEnter ? 0f : (row ? -1f : 1f) * Mathf.Min(width * 0.25f, 0.6f);
                var start = new Vector2(side * 0.75f, row ? z0 - 5f - (allEnter ? (i / 2) * 1.2f : 0f) : z0 + 5f);
                var end = new Vector2(side * 2.5f, row ? z0 + 6f + (allEnter ? (i / 2) * 1.2f : 0f) : z0 - 6f);
                var near = (depth * 0.5f + 0.6f) * (row ? -1f : 1f);
                divers.Add((Diver(start), new[]
                {
                    new Vector2(lane, z0 + near), new Vector2(lane, z0 - near), end
                }));
            }

            var result = new Door { test = test, width = width, depth = depth };
            try
            {
                var next = new int[4];
                var frames = 0;
                const int budget = 2000; // 40 s; a free walk takes about 4 s
                while (frames < budget && next.Any(n => n < 3))
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (next[i] >= 3) continue;
                        var cc = divers[i].Cc;
                        var target = divers[i].Path[next[i]];
                        var before = XZ(cc);
                        MoveToward(cc, target);
                        if (Vector2.Distance(before, XZ(cc)) < speed * Dt * 0.5f) result.blockedFrames++;
                        if (Vector2.Distance(XZ(cc), target) < 0.2f) next[i]++;
                    }
                    frames++;
                }
                result.allThrough = next.All(n => n >= 3);
                result.seconds = frames * Dt;
            }
            finally
            {
                foreach (var entry in divers) Object.DestroyImmediate(entry.Cc.gameObject);
                foreach (var go in built) Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            return result;
        }

        private static bool ClimbsStep(float rise)
        {
            const float z0 = 60f;
            var step = Box("Step", new Vector3(0f, GroundY + rise * 0.5f, z0 + 5f), new Vector3(10f, rise, 10f));
            Physics.SyncTransforms();
            var cc = Diver(new Vector2(0f, z0 - 2f));
            try
            {
                var target = new Vector2(0f, z0 + 2f);
                for (var i = 0; i < 300 && Vector2.Distance(XZ(cc), target) > 0.1f; i++) MoveToward(cc, target);
                return Vector2.Distance(XZ(cc), target) <= 0.1f;
            }
            finally
            {
                Object.DestroyImmediate(cc.gameObject);
                Object.DestroyImmediate(step);
                Physics.SyncTransforms();
            }
        }

        private static CharacterController Diver(Vector2 at)
        {
            var go = new GameObject("Audit_P172_City_Diver");
            var cc = go.AddComponent<CharacterController>();
            cc.height = height; cc.radius = radius; cc.center = new Vector3(0f, height * 0.5f, 0f);
            cc.slopeLimit = slope; cc.stepOffset = stepOffset; cc.skinWidth = skin;
            go.transform.position = new Vector3(at.x, GroundY + skin, at.y);
            Physics.SyncTransforms();
            return cc;
        }

        private static void MoveToward(CharacterController cc, Vector2 target)
        {
            var step = Vector2.ClampMagnitude(target - XZ(cc), speed * Dt);
            cc.Move(new Vector3(step.x, -1f * Dt, step.y));
        }

        private static Vector2 XZ(CharacterController cc) =>
            new Vector2(cc.transform.position.x, cc.transform.position.z);

        private static GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }
    }
}
