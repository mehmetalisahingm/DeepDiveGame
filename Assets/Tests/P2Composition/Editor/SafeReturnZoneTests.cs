using DeepDive.Composition;
using NUnit.Framework;
using UnityEngine;

public class SafeReturnZoneTests
{
    private GameObject zoneGo;
    private BoxCollider box;

    [SetUp]
    public void Setup()
    {
        zoneGo = new GameObject("SafeReturnZoneTest");
        zoneGo.transform.position = new Vector3(0, 7, 0);
        box = zoneGo.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(30, 2, 30);
    }

    [TearDown]
    public void Cleanup() => Object.DestroyImmediate(zoneGo);

    [Test]
    public void PointAtZoneCenterIsInside()
    {
        Assert.IsTrue(SafeReturnZone.Contains(box, zoneGo.transform.position));
    }

    [Test]
    public void PointWellOutsideZoneIsNotInside()
    {
        Assert.IsFalse(SafeReturnZone.Contains(box, zoneGo.transform.position + new Vector3(100, 0, 0)));
    }

    [Test]
    public void PointJustInsideHorizontalEdgeIsInside()
    {
        Assert.IsTrue(SafeReturnZone.Contains(box, zoneGo.transform.position + new Vector3(14.9f, 0, 0)));
    }

    [Test]
    public void PointJustOutsideHorizontalEdgeIsNotInside()
    {
        Assert.IsFalse(SafeReturnZone.Contains(box, zoneGo.transform.position + new Vector3(15.1f, 0, 0)));
    }

    [Test]
    public void NarrowLandSidePadAcceptsLedgeButRejectsTheWadeCorridor()
    {
        zoneGo.transform.position = new Vector3(9f, 9.3f, -9f);
        box.size = new Vector3(3.5f, 2f, 3.5f);

        var ledgeStandProbe = new Vector3(9f, 8.4f, -9f) + Vector3.up * 0.9f;
        var wadeStandProbe = new Vector3(-6.5f, 7.2f, -5.35f) + Vector3.up * 0.9f;

        Assert.IsTrue(SafeReturnZone.Contains(box, ledgeStandProbe));
        Assert.IsFalse(SafeReturnZone.Contains(box, wadeStandProbe));
    }

    [Test]
    public void RespectsOffsetCenterAndTransformScale()
    {
        zoneGo.transform.localScale = new Vector3(2, 1, 1);
        box.center = new Vector3(5, 0, 0);
        // Local-space offset center (5,0,0) with box half-size 15 on X and a 2x X-scale
        // places the box's world center at local (5,0,0) -> world (10,7,0), half-extent 30.
        Assert.IsTrue(SafeReturnZone.Contains(box, zoneGo.transform.position + new Vector3(10, 0, 0)));
        Assert.IsFalse(SafeReturnZone.Contains(box, zoneGo.transform.position + new Vector3(41, 0, 0)));
    }
}
