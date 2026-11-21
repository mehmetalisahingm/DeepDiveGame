using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    // #310 (Utku): how much easier the reference lengths make a harpoon hit. The harpoon is one server ray from the
    // eye (NetworkPlayer.HandleHarpoon), so a shot hits when that ray crosses the fish's collider. For one mid-weight
    // fish of each species this fires rays at the collider as authored (before #321) and again at the reference length
    // (#321), and with the capsule FishActor now fits round the drawn body (fitted), side-on and head-on, from 4, 8 and 12 m, with the aim scattered round the collider centre by a fixed
    // angular error. The scatter is an illustration of an unsteady hand, not a measured player. Nothing is saved.
    public static class P310FishHitAudit
    {
        public const string OutputPath = "docs/reports/evidence/p310-fish-length/hit-window.json";
        private static readonly float[] Distances = { 4f, 8f, 12f };
        private static readonly float[] ScatterDegrees = { 1f, 2f };
        private const int RaysPerCase = 4000;

        [Serializable] public sealed class Row
        {
            public string species, fish, view;
            public float factor, distance, scatterDegrees;
            public Vector3 colliderBefore, colliderAfter, colliderFitted, modelAfter;
            public float hitBefore, hitAfter, hitFitted, halfWindowDegBefore, halfWindowDegAfter, halfWindowDegFitted;
        }

        [Serializable] public sealed class Audit { public List<Row> rows = new List<Row>(); }

        public static void Run()
        {
            var audit = new Audit();
            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                var fishes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true))
                    .Where(f => f.Species != null && f.Species.ReferenceLengthMetres > 0f)
                    .GroupBy(f => f.Species.SpeciesId).Select(g => g.OrderBy(f => f.name).First()).ToArray();
                if (fishes.Length != 4) throw new InvalidOperationException("P310_HIT_SPECIES " + fishes.Length);
                foreach (var fish in fishes)
                {
                    var collider = fish.GetComponent<Collider>();
                    var factor = fish.ModelLengthFactor();
                    var authored = fish.transform.localScale;
                    var facing = fish.transform.rotation;
                    // Squared up to the world axes so the collider's bounds are its own size.
                    fish.transform.rotation = Quaternion.identity;
                    var before = Sample(fish, collider);
                    fish.transform.localScale = authored * factor;
                    var after = Sample(fish, collider);
                    // Mehmet's decision (21 Nov): the capsule FishActor now fits round the drawn body at runtime
                    // (FishActor.BodyColliderTolerance headroom), here at the reference length.
                    var capsule = (CapsuleCollider)collider;
                    var (radius, height, direction, center) = (capsule.radius, capsule.height, capsule.direction, capsule.center);
                    var model = ModelBounds(fish);
                    if (!FishActor.FittedCapsule(fish.transform, fish.GetComponentsInChildren<Renderer>(true), FishActor.BodyColliderTolerance,
                            out var fitCenter, out var fitRadius, out var fitHeight, out var fitDirection))
                        throw new InvalidOperationException("P310_HIT_NO_BODY " + fish.name);
                    (capsule.center, capsule.radius, capsule.height, capsule.direction) = (fitCenter, fitRadius, fitHeight, fitDirection);
                    var fitted = Sample(fish, collider);
                    (capsule.radius, capsule.height, capsule.direction, capsule.center) = (radius, height, direction, center);
                    fish.transform.localScale = authored;
                    fish.transform.rotation = facing;
                    for (var i = 0; i < before.Count; i++)
                    {
                        var row = before[i];
                        row.factor = factor;
                        row.colliderAfter = after[i].colliderBefore;
                        row.hitAfter = after[i].hitBefore;
                        row.halfWindowDegAfter = after[i].halfWindowDegBefore;
                        row.colliderFitted = fitted[i].colliderBefore;
                        row.hitFitted = fitted[i].hitBefore;
                        row.halfWindowDegFitted = fitted[i].halfWindowDegBefore;
                        row.modelAfter = model.size;
                        audit.rows.Add(row);
                    }
                }
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                foreach (var r in audit.rows.Where(r => r.distance == 8f))
                    Debug.Log($"P310_HIT {r.species} {r.view} d={r.distance} scatter={r.scatterDegrees} before={r.hitBefore:0.00} after={r.hitAfter:0.00} fitted={r.hitFitted:0.00} window={r.halfWindowDegBefore:0.00}->{r.halfWindowDegAfter:0.00}/{r.halfWindowDegFitted:0.00} model={r.modelAfter} collider={r.colliderAfter}");
                Debug.Log("P310_HIT_WRITTEN " + OutputPath + " rows=" + audit.rows.Count);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // The fish as it currently stands: hit share for every view, distance and scatter.
        private static List<Row> Sample(FishActor fish, Collider collider)
        {
            Physics.SyncTransforms();
            var bounds = collider.bounds;
            var centre = bounds.center;
            // The fish's long axis is the longest side of its collider; a side view looks across it, a head-on view
            // along it.
            var size = bounds.size;
            var longAxis = size.x >= size.z ? Vector3.right : Vector3.forward;
            var across = longAxis == Vector3.right ? Vector3.forward : Vector3.right;
            var rows = new List<Row>();
            foreach (var (view, look) in new[] { ("side", across), ("head", longAxis) })
            foreach (var distance in Distances)
            {
                var eye = centre - look * distance;
                var window = HalfWindowDegrees(collider, eye, look, distance);
                foreach (var scatter in ScatterDegrees)
                {
                    var random = new System.Random(310);
                    var hits = 0;
                    for (var i = 0; i < RaysPerCase; i++)
                    {
                        var dir = Scatter(look, scatter, random);
                        if (collider.Raycast(new Ray(eye, dir), out _, distance * 2f)) hits++;
                    }
                    rows.Add(new Row
                    {
                        species = fish.Species.SpeciesId, fish = fish.name, view = view, distance = distance,
                        scatterDegrees = scatter, colliderBefore = size, hitBefore = hits / (float)RaysPerCase,
                        halfWindowDegBefore = window
                    });
                }
            }
            return rows;
        }

        private static Bounds ModelBounds(FishActor fish)
        {
            var renderers = fish.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        // The smallest angle off the centre line, in any of eight directions, at which the ray first misses.
        private static float HalfWindowDegrees(Collider collider, Vector3 eye, Vector3 look, float distance)
        {
            var smallest = 90f;
            for (var k = 0; k < 8; k++)
            {
                var axis = Quaternion.AngleAxis(k * 45f, look) * Vector3.up;
                var angle = 0f;
                while (angle < 45f && collider.Raycast(new Ray(eye, Quaternion.AngleAxis(angle, Vector3.Cross(look, axis)) * look), out _, distance * 2f))
                    angle += 0.01f;
                smallest = Mathf.Min(smallest, angle);
            }
            return smallest;
        }

        // A direction off the aim line by a normally distributed angle (standard deviation sigma degrees).
        private static Vector3 Scatter(Vector3 look, float sigma, System.Random random)
        {
            double Gauss() => Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble());
            var up = Vector3.up;
            var side = Vector3.Cross(up, look).normalized;
            return (Quaternion.AngleAxis((float)(Gauss() * sigma), up) * Quaternion.AngleAxis((float)(Gauss() * sigma), side) * look).normalized;
        }
    }
}
