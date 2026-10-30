using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Composition;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace DeepDive.EditorTools
{
    // #217 P4.11-C: original reef art kit (three coral silhouettes, two rocks, one swaying seagrass group). Procedural, deterministic
    // (fixed seeds), no downloaded model or texture. Flat-shaded low poly in the palette of the supplied Quaternius fish so the reef
    // matches what swims over it. Each piece: real metre scale, pivot on the ground at the centre, two LODs, at most two materials,
    // no collider / rigidbody / NetworkObject. Output: Assets/DeepDive/Art/Resources/ReefKit/.
    public static class P411ReefKitBuilder
    {
        public const string Root = "Assets/DeepDive/Art/Resources/ReefKit";
        public const string BudgetCsv = "docs/reports/evidence/p411-reef-kit/budget.csv";

        private static Material branch, branchTip, fan, fanRim, mound, moundTip, rockDark, rockWarm, grass, grassTip;

        [MenuItem("DeepDive/P4.11/Generate reef kit (coral, rock, seagrass)")]
        public static void Generate()
        {
            Folder(Root); Folder(Root + "/Meshes"); Folder(Root + "/Materials");
            branch = Mat("RK_CoralBranch", new Color(.96f, .42f, .52f), .12f, false);
            branchTip = Mat("RK_CoralBranchTip", new Color(1f, .8f, .84f), .15f, false);
            // The one measured emission of the kit: faint warm polyp tips. Nothing else glows (not every coral is a light).
            branchTip.EnableKeyword("_EMISSION"); branchTip.SetColor("_EmissionColor", new Color(.5f, .22f, .3f));
            branchTip.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; EditorUtility.SetDirty(branchTip);
            fan = Mat("RK_CoralFan", new Color(.74f, .34f, .72f), .1f, true);
            fanRim = Mat("RK_CoralFanRim", new Color(.95f, .62f, .82f), .1f, true);
            mound = Mat("RK_CoralMound", new Color(.95f, .66f, .22f), .14f, false);
            moundTip = Mat("RK_CoralMoundTip", new Color(1f, .88f, .5f), .14f, false);
            rockDark = Mat("RK_RockDark", new Color(.33f, .37f, .41f), .08f, false);
            rockWarm = Mat("RK_RockWarm", new Color(.54f, .49f, .41f), .08f, false);
            grass = Mat("RK_Grass", new Color(.28f, .6f, .34f), .1f, true);
            grassTip = Mat("RK_GrassTip", new Color(.56f, .78f, .4f), .1f, true);

            var csv = new StringBuilder("id,kind,lod0_tris,lod1_tris,renderers,materials,width_m,height_m,depth_m\n");
            Piece("reef-coral-branching", ReefKitKind.Coral, d => BranchingCoral(d), new[] { branch, branchTip }, csv, new Vector3(1.2f, 1.1f, 1.2f), false);
            Piece("reef-coral-fan", ReefKitKind.Coral, d => FanCoral(d), new[] { fan, fanRim }, csv, new Vector3(1.2f, 1.2f, .3f), false);
            Piece("reef-coral-mound", ReefKitKind.Coral, d => MoundCoral(d), new[] { mound, moundTip }, csv, new Vector3(1.0f, .4f, .9f), false);
            Piece("reef-rock-boulder", ReefKitKind.Rock, d => Boulder(d), new[] { rockDark, rockWarm }, csv, new Vector3(1.8f, 1.1f, 1.5f), false);
            Piece("reef-rock-arch", ReefKitKind.Rock, d => Arch(d), new[] { rockWarm, rockDark }, csv, new Vector3(3.4f, 2.1f, 1.2f), true);
            Seagrass(csv);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Directory.CreateDirectory(Path.GetDirectoryName(BudgetCsv));
            File.WriteAllText(BudgetCsv, csv.ToString());
            Debug.Log("P411_REEF_KIT_DONE\n" + csv);
        }

        public static void GenerateBatch() => Generate();

        // --- pieces -------------------------------------------------------------------------------

        private static void Piece(string id, ReefKitKind kind, Func<int, MB> build, Material[] mats, StringBuilder csv, Vector3 hint, bool walkThrough)
        {
            var root = new GameObject(id);
            var lod0 = build(0); var lod1 = build(1);
            var r0 = Part(root.transform, "LOD0", lod0.ToMesh(id + "_LOD0"), mats);
            var r1 = Part(root.transform, "LOD1", lod1.ToMesh(id + "_LOD1"), mats);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(.28f, new[] { r0 }), new LOD(.04f, new[] { r1 }) });
            group.RecalculateBounds();
            var bounds = r0.GetComponent<MeshFilter>().sharedMesh.bounds;
            var piece = root.AddComponent<ReefKitPiece>();
            piece.Id = id; piece.Kind = kind; piece.Size = Round(bounds.size); piece.WalkThrough = walkThrough;
            piece.ColliderHint = walkThrough ? Vector3.zero : Round(new Vector3(bounds.size.x * .8f, bounds.size.y * .8f, bounds.size.z * .8f));
            csv.AppendLine(string.Join(",", id, kind, lod0.Triangles, lod1.Triangles, 2, mats.Length, F(bounds.size.x), F(bounds.size.y), F(bounds.size.z)));
            Save(root, id);
        }

        private static void Seagrass(StringBuilder csv)
        {
            const string id = "reef-plant-seagrass";
            var root = new GameObject(id);
            var offsets = new[] { new Vector3(-.32f, 0f, .12f), new Vector3(.3f, 0f, .26f), new Vector3(.02f, 0f, -.34f) };
            var tris = 0;
            for (var c = 0; c < offsets.Length; c++)
            {
                var m = Clump(900 + c, 6, .55f + .22f * c);
                tris += m.Triangles;
                var part = Part(root.transform, "Clump_" + c, m.ToMesh(id + "_Clump" + c), new[] { grass, grassTip });
                part.transform.localPosition = offsets[c];
            }
            root.AddComponent<ReefPlantSway>();
            var bounds = BoundsOf(root);
            var piece = root.AddComponent<ReefKitPiece>();
            piece.Id = id; piece.Kind = ReefKitKind.Plant; piece.Size = Round(bounds.size); piece.ColliderHint = Vector3.zero;
            csv.AppendLine(string.Join(",", id, ReefKitKind.Plant, tris, tris, offsets.Length, 2, F(bounds.size.x), F(bounds.size.y), F(bounds.size.z)));
            Save(root, id);
        }

        // --- shapes -----------------------------------------------------------------------------

        private static MB BranchingCoral(int lod)
        {
            var m = new MB(); var rng = new Random(2711);
            var sides = lod == 0 ? 5 : 3; var depth = lod == 0 ? 3 : 2;
            for (var i = 0; i < 3; i++)
            {
                var az = (i * 120f + 20f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(az) * .2f, 1f, Mathf.Sin(az) * .2f).normalized;
                Branch(m, new Vector3(Mathf.Cos(az) * .06f, 0f, Mathf.Sin(az) * .06f), dir, .5f, .09f, depth, sides, rng);
            }
            return m;
        }

        private static void Branch(MB m, Vector3 p, Vector3 dir, float len, float r, int depth, int sides, Random rng)
        {
            var end = p + dir * len;
            m.Tube(p, end, r, r * .68f, sides, 0);
            if (depth == 0) { m.Cone(end, dir, r * .68f, len * .3f, sides, 1); return; }
            var kids = depth >= 2 ? 3 : 2;
            for (var k = 0; k < kids; k++)
            {
                var az = (k / (float)kids + (float)rng.NextDouble() * .25f) * 2f * Mathf.PI;
                var side = Vector3.Cross(dir, Mathf.Abs(dir.y) > .9f ? Vector3.right : Vector3.up).normalized;
                var other = Vector3.Cross(dir, side);
                var tilt = (20f + (float)rng.NextDouble() * 16f) * Mathf.Deg2Rad;
                var d = (dir * Mathf.Cos(tilt) + (side * Mathf.Cos(az) + other * Mathf.Sin(az)) * Mathf.Sin(tilt)).normalized;
                d = (d + Vector3.up * .22f).normalized;
                Branch(m, end - dir * len * .08f, d, len * .74f, r * .66f, depth - 1, sides, rng);
            }
        }

        private static MB FanCoral(int lod)
        {
            var m = new MB(); var top = new Vector3(0f, .22f, 0f);
            m.Tube(Vector3.zero, top, .07f, .045f, lod == 0 ? 6 : 4, 0);
            var ribs = lod == 0 ? 13 : 7; var segs = lod == 0 ? 4 : 2; var links = lod == 0 ? new[] { .22f, .4f, .58f, .75f, .92f } : new[] { .45f, .8f };
            Func<float, float, Vector3> pos = (theta, s) =>
            {
                var len = .8f - .18f * Mathf.Abs(theta) / 75f;
                var a = theta * Mathf.Deg2Rad;
                return top + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (s * len) + new Vector3(0f, 0f, .13f * s * s);
            };
            for (var j = 0; j < ribs; j++)
            {
                var theta = Mathf.Lerp(-75f, 75f, j / (float)(ribs - 1)); var a = theta * Mathf.Deg2Rad;
                var wd = new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0f);
                for (var k = 0; k < segs; k++)
                {
                    float s0 = k / (float)segs, s1 = (k + 1f) / segs;
                    m.Strip(pos(theta, s0), pos(theta, s1), wd, .085f * (1f - .45f * s0), .085f * (1f - .45f * s1), 0);
                }
            }
            for (var j = 0; j < ribs - 1; j++)
            {
                float t0 = Mathf.Lerp(-75f, 75f, j / (float)(ribs - 1)), t1 = Mathf.Lerp(-75f, 75f, (j + 1f) / (ribs - 1));
                foreach (var f in links)
                {
                    var a = pos(t0, f); var b = pos(t1, f);
                    var rad = (a + b) * .5f - top;
                    m.Strip(a, b, rad.normalized, .05f, .05f, 1);
                }
            }
            return m;
        }

        private static MB MoundCoral(int lod)
        {
            var m = new MB(); var rng = new Random(4409);
            var segs = lod == 0 ? 12 : 7; var rings = lod == 0 ? 5 : 3;
            var lumps = new[] { new Vector4(0f, 0f, .36f, .46f), new Vector4(.42f, .12f, .26f, .36f), new Vector4(-.38f, .18f, .28f, .4f), new Vector4(.1f, -.4f, .24f, .32f), new Vector4(-.2f, -.3f, .2f, .28f), new Vector4(.3f, -.28f, .17f, .24f) };
            for (var i = 0; i < lumps.Length; i++)
            {
                var l = lumps[i];
                m.Lump(new Vector3(l.x, 0f, l.y), l.z, l.w, segs, rings, 5 + i, (float)rng.NextDouble() * 6f, i == 0 ? 0 : (i % 2));
            }
            return m;
        }

        private static MB Boulder(int lod)
        {
            var m = new MB();
            m.Rock(Vector3.zero, new Vector3(.9f, .55f, .75f), lod == 0 ? 2 : 1, 11, 0, .2f);
            m.Rock(new Vector3(.95f, 0f, .35f), new Vector3(.38f, .26f, .32f), lod == 0 ? 1 : 0, 23, 1, .2f);
            m.Rock(new Vector3(-.7f, 0f, -.62f), new Vector3(.3f, .22f, .27f), lod == 0 ? 1 : 0, 37, 1, .2f);
            return m;
        }

        // A natural bridge: a noisy tube swept over a half circle with a root at each end. Clear opening: about 1.9 m wide at the base,
        // still 1.0 m wide at 1.4 m height (EditMode test ArchOpeningLetsADiverSwimThrough).
        private static MB Arch(int lod)
        {
            var m = new MB(); var sections = lod == 0 ? 15 : 8; var sides = lod == 0 ? 8 : 5;
            const float R = 1.6f, H = 1.75f;
            var o = new Vector3[sections + 1, sides]; var c = new Vector3[sections + 1];
            for (var i = 0; i <= sections; i++)
            {
                var th = Mathf.PI * i / sections;
                c[i] = new Vector3(Mathf.Cos(th) * R, Mathf.Sin(th) * H, 0f);
                var tangent = new Vector3(-Mathf.Sin(th) * R, Mathf.Cos(th) * H, 0f).normalized;
                var radial = Vector3.Cross(Vector3.forward, tangent).normalized;
                var a = .4f + .1f * (Noise(new Vector3(i * .7f, 1.3f, 0f), 5) - .5f) * 2f + (i == 0 || i == sections ? .12f : 0f);
                var b = .5f + .12f * (Noise(new Vector3(i * .6f, 4.1f, 0f), 9) - .5f) * 2f;
                for (var s = 0; s < sides; s++)
                {
                    var ang = 2f * Mathf.PI * s / sides; var j = .1f * (Noise(new Vector3(i, s, 2f), 3) - .5f);
                    o[i, s] = c[i] + radial * (Mathf.Cos(ang) * (a + j)) + Vector3.forward * (Mathf.Sin(ang) * (b + j));
                }
            }
            for (var i = 0; i < sections; i++)
                for (var s = 0; s < sides; s++)
                {
                    var s2 = (s + 1) % sides;
                    m.Quad(0, o[i, s], o[i, s2], o[i + 1, s2], o[i + 1, s], (o[i, s] + o[i, s2] + o[i + 1, s] + o[i + 1, s2]) / 4f - (c[i] + c[i + 1]) / 2f);
                }
            m.Rock(c[0] + new Vector3(.05f, -.2f, 0f), new Vector3(.72f, .5f, .62f), lod == 0 ? 1 : 0, 41, 1, .22f);
            m.Rock(c[sections] + new Vector3(-.05f, -.2f, 0f), new Vector3(.72f, .5f, .62f), lod == 0 ? 1 : 0, 53, 1, .22f);
            m.GroundTo(0f);
            return m;
        }

        // One clump of curved ribbon blades; its origin is the base the sway rotates about.
        private static MB Clump(int seed, int blades, float height)
        {
            var m = new MB(); var rng = new Random(seed);
            for (var b = 0; b < blades; b++)
            {
                var az = (float)rng.NextDouble() * 2f * Mathf.PI; var r = .05f + (float)rng.NextDouble() * .12f;
                var basePoint = new Vector3(Mathf.Cos(az) * r, 0f, Mathf.Sin(az) * r);
                var h = height * (.7f + (float)rng.NextDouble() * .5f);
                var lean = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az)) * (.1f + (float)rng.NextDouble() * .22f) * h;
                var side = Vector3.Cross(Vector3.up, lean.sqrMagnitude < 1e-5f ? Vector3.right : lean).normalized;
                var w0 = .055f + (float)rng.NextDouble() * .02f;
                for (var k = 0; k < 4; k++)
                {
                    float t0 = k / 4f, t1 = (k + 1f) / 4f;
                    var p0 = basePoint + new Vector3(lean.x * t0 * t0, h * t0, lean.z * t0 * t0);
                    var p1 = basePoint + new Vector3(lean.x * t1 * t1, h * t1, lean.z * t1 * t1);
                    m.Strip(p0, p1, side, w0 * (1f - .8f * t0), w0 * (1f - .8f * t1), k == 3 ? 1 : 0);
                }
            }
            return m;
        }

        // --- mesh builder: flat-shaded triangles, orientation fixed by an "away" vector ------------

        private static float Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647 + seed * 362437);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static float Noise(Vector3 p, int seed)
        {
            var x0 = Mathf.FloorToInt(p.x); var y0 = Mathf.FloorToInt(p.y); var z0 = Mathf.FloorToInt(p.z);
            float fx = Smooth(p.x - x0), fy = Smooth(p.y - y0), fz = Smooth(p.z - z0);
            float L(int dx, int dy, int dz) => Hash(x0 + dx, y0 + dy, z0 + dz, seed);
            var a = Mathf.Lerp(Mathf.Lerp(L(0, 0, 0), L(1, 0, 0), fx), Mathf.Lerp(L(0, 1, 0), L(1, 1, 0), fx), fy);
            var b = Mathf.Lerp(Mathf.Lerp(L(0, 0, 1), L(1, 0, 1), fx), Mathf.Lerp(L(0, 1, 1), L(1, 1, 1), fx), fy);
            return Mathf.Lerp(a, b, fz);
        }
        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private sealed class MB
        {
            private readonly List<Vector3> v = new List<Vector3>();
            private readonly List<int>[] t = { new List<int>(), new List<int>() };
            public int Triangles => (t[0].Count + t[1].Count) / 3;

            public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 away)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), away) < 0f) { var s = b; b = c; c = s; }
                var k = v.Count; v.Add(a); v.Add(b); v.Add(c); t[sub].AddRange(new[] { k, k + 1, k + 2 });
            }
            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 away) { Tri(sub, a, b, c, away); Tri(sub, a, c, d, away); }

            // A flat ribbon from a to b with a given width direction; double-sided materials render both faces.
            public void Strip(Vector3 a, Vector3 b, Vector3 wd, float w0, float w1, int sub)
            {
                wd = wd.normalized;
                var n = Vector3.Cross(b - a, wd);
                Quad(sub, a - wd * w0 * .5f, a + wd * w0 * .5f, b + wd * w1 * .5f, b - wd * w1 * .5f, n.sqrMagnitude < 1e-9f ? Vector3.forward : n);
            }

            public void Tube(Vector3 p0, Vector3 p1, float r0, float r1, int sides, int sub)
            {
                var axis = (p1 - p0).normalized;
                var u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(axis, u);
                for (var i = 0; i < sides; i++)
                {
                    float a0 = 2f * Mathf.PI * i / sides, a1 = 2f * Mathf.PI * (i + 1) / sides;
                    Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                    Quad(sub, p0 + d0 * r0, p0 + d1 * r0, p1 + d1 * r1, p1 + d0 * r1, (d0 + d1) * .5f);
                }
            }

            public void Cone(Vector3 p, Vector3 axis, float r, float len, int sides, int sub)
            {
                var u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(axis, u);
                var tip = p + axis * len;
                for (var i = 0; i < sides; i++)
                {
                    float a0 = 2f * Mathf.PI * i / sides, a1 = 2f * Mathf.PI * (i + 1) / sides;
                    Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                    Tri(sub, p + d0 * r, p + d1 * r, tip, (d0 + d1) * .5f + axis * .3f);
                }
            }

            // A grooved low dome (brain coral): hemisphere with ridges, resting on y = 0.
            public void Lump(Vector3 centre, float radius, float height, int segs, int rings, int ridges, float seed, int sub)
            {
                Vector3 P(int ring, int seg)
                {
                    var phi = (Mathf.PI / 2f) * ring / rings; var az = 2f * Mathf.PI * seg / segs;
                    var ridge = 1f + .07f * Mathf.Sin(az * ridges + phi * 4f + seed) * Mathf.Sin(phi * 2f);
                    var rr = radius * Mathf.Sin(phi) * ridge;
                    return centre + new Vector3(Mathf.Cos(az) * rr, height * Mathf.Cos(phi), Mathf.Sin(az) * rr);
                }
                for (var r = 0; r < rings; r++)
                    for (var s = 0; s < segs; s++)
                    {
                        Vector3 a = P(r, s), b = P(r, s + 1), c = P(r + 1, s + 1), d = P(r + 1, s);
                        var away = (a + b + c + d) / 4f - centre;
                        if (r == 0) Tri(sub, a, c, d, away); else Quad(sub, a, b, c, d, away);
                    }
            }

            // A noisy squashed icosphere sitting on y = centre.y (flat-ish base), for rocks and root blocks.
            public void Rock(Vector3 centre, Vector3 radii, int subdiv, int seed, int sub, float roughness)
            {
                var verts = new List<Vector3>(); var faces = new List<int[]>();
                float tt = (1f + Mathf.Sqrt(5f)) / 2f;
                foreach (var p in new[] { new Vector3(-1, tt, 0), new Vector3(1, tt, 0), new Vector3(-1, -tt, 0), new Vector3(1, -tt, 0), new Vector3(0, -1, tt), new Vector3(0, 1, tt), new Vector3(0, -1, -tt), new Vector3(0, 1, -tt), new Vector3(tt, 0, -1), new Vector3(tt, 0, 1), new Vector3(-tt, 0, -1), new Vector3(-tt, 0, 1) }) verts.Add(p.normalized);
                faces.AddRange(new[] { new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 }, new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
                    new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 }, new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 } });
                for (var s = 0; s < subdiv; s++)
                {
                    var cache = new Dictionary<long, int>(); var next = new List<int[]>();
                    int Mid(int a, int b)
                    {
                        var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                        if (cache.TryGetValue(key, out var i)) return i;
                        verts.Add(((verts[a] + verts[b]) * .5f).normalized); cache[key] = verts.Count - 1; return verts.Count - 1;
                    }
                    foreach (var f in faces) { int a = Mid(f[0], f[1]), b = Mid(f[1], f[2]), c = Mid(f[2], f[0]); next.Add(new[] { f[0], a, c }); next.Add(new[] { f[1], b, a }); next.Add(new[] { f[2], c, b }); next.Add(new[] { a, b, c }); }
                    faces = next;
                }
                var shaped = verts.Select(d =>
                {
                    var n = (Noise(d * 1.7f + Vector3.one * 7f, seed) - .5f) * 2f * roughness + (Noise(d * 4.3f, seed + 1) - .5f) * 2f * roughness * .4f;
                    var p = Vector3.Scale(d * (1f + n), radii);
                    p.y = Mathf.Max(p.y, -radii.y * .3f);   // flat bed
                    return p + new Vector3(0f, radii.y * .3f, 0f);
                }).ToArray();
                foreach (var f in faces)
                {
                    Vector3 a = shaped[f[0]], b = shaped[f[1]], c = shaped[f[2]];
                    Tri(sub, a + centre, b + centre, c + centre, (a + b + c) / 3f - new Vector3(0f, radii.y * .3f, 0f));
                }
            }

            // Shift so the lowest point is exactly `y` and the footprint is centred on x/z = 0.
            public void GroundTo(float y)
            {
                var min = v.Min(p => p.y); var cx = (v.Min(p => p.x) + v.Max(p => p.x)) * .5f; var cz = (v.Min(p => p.z) + v.Max(p => p.z)) * .5f;
                for (var i = 0; i < v.Count; i++) v[i] += new Vector3(-cx, y - min, -cz);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(v); mesh.subMeshCount = 2;
                mesh.SetTriangles(t[0], 0); mesh.SetTriangles(t[1], 1);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return SaveMesh(mesh, name);
            }
        }

        // --- helpers ---------------------------------------------------------------------------------

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            var path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh);
            return existing;
        }

        private static Renderer Part(Transform parent, string name, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = materials;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // seabed dressing never needs shadow passes
            return r;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(); var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static Vector3 Round(Vector3 v) => new Vector3(Mathf.Round(v.x * 100f) / 100f, Mathf.Round(v.y * 100f) / 100f, Mathf.Round(v.z * 100f) / 100f);
        private static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        private static Material Mat(string name, Color color, float smooth, bool doubleSided)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Save(GameObject root, string id)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + id + ".prefab"); Object.DestroyImmediate(root);
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // --- review renders (editor preview with the game's underwater atmosphere formulas) -------------------

        // -executeMethod DeepDive.EditorTools.P411ReefKitBuilder.PreviewBatch -previewMode <singles|group-day|group-night> -previewOut <png>
        public static void PreviewBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key, string fallback) { var i = Array.IndexOf(args, key); return i >= 0 ? args[i + 1] : fallback; }
            var mode = Arg("-previewMode", "singles"); var output = Arg("-previewOut", "Logs/p411-preview.png");
            var root = new GameObject("PreviewRoot");
            GameObject Spawn(string id, Vector3 at, float yaw) { var go = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("ReefKit/" + id), root.transform); go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); return go; }
            if (mode == "singles")
            {
                var ids = ReefKitPiece.AllIds; var x = -6f;
                foreach (var id in ids) { Spawn(id, new Vector3(x, 0, 0), 20f); x += id.Contains("arch") ? 4.2f : 2.4f; }
                Render(root, output, new[] { new Vector3(0, 1.1f, -6.5f), new Vector3(-3.5f, 1.2f, -4.5f) }, new[] { new Vector3(.4f, .7f, 0), new Vector3(-4.2f, .5f, 0) }, new[] { 1f, 1f }, new[] { 55f, 40f }, false);
            }
            else
            {
                Spawn("reef-rock-arch", new Vector3(0, 0, 3.5f), 0f); Spawn("reef-rock-boulder", new Vector3(-4.2f, 0, 2f), 70f);
                Spawn("reef-coral-branching", new Vector3(-1.5f, 0, -.5f), 10f); Spawn("reef-coral-branching", new Vector3(2.6f, 0, -1.2f), 140f);
                Spawn("reef-coral-fan", new Vector3(.6f, 0, -1.9f), 8f); Spawn("reef-coral-fan", new Vector3(-3.2f, 0, -1f), -25f);
                Spawn("reef-coral-mound", new Vector3(1.8f, 0, .2f), 0f); Spawn("reef-coral-mound", new Vector3(-2.2f, 0, 0.9f), 40f);
                Spawn("reef-plant-seagrass", new Vector3(3.5f, 0, 1.0f), 0f); Spawn("reef-plant-seagrass", new Vector3(-.4f, 0, 1.2f), 60f);
                var night = mode == "group-night";
                Render(root, output, new[] { new Vector3(0, 1.3f, -6.2f), new Vector3(-5f, 1.6f, -5f), new Vector3(1.5f, .8f, -3.2f), new Vector3(6f, 3f, -7f) },
                    new[] { new Vector3(0, .9f, 1f), new Vector3(0, .8f, 0), new Vector3(0, .5f, 0), new Vector3(0, .6f, 0) }, new[] { 1f, 1f, 1f, 1f }, new[] { 55f, 55f, 50f, 50f }, true, night);
            }
            Debug.Log("P411_PREVIEW_DONE " + output);
        }

        // Underwater atmosphere (CoastalAtmosphere, submerged branch) at the daylight of the given factor.
        private static void Render(GameObject root, string output, Vector3[] cams, Vector3[] targets, float[] unused, float[] fovs, bool underwater, bool night = false)
        {
            const int w = 800, h = 520;
            var daylight = night ? 0f : 1f;
            var ambientK = Mathf.Lerp(.25f, 1f, daylight); var sunK = Mathf.Lerp(.08f, 1f, daylight); var fogK = Mathf.Lerp(.2f, 1f, daylight);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.localScale = new Vector3(80, .1f, 80); floor.transform.position = new Vector3(0, -.05f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = underwater ? new Color(.62f, .58f, .46f) : new Color(.7f, .66f, .52f) };
            var camObject = new GameObject("cam"); var cam = camObject.AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.clearFlags = CameraClearFlags.SolidColor;
            var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(48, -32, 0);
            if (underwater)
            {
                RenderSettings.fog = true; RenderSettings.fogDensity = .045f; RenderSettings.fogColor = Scale(new Color(.04f, .30f, .36f), fogK);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Scale(new Color(.26f, .48f, .52f), ambientK);
                cam.backgroundColor = Scale(new Color(.04f, .30f, .36f), fogK); light.color = new Color(.61f, .85f, .90f); light.intensity = 1f * sunK;
            }
            else
            {
                RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .55f, .6f);
                cam.backgroundColor = new Color(.62f, .78f, .9f); light.intensity = 1.4f;
            }
            var human = GameObject.CreatePrimitive(PrimitiveType.Capsule); human.transform.localScale = new Vector3(.5f, .85f, .5f); human.transform.position = new Vector3(underwater ? 1.5f : 4.2f, .85f, underwater ? -2.4f : -2f);
            human.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.9f, .3f, .25f) };
            var sheet = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
            cam.transform.position = new Vector3(0, 3, -8); cam.transform.LookAt(Vector3.zero); cam.Render();
            for (var k = 0; k < 4; k++)
            {
                var i = Mathf.Min(k, cams.Length - 1);
                cam.transform.position = cams[i]; cam.transform.LookAt(targets[i]); cam.fieldOfView = fovs[i]; cam.Render();
                RenderTexture.active = rt;
                var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
                sheet.SetPixels((k % 2) * w, (1 - k / 2) * h, w, h, t.GetPixels()); Object.DestroyImmediate(t);
            }
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            foreach (var o in new Object[] { camObject, light.gameObject, floor, human, root }) Object.DestroyImmediate(o);
        }

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    }
}
