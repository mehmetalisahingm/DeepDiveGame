using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #189 P4.8-C: original project-authored art for the starter rowboat's repair worksite (no downloaded model).
    // R01 groups, in BoatRepairParts.All order: hull patch, seat/support, oar connection. The broken hull is a
    // transom frame with three open bays (the slot positions of BoatRepairNetworkBinding.SlotLocal); each group's
    // permanent repaired part fills one bay and keeps OakRepairPatch's nail surface, so the host-driven nail seating
    // and the technical ids boat-part-hull|engine|fuel-tank stay untouched. Re-runnable; GUIDs of existing assets are kept.
    public static class P48RepairSetBuilder
    {
        public const string Root = "Assets/DeepDive/Art/Resources/RepairSet";
        private const string Tools = "Assets/DeepDive/Art/RepairTools";

        // BoatRepairNetworkBinding.SlotLocal(i); RepairSetArtTests pins the two together.
        private static readonly Vector3[] Slot = { new Vector3(-.65f, .75f, -2.15f), new Vector3(0f, .75f, -2.15f), new Vector3(.65f, .75f, -2.15f) };
        private const float Stern = -2.15f, Bow = 2.45f, Keel = .22f;

        private static Material oldWood, ribWood, rawWood, tar, rope, brass, glow, chalk, chock, oak, oakEdge, steel, steelEdge, leather;

        [MenuItem("DeepDive/P4.8/Generate repair set (broken rowboat, R01 parts, drop trestle)")]
        public static void Generate()
        {
            Folder(Root); Folder(Root + "/Meshes"); Folder(Root + "/Materials");
            oldWood = Mat("RS_OldPlank", new Color(.44f, .37f, .29f), 0f, .08f, true);
            ribWood = Mat("RS_OldRib", new Color(.36f, .295f, .225f), 0f, .06f, true);
            rawWood = Mat("RS_RawSplinter", new Color(.66f, .5f, .3f), 0f, .1f, true);
            tar = Mat("RS_Tar", new Color(.045f, .04f, .035f), 0f, .35f, false);
            rope = Mat("RS_Rope", new Color(.7f, .58f, .38f), 0f, .08f, false);
            brass = Mat("RS_Brass", new Color(.45f, .32f, .12f), .6f, .45f, false);
            chalk = Mat("RS_Chalk", new Color(.86f, .84f, .76f), 0f, .05f, false);
            chock = Mat("RS_Chock", new Color(.3f, .245f, .185f), 0f, .05f, false);
            glow = Mat("RS_LanternGlow", new Color(1f, .78f, .38f), 0f, .3f, false);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(3.1f, 1.95f, .75f));
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(glow);
            oak = Existing("WarmOak"); oakEdge = Existing("WornOakEdges"); steel = Existing("ForgedSteel"); steelEdge = Existing("WornSteelEdges"); leather = Existing("HandleBinding");

            SavePrefab(BuildDamagedHull(), "DamagedRowboat");
            SavePrefab(BuildHullPatch(), "RepairedHullPatch");
            SavePrefab(BuildSeatSupport(), "RepairedSeatSupport");
            SavePrefab(BuildOarlock(), "RepairedOarlock");
            SavePrefab(BuildTrestle(), "DropTrestle");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("P48_REPAIR_SET_DONE");
        }

        public static void GenerateBatch() => Generate();

        // --- broken hull --------------------------------------------------------------------------

        private static GameObject BuildDamagedHull()
        {
            var root = new GameObject("DamagedRowboat");
            var hull = new GameObject("Hull"); hull.transform.SetParent(root.transform, false);

            // Cradle chocks the keel rests on.
            Prim(PrimitiveType.Cube, root.transform, "CradleChock_Stern", new Vector3(0, .1f, -1.3f), new Vector3(1.0f, .2f, .32f), chock);
            Prim(PrimitiveType.Cube, root.transform, "CradleChock_Bow", new Vector3(0, .1f, 1.35f), new Vector3(.8f, .2f, .32f), chock);

            var planks = new MeshBuilder();
            const float seam = .006f;
            float[] edges = { 0f, .28f, .52f, .76f, 1f };
            for (var side = -1; side <= 1; side += 2)
                for (var k = 0; k < 4; k++)
                {
                    float lo = edges[k] + seam, hi = edges[k + 1] - seam;
                    if (side < 0 && (k == 1 || k == 2))
                    {
                        // Port quarter stove in: staggered splintered ends back to ~z -1.2.
                        var thirds = (hi - lo) / 3f;
                        float[] start = { -1.25f, -.82f, -1.05f };
                        for (var t = 0; t < 3; t++)
                            AddStrake(planks, side, lo + thirds * t, lo + thirds * (t + 1) - .002f, start[t] + k * .07f, Bow - .1f);
                    }
                    else if (side > 0 && k == 3)
                    {
                        // Starboard gunwale strake torn off aft, ragged: the oar connection was ripped out with it.
                        var halves = (hi - lo) / 3f;
                        float[] start = { -.55f, -.95f, -.2f };
                        for (var t = 0; t < 3; t++)
                            AddStrake(planks, side, lo + halves * t, lo + halves * (t + 1) - .002f, start[t], Bow - .1f);
                    }
                    else AddStrake(planks, side, lo, hi, Stern + .06f, Bow - .1f);
                }
            Part(hull.transform, "Planking", planks.ToMesh("DamagedPlanking"), new[] { oldWood, rawWood });

            var ribs = new MeshBuilder();
            foreach (var z in new[] { -1.55f, -.6f, .35f, 1.3f, 2.05f }) AddRib(ribs, z, .07f);
            Part(hull.transform, "Ribs", ribs.ToMesh("DamagedRibs"), new[] { ribWood, rawWood });

            // Bow stem and breasthook.
            Prim(PrimitiveType.Cube, hull.transform, "BowStem", new Vector3(0, .74f, Bow - .14f), new Vector3(.1f, .9f, .12f), ribWood, new Vector3(-16f, 0, 0));
            // Forward thwart and what is left of the floor boards.
            Prim(PrimitiveType.Cube, hull.transform, "Thwart_Forward", new Vector3(0, .62f, .45f), new Vector3(1.7f, .05f, .32f), oldWood);
            float[] floorX = { -.48f, -.16f, .16f, .48f };
            for (var i = 0; i < floorX.Length; i++)
            {
                if (i == 2) continue; // one board gone
                var len = i == 0 ? 2.3f : 2.7f;
                Prim(PrimitiveType.Cube, hull.transform, "FloorBoard_" + i, new Vector3(floorX[i], .31f + .01f * i, .3f - (i == 0 ? .35f : 0f)), new Vector3(.29f, .035f, len), oldWood);
            }

            // Transom frame: three open bays where the repaired parts fit.
            var frame = new GameObject("TransomFrame"); frame.transform.SetParent(root.transform, false);
            Prim(PrimitiveType.Cube, frame.transform, "Sill", new Vector3(0, .35f, Stern), new Vector3(2.04f, .1f, .12f), ribWood);
            Prim(PrimitiveType.Cube, frame.transform, "TopRail", new Vector3(0, 1.14f, Stern), new Vector3(2.04f, .09f, .12f), oldWood);
            foreach (var x in new[] { -.985f, -.325f, .325f, .985f })
                Prim(PrimitiveType.Cube, frame.transform, "Post_" + (x < 0 ? "L" : "R") + Mathf.Abs(Mathf.RoundToInt(x * 100)), new Vector3(x, .745f, Stern), new Vector3(Mathf.Abs(x) > .9f ? .07f : .06f, .89f, .12f), ribWood);

            // Chalk-white trim around each bay opening: still readable when the oak inside has gone dark.
            var trim = new GameObject("BayTrim"); trim.transform.SetParent(frame.transform, false);
            foreach (var slotX in new[] { -.65f, 0f, .65f })
            {
                var tag = "Bay" + Mathf.RoundToInt(slotX * 100);
                Prim(PrimitiveType.Cube, trim.transform, tag + "_Top", new Vector3(slotX, 1.1f, Stern - .062f), new Vector3(.62f, .02f, .006f), chalk);
                Prim(PrimitiveType.Cube, trim.transform, tag + "_Bottom", new Vector3(slotX, .39f, Stern - .062f), new Vector3(.62f, .02f, .006f), chalk);
                Prim(PrimitiveType.Cube, trim.transform, tag + "_Left", new Vector3(slotX - .3f, .745f, Stern - .062f), new Vector3(.02f, .72f, .006f), chalk);
                Prim(PrimitiveType.Cube, trim.transform, tag + "_Right", new Vector3(slotX + .3f, .745f, Stern - .062f), new Vector3(.02f, .72f, .006f), chalk);
            }

            // Splintered leftovers behind the bays (kept behind the repaired panel plane, never inside it).
            var remnants = new GameObject("Remnants"); remnants.transform.SetParent(root.transform, false);
            // Bay 0, hull patch: ragged plank stubs hanging from the port post, sill splinters.
            for (var i = 0; i < 3; i++)
                Prim(PrimitiveType.Cube, remnants.transform, "PortSplinter_" + i, new Vector3(-.88f + i * .03f, .52f + i * .17f, Stern + .16f + i * .02f), new Vector3(.05f, .24f - i * .04f, .05f), rawWood, new Vector3(0, i * 9f, 12f - i * 14f));
            // Bay 1, seat/support: the stern sheets thwart snapped short either side, a loose knee hanging by one bolt.
            foreach (var sign in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Cube, remnants.transform, "SternSheets_" + (sign < 0 ? "L" : "R"), new Vector3(sign * .63f, .62f, Stern + .42f), new Vector3(.62f, .05f, .34f), oldWood, new Vector3(0, 0, sign * 6f));
                Prim(PrimitiveType.Cube, remnants.transform, "SnappedEnd_" + (sign < 0 ? "L" : "R"), new Vector3(sign * .33f, .62f, Stern + .42f), new Vector3(.04f, .055f, .34f), rawWood, new Vector3(0, sign * 14f, 0));
            }
            Prim(PrimitiveType.Cube, remnants.transform, "LooseKnee", new Vector3(.38f, .42f, Stern + .38f), new Vector3(.045f, .045f, .4f), ribWood, new Vector3(-52f, 18f, 0));
            // Bay 2, oar connection: the rotten rowlock block, a lashing left trailing.
            Prim(PrimitiveType.Cube, remnants.transform, "RottenRowlockBlock", new Vector3(.64f, 1.02f, Stern + .17f), new Vector3(.22f, .12f, .13f), ribWood, new Vector3(0, 8f, 6f));
            Prim(PrimitiveType.Cylinder, remnants.transform, "TrailingLashing_A", new Vector3(.58f, .78f, Stern + .17f), new Vector3(.025f, .14f, .025f), rope, new Vector3(0, 0, 8f));
            Prim(PrimitiveType.Cylinder, remnants.transform, "TrailingLashing_B", new Vector3(.7f, .66f, Stern + .19f), new Vector3(.025f, .12f, .025f), rope, new Vector3(0, 0, -14f));

            // Work lantern on the starboard post: the one lit, readable thing at dusk.
            var lantern = new GameObject("WorkLantern"); lantern.transform.SetParent(root.transform, false);
            lantern.transform.localPosition = new Vector3(.985f, 1.19f, Stern);
            Prim(PrimitiveType.Cube, lantern.transform, "Pole", new Vector3(0, .2f, 0), new Vector3(.045f, .4f, .045f), ribWood);
            // The arm reaches out toward the crew so the light falls on the bays head-on, not along the panel faces.
            Prim(PrimitiveType.Cube, lantern.transform, "Arm", new Vector3(0, .38f, -.2f), new Vector3(.03f, .03f, .4f), ribWood);
            AddLantern(lantern.transform, new Vector3(0f, .24f, -.4f));
            return root;
        }

        private static void AddLantern(Transform parent, Vector3 at)
        {
            var l = new GameObject("Lantern"); l.transform.SetParent(parent, false); l.transform.localPosition = at;
            Prim(PrimitiveType.Cube, l.transform, "Glass", Vector3.zero, new Vector3(.11f, .15f, .11f), glow);
            Prim(PrimitiveType.Cube, l.transform, "Base", new Vector3(0, -.09f, 0), new Vector3(.14f, .03f, .14f), brass);
            Prim(PrimitiveType.Cube, l.transform, "Cap", new Vector3(0, .095f, 0), new Vector3(.15f, .03f, .15f), brass);
            Prim(PrimitiveType.Cube, l.transform, "CapTop", new Vector3(0, .125f, 0), new Vector3(.08f, .03f, .08f), brass);
            foreach (var x in new[] { -.058f, .058f })
                foreach (var z in new[] { -.058f, .058f })
                    Prim(PrimitiveType.Cube, l.transform, "Bar", new Vector3(x, 0, z), new Vector3(.012f, .15f, .012f), brass);
            // Warm point light, only on in the dark (RepairLanternGlow): the cue that makes the fresh oak readable at night.
            var lampObject = new GameObject("LanternLight"); lampObject.transform.SetParent(l.transform, false);
            lampObject.transform.localPosition = new Vector3(0, -.05f, 0);
            var lamp = lampObject.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.range = 6.5f; lamp.intensity = 0f; lamp.shadows = LightShadows.None;
            lamp.color = new Color(1f, .74f, .4f); lamp.enabled = false;
            lampObject.AddComponent<DeepDive.Composition.RepairLanternGlow>();
        }

        // --- repaired parts (panel frame: origin = slot centre, front/nail face toward -z, same as OakRepairPatch) --

        private static GameObject BuildHullPatch()
        {
            var root = PatchRoot("RepairedHullPatch");
            // Tar-caulked seams between the three fitted slats, battens inside, strap iron at the lower corners.
            foreach (var y in new[] { -.1125f, .1125f })
                Prim(PrimitiveType.Cube, root.transform, "TarSeam", new Vector3(0, y, -.0315f), new Vector3(.6f, .014f, .004f), tar);
            foreach (var x in new[] { -.2f, .2f })
                Prim(PrimitiveType.Cube, root.transform, "Batten", new Vector3(x, 0, .058f), new Vector3(.07f, .66f, .05f), oakEdge);
            foreach (var x in new[] { -.275f, .275f })
                Prim(PrimitiveType.Cube, root.transform, "SideCleat", new Vector3(x, 0, -.05f), new Vector3(.045f, .68f, .05f), oakEdge);
            foreach (var y in new[] { -.1125f, .1125f })
                Prim(PrimitiveType.Cube, root.transform, "Oakum", new Vector3(0, y, -.0335f), new Vector3(.5f, .018f, .004f), rope);
            return root;
        }

        private static GameObject BuildSeatSupport()
        {
            var root = PatchRoot("RepairedSeatSupport");
            // Thwart plank fixed to the panel and held up by two knee braces.
            Prim(PrimitiveType.Cube, root.transform, "Thwart", new Vector3(0, -.12f, .32f), new Vector3(.58f, .05f, .58f), oakEdge);
            Prim(PrimitiveType.Cube, root.transform, "ThwartEdge", new Vector3(0, -.12f, .62f), new Vector3(.58f, .056f, .018f), oakEdge);
            foreach (var x in new[] { -.2f, .2f })
            {
                Prim(PrimitiveType.Cube, root.transform, "KneeBrace", new Vector3(x, -.26f, .18f), new Vector3(.045f, .045f, .4f), oak, new Vector3(-38f, 0, 0));
                Prim(PrimitiveType.Cylinder, root.transform, "BraceBolt", new Vector3(x, -.355f, .045f), new Vector3(.022f, .008f, .022f), steel, new Vector3(90f, 0, 0));
            }
            Prim(PrimitiveType.Cube, root.transform, "ThwartStrap", new Vector3(0, -.09f, .5f), new Vector3(.6f, .006f, .05f), steel);
            // Outer face (where the crew works): heavy sill cleat and crossed iron straps over the two lower slats.
            Prim(PrimitiveType.Cube, root.transform, "SillCleat", new Vector3(0, -.355f, -.055f), new Vector3(.6f, .06f, .07f), oakEdge);
            foreach (var sign in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, root.transform, "CrossStrap", new Vector3(0, -.15f, -.036f), new Vector3(.03f, .6f, .006f), steelEdge, new Vector3(0, 0, sign * 28f));
            foreach (var x in new[] { -.2f, .2f })
                foreach (var y in new[] { -.3f, -.0f })
                    Prim(PrimitiveType.Cylinder, root.transform, "StrapBolt", new Vector3(x, y - .03f, -.04f), new Vector3(.028f, .006f, .028f), steel, new Vector3(90f, 0, 0));
            return root;
        }

        private static GameObject BuildOarlock()
        {
            var root = PatchRoot("RepairedOarlock");
            const float top = .3375f;
            // Oak rowlock pad on the panel top, an iron oarlock horn, lashing, and the oar resting in it.
            Prim(PrimitiveType.Cube, root.transform, "RowlockPad", new Vector3(0, top + .045f, 0), new Vector3(.36f, .09f, .1f), oak);
            Prim(PrimitiveType.Cube, root.transform, "RowlockPlate", new Vector3(0, top + .096f, 0), new Vector3(.14f, .012f, .08f), steel);
            foreach (var x in new[] { -.05f, .05f })
                Prim(PrimitiveType.Cylinder, root.transform, "Horn", new Vector3(x, top + .165f, 0), new Vector3(.02f, .065f, .02f), steel);
            foreach (var x in new[] { -.05f, .05f })
                Prim(PrimitiveType.Cube, root.transform, "HornHook", new Vector3(x * .55f, top + .228f, 0), new Vector3(.05f, .016f, .02f), steelEdge);
            foreach (var z in new[] { -.04f, .04f })
                Prim(PrimitiveType.Cylinder, root.transform, "Lashing", new Vector3(0, top + .045f, z), new Vector3(.38f, .005f, .02f), rope, new Vector3(0, 0, 90f));
            foreach (var x in new[] { -.24f, .24f })
                Prim(PrimitiveType.Cube, root.transform, "RopeBand", new Vector3(x, 0, -.0345f), new Vector3(.04f, .68f, .008f), rope);
            var oar = new GameObject("Oar"); oar.transform.SetParent(root.transform, false);
            oar.transform.localPosition = new Vector3(0, top + .205f, 0); oar.transform.localRotation = Quaternion.Euler(12f, 0, 0);
            Prim(PrimitiveType.Cylinder, oar.transform, "Loom", new Vector3(0, 0, .45f), new Vector3(.055f, .75f, .055f), oak, new Vector3(90f, 0, 0));
            Prim(PrimitiveType.Cylinder, oar.transform, "LeatherCollar", new Vector3(0, 0, .02f), new Vector3(.085f, .05f, .085f), leather, new Vector3(90f, 0, 0));
            Prim(PrimitiveType.Cube, oar.transform, "Blade", new Vector3(0, 0, 1.0f), new Vector3(.17f, .022f, .46f), oakEdge);
            return root;
        }

        // The repaired parts are the OakRepairPatch panel (nail surface, 3 fitted slats) plus group-specific hardware.
        private static GameObject PatchRoot(string name)
        {
            var root = new GameObject(name);
            var patch = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Tools + "/OakRepairPatch.prefab"), root.transform);
            patch.name = "OakRepairPatch";
            return root;
        }

        // --- drop trestle (repair-drop-0) -----------------------------------------------------------

        private static GameObject BuildTrestle()
        {
            var root = new GameObject("DropTrestle");
            // Plank rack positions are drop.TransformPoint((i-1)*.8, .65, 0): the bench top is just below them.
            Prim(PrimitiveType.Cube, root.transform, "BenchTop", new Vector3(0, .52f, 0), new Vector3(2.5f, .06f, .55f), oakEdge);
            Prim(PrimitiveType.Cube, root.transform, "ChalkMark", new Vector3(0, .552f, 0), new Vector3(2.4f, .006f, .09f), chalk);
            foreach (var x in new[] { -1.05f, 1.05f })
                for (var side = -1; side <= 1; side += 2)
                    Prim(PrimitiveType.Cube, root.transform, "Leg", new Vector3(x, .25f, side * .2f), new Vector3(.07f, .56f, .07f), ribWood, new Vector3(side * 12f, 0, 0));
            foreach (var x in new[] { -1.05f, 1.05f })
                Prim(PrimitiveType.Cube, root.transform, "Brace", new Vector3(x, .2f, 0), new Vector3(.05f, .05f, .46f), ribWood);
            var post = Prim(PrimitiveType.Cube, root.transform, "LanternPost", new Vector3(1.28f, .62f, 0), new Vector3(.06f, 1.24f, .06f), ribWood);
            Prim(PrimitiveType.Cube, root.transform, "LanternArm", new Vector3(1.2f, 1.2f, 0), new Vector3(.2f, .03f, .03f), ribWood);
            AddLantern(root.transform, new Vector3(1.12f, 1.06f, 0));
            return root;
        }

        // --- planks and ribs: lofted from the hull function ---------------------------------------------

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        private static float HalfWidth(float t)
        {
            if (t <= .4f) return .95f + .15f * Smooth(t / .4f);
            var u = Mathf.Clamp01((t - .4f) / .6f);
            return Mathf.Max(.05f, 1.1f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(u, 2.4f))));
        }
        private static float Sheer(float t)
        {
            var aft = t < .45f ? Mathf.Pow(1f - t / .45f, 2f) * .9f : 0f;
            var fwd = t >= .45f ? Mathf.Pow((t - .45f) / .55f, 2f) * 1.4f : 0f;
            return .78f + .3f * (aft + fwd);
        }

        // A point of the hull surface: s = 0 keel line, s = 1 sheer line; z along the boat (stern -2.15 .. bow 2.45).
        private static Vector3 Hull(float z, float s, int side)
        {
            var t = Mathf.Clamp01((z - Stern) / (Bow - Stern));
            var w = HalfWidth(t); var h = Sheer(t); var keel = Keel + .25f * t * t * t;
            var box = Smooth(1f - t / .22f);
            var xu = w * Mathf.Sin(s * 1.35f) / Mathf.Sin(1.35f);
            var yu = keel + (h - keel) * Mathf.Pow(s, 1.8f);
            var xb = w * Mathf.Min(1f, s / .12f);
            var yb = keel + (h - keel) * s;
            return new Vector3(side * Mathf.Lerp(xu, xb, box), Mathf.Lerp(yu, yb, box), z);
        }
        private static Vector3 Outward(Vector3 p) => (p - new Vector3(0, .6f, p.z)).normalized;

        private static void AddStrake(MeshBuilder m, int side, float sLo, float sHi, float z0, float z1, float th = .04f)
        {
            var n = Mathf.Max(2, Mathf.CeilToInt((z1 - z0) / .42f));
            var o = new Vector3[n + 1, 2]; var i = new Vector3[n + 1, 2];
            for (var a = 0; a <= n; a++)
                for (var b = 0; b < 2; b++)
                {
                    var p = Hull(Mathf.Lerp(z0, z1, a / (float)n), b == 0 ? sLo : sHi, side);
                    o[a, b] = p; i[a, b] = p - Outward(p) * th;
                }
            m.Slab(o, i);
        }

        private static void AddRib(MeshBuilder m, float z, float width)
        {
            const int n = 16;
            var o = new Vector3[n + 1, 2]; var i = new Vector3[n + 1, 2];
            for (var a = 0; a <= n; a++)
            {
                var u = -1f + 2f * a / n;
                for (var b = 0; b < 2; b++)
                {
                    var p = Hull(z + b * width, Mathf.Abs(u), u < 0 ? -1 : 1);
                    var inner = p - Outward(p) * .04f;
                    o[a, b] = inner; i[a, b] = inner - Outward(p) * .055f;
                }
            }
            m.Slab(o, i);
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> v = new List<Vector3>();
            private readonly List<int> main = new List<int>(), caps = new List<int>();

            private void Quad(List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 away)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), away) < 0f) { var s = b; b = d; d = s; }
                var k = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            private static Vector3 Mid(params Vector3[] p) { var s = Vector3.zero; foreach (var x in p) s += x; return s / p.Length; }

            // O = outer surface grid [u, v], I = inner surface grid; u ends become raw end-grain caps.
            public void Slab(Vector3[,] o, Vector3[,] i)
            {
                int nu = o.GetLength(0), nv = o.GetLength(1);
                for (var a = 0; a < nu - 1; a++)
                    for (var b = 0; b < nv - 1; b++)
                    {
                        var co = Mid(o[a, b], o[a + 1, b], o[a + 1, b + 1], o[a, b + 1]);
                        var ci = Mid(i[a, b], i[a + 1, b], i[a + 1, b + 1], i[a, b + 1]);
                        Quad(main, o[a, b], o[a + 1, b], o[a + 1, b + 1], o[a, b + 1], co - ci);
                        Quad(main, i[a, b], i[a + 1, b], i[a + 1, b + 1], i[a, b + 1], ci - co);
                    }
                for (var a = 0; a < nu - 1; a++)
                {
                    Quad(main, o[a, 0], o[a + 1, 0], i[a + 1, 0], i[a, 0],
                        Mid(o[a, 0], o[a + 1, 0], i[a, 0], i[a + 1, 0]) - Mid(o[a, 1], o[a + 1, 1], i[a, 1], i[a + 1, 1]));
                    Quad(main, o[a, nv - 1], o[a + 1, nv - 1], i[a + 1, nv - 1], i[a, nv - 1],
                        Mid(o[a, nv - 1], o[a + 1, nv - 1], i[a, nv - 1], i[a + 1, nv - 1]) - Mid(o[a, nv - 2], o[a + 1, nv - 2], i[a, nv - 2], i[a + 1, nv - 2]));
                }
                for (var b = 0; b < nv - 1; b++)
                {
                    Quad(caps, o[0, b], o[0, b + 1], i[0, b + 1], i[0, b],
                        Mid(o[0, b], o[0, b + 1], i[0, b], i[0, b + 1]) - Mid(o[1, b], o[1, b + 1], i[1, b], i[1, b + 1]));
                    Quad(caps, o[nu - 1, b], o[nu - 1, b + 1], i[nu - 1, b + 1], i[nu - 1, b],
                        Mid(o[nu - 1, b], o[nu - 1, b + 1], i[nu - 1, b], i[nu - 1, b + 1]) - Mid(o[nu - 2, b], o[nu - 2, b + 1], i[nu - 2, b], i[nu - 2, b + 1]));
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(v);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(main, 0); mesh.SetTriangles(caps, 1);
                var uv = new Vector2[v.Count];
                for (var k = 0; k < uv.Length; k++) uv[k] = new Vector2(v[k].z, v[k].y);
                mesh.uv = uv;
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return Save(mesh, name);
            }
        }

        // --- helpers ---------------------------------------------------------------------------------

        private static Mesh Save(Mesh mesh, string name)
        {
            var path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        private static void Part(Transform parent, string name, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localEulerAngles = euler; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>(Tools + "/Materials/" + name + ".mat");

        private static Material Mat(string name, Color color, float metallic, float smooth, bool doubleSided)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // --- review renders --------------------------------------------------------------------------------

        // -executeMethod DeepDive.EditorTools.P48RepairSetBuilder.PreviewBatch -previewMode <assembly|damaged|repaired> -previewOut <png>
        public static void PreviewBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key) { var k = Array.IndexOf(args, key); return k >= 0 ? args[k + 1] : null; }
            var mode = Arg("-previewMode") ?? "assembly"; var output = Arg("-previewOut");
            var root = new GameObject("PreviewRoot");
            Spawn("DamagedRowboat", root.transform, Vector3.zero);
            if (mode != "damaged")
            {
                var names = new[] { "RepairedHullPatch", "RepairedSeatSupport", "RepairedOarlock" };
                for (var k = 0; k < 3; k++) if (mode == "assembly" || mode == "repaired") Spawn(names[k], root.transform, Slot[k]);
            }
            Spawn("DropTrestle", root.transform, new Vector3(4.5f, 0, -3.5f));
            Render(root, output, mode == "damaged" || mode == "assembly");
            Debug.Log("P48_PREVIEW_DONE " + output);
        }

        private static void Spawn(string prefab, Transform parent, Vector3 at)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + prefab + ".prefab"), parent);
            go.transform.localPosition = at;
        }

        private static void Render(GameObject go, string output, bool night)
        {
            const int tile = 640;
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            var human = GameObject.CreatePrimitive(PrimitiveType.Capsule); human.transform.localScale = new Vector3(.6f, .9f, .6f);
            human.transform.position = new Vector3(2.2f, .9f, -3.2f);
            human.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.85f, .3f, .25f) };
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.localScale = new Vector3(60, .02f, 60); ground.transform.position = new Vector3(0, -.01f, 0);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.78f, .7f, .52f) };
            var camObject = new GameObject("cam"); var cam = camObject.AddComponent<Camera>();
            var rt = new RenderTexture(tile, tile, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .78f, .9f); cam.fieldOfView = 32f;
            var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(48, -35, 0); light.intensity = 1.4f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .5f, .56f);
            var sheet = new Texture2D(tile * 2, tile * 2, TextureFormat.RGB24, false);
            var aim = new[] { new Vector3(0, .9f, -1.5f), new Vector3(0, .8f, 0), new Vector3(0, .8f, -1.8f), new Vector3(0, .8f, -2.1f) };
            var yaws = new[] { 200f, 140f, 20f, 0f }; var dist = new[] { 5.2f, 9f, 6.2f, 3.6f }; var pitch = new[] { 14f, 26f, 12f, 4f };
            cam.transform.position = new Vector3(0, 3, -8); cam.transform.LookAt(Vector3.zero); cam.Render(); // first frame is not lit like the rest
            for (var k = 0; k < 4; k++)
            {
                cam.transform.position = aim[k] + Quaternion.Euler(pitch[k], yaws[k], 0f) * Vector3.back * dist[k];
                cam.transform.LookAt(aim[k]); cam.Render();
                RenderTexture.active = rt;
                var t = new Texture2D(tile, tile, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, tile, tile), 0, 0); t.Apply();
                sheet.SetPixels((k % 2) * tile, (1 - k / 2) * tile, tile, tile, t.GetPixels()); Object.DestroyImmediate(t);
            }
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            foreach (var o in new Object[] { camObject, light.gameObject, ground, human, go }) Object.DestroyImmediate(o);
        }
    }
}
