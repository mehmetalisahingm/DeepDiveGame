using DeepDive.Composition;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class NaturalSeabedContourMeshTests
    {
        [Test]
        public void NaturalPatchesMatchTheFourAuthoredFloorBandsWithoutAddingAZone()
        {
            var bands = NaturalSeabedContourMesh.Bands;
            Assert.AreEqual(4, bands.Length);
            Assert.AreEqual(15f, bands[0].StartZ);
            Assert.AreEqual(65f, bands[3].EndZ);
            Assert.AreEqual(0f, bands[0].StartY);
            Assert.AreEqual(-26f, bands[3].EndY);
            for (var i = 1; i < bands.Length; i++)
            {
                Assert.AreEqual(bands[i - 1].EndZ, bands[i].StartZ);
                Assert.AreEqual(bands[i - 1].EndY, bands[i].StartY);
                foreach (var x in new[] { -14f, 0f, 14f })
                    Assert.That(Mathf.Abs(
                        NaturalSeabedContourMesh.HeightAt(bands[i - 1], x, bands[i - 1].EndZ) -
                        NaturalSeabedContourMesh.HeightAt(bands[i], x, bands[i].StartZ)),
                        Is.LessThan(0.0001f));
            }
        }

        [Test]
        public void ContoursFollowExistingPhysicsFloorWithinFifteenCentimetres()
        {
            foreach (var band in NaturalSeabedContourMesh.Bands)
            {
                for (var xi = 0; xi <= 30; xi++)
                    for (var zi = 0; zi <= 20; zi++)
                    {
                        var x = Mathf.Lerp(-15f, 15f, xi / 30f);
                        var t = zi / 20f;
                        var z = Mathf.Lerp(band.StartZ, band.EndZ, t);
                        var authoritativeFloor = Mathf.Lerp(band.StartY, band.EndY, t);
                        var offset = NaturalSeabedContourMesh.HeightAt(band, x, z) - authoritativeFloor;
                        Assert.GreaterOrEqual(offset, 0.017f);
                        Assert.LessOrEqual(offset, 0.154f);
                    }
            }
        }

        [Test]
        public void GeneratedSeabedIsABoundedUpwardFacingMeshWithoutPhysics()
        {
            foreach (var band in NaturalSeabedContourMesh.Bands)
            {
                var mesh = NaturalSeabedContourMesh.Generate(band);
                try
                {
                    var zSegments = Mathf.Max(2, Mathf.CeilToInt(band.EndZ - band.StartZ));
                    Assert.AreEqual((NaturalSeabedContourMesh.WidthSegments + 1) * (zSegments + 1),
                        mesh.vertexCount);
                    Assert.AreEqual(NaturalSeabedContourMesh.WidthSegments * zSegments * 6,
                        mesh.triangles.Length);
                    Assert.AreEqual(mesh.vertexCount, mesh.normals.Length);
                    Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
                    Assert.AreEqual(-15f, mesh.bounds.min.x, 0.001f);
                    Assert.AreEqual(15f, mesh.bounds.max.x, 0.001f);
                    Assert.AreEqual(band.StartZ, mesh.bounds.min.z, 0.001f);
                    Assert.AreEqual(band.EndZ, mesh.bounds.max.z, 0.001f);
                    foreach (var normal in mesh.normals)
                        Assert.Greater(normal.y, 0f, "Triangles must face upward for seabed lighting");
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }
        }
    }
}
