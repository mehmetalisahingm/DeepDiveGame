using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeepDive.World.Tests
{
    // The shell, tested without a scene and without play mode. Everything here builds a
    // GameObject by hand, which is also the point: in edit mode Awake never runs, so a field
    // that only built its bodies there would read as no water at all.
    public class WaterFieldTests
    {
        private GameObject root;

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        // Mirrors the scene's SwimVolume: a unit box on a transform scaled to the arena.
        private WaterField Field(Vector3 position, Vector3 scale, Quaternion rotation, Vector3 boxCenter, Vector3 boxSize)
        {
            root = new GameObject("WaterFieldUnderTest");
            var volumeObject = new GameObject("Volume");
            volumeObject.transform.SetParent(root.transform, false);
            volumeObject.transform.SetPositionAndRotation(position, rotation);
            volumeObject.transform.localScale = scale;

            var box = volumeObject.AddComponent<BoxCollider>();
            box.center = boxCenter;
            box.size = boxSize;
            box.isTrigger = true;

            var field = root.AddComponent<WaterField>();
            var data = new SerializedObject(field);
            var list = data.FindProperty("volumes");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = box;
            data.FindProperty("deadband").floatValue = WaterTracker.DefaultDeadband;
            data.ApplyModifiedPropertiesWithoutUndo();
            return field;
        }

        private WaterField SceneLikeField() =>
            Field(new Vector3(0, 4, 0), Vector3.one, Quaternion.identity, Vector3.zero, new Vector3(30, 8, 30));

        [Test]
        public void ReadsABoxColliderWithoutAwake()
        {
            var field = SceneLikeField();

            // No Awake, no Start, no play mode - the first read is what builds it.
            Assert.AreEqual(1, field.Bodies.Count);
            var body = field.Bodies[0];
            Assert.AreEqual(8f, body.SurfaceY);
            Assert.AreEqual(-15f, body.MinX);
            Assert.AreEqual(15f, body.MaxX);
            Assert.AreEqual(-15f, body.MinZ);
            Assert.AreEqual(15f, body.MaxZ);
        }

        [Test]
        public void UsesLossyScaleLikeTheSceneVolume()
        {
            // DiveTestArea scales the transform rather than the collider: a unit box on a
            // transform scaled (30, 8, 30). Reading collider.size alone would give a 1 m pond.
            var field = Field(new Vector3(0, 4, 0), new Vector3(30, 8, 30), Quaternion.identity,
                Vector3.zero, Vector3.one);

            Assert.AreEqual(1, field.Bodies.Count);
            var body = field.Bodies[0];
            Assert.AreEqual(8f, body.SurfaceY);
            Assert.AreEqual(-15f, body.MinX);
            Assert.AreEqual(15f, body.MaxX);
        }

        [Test]
        public void ARotatedVolumeIsRefused()
        {
            // The pure layer measures an axis-aligned footprint, so a rotated box would read as
            // its unrotated self - water where there is none. The shell is the only layer that
            // can see the rotation, so it refuses the volume rather than passing it on.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("P3_WATER_VOLUME_ROTATED"));

            var field = Field(new Vector3(0, 4, 0), new Vector3(30, 8, 30), Quaternion.Euler(0, 30, 0),
                Vector3.zero, Vector3.one);

            Assert.AreEqual(0, field.Bodies.Count);
        }

        [Test]
        public void CreateTrackerGivesIndependentTrackers()
        {
            var field = SceneLikeField();
            var dry = field.CreateTracker();
            var wet = field.CreateTracker();
            Assert.AreNotSame(dry, wet);

            // Drive them to opposite states.
            Assert.AreEqual(EnvironmentLocomotion.Land, dry.Classify(Diver(40f)));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, wet.Classify(Diver(2f)));

            // Same place, inside the band from both sides: each holds its own memory, so the
            // two answers must differ. One shared tracker would give the same answer twice.
            // Feet 0.05 under the surface: inside the band from both sides. The dry tracker
            // has not earned the water yet, the wet one has not lost it - one shared tracker
            // could not give both answers.
            var onTheLine = Diver(8f - 0.05f);
            Assert.AreEqual(EnvironmentLocomotion.Land, dry.Classify(onTheLine));
            Assert.AreEqual(EnvironmentLocomotion.Surface, wet.Classify(onTheLine));
        }

        private static WaterProbe Diver(float feetY) =>
            new WaterProbe(new Vector3(0, feetY, 0), new Vector3(0, 0.9f, 0), 0.35f, 1.8f, Vector3.up);
    }
}
