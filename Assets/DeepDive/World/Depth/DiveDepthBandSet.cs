using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // The scene's water line, so a world Y can be turned into a depth and then into a band.
    //
    // It is deliberately NOT a second water authority. Everything about the diver's
    // Land/Surface/Underwater state comes from WaterField -> IWaterField ->
    // NetworkPlayer.SetEnvironmentLocomotionServer, and this type is absent from that chain in
    // four independent ways:
    //
    //   (a) PlayerWaterLocomotionBinding finds its source with FindFirstObjectByType<WaterField>,
    //       and this is a different type, so it cannot be picked up even by accident.
    //   (b) It does not implement IWaterField, so it cannot be assigned to the binding's tracker.
    //   (c) EnvironmentLocomotion appears nowhere in its surface - it cannot produce a value to
    //       hand to the mover.
    //   (d) Nothing here reads or writes WaterField.volumes.
    //
    // (a)-(d) are arguments; the proof is the scene test that deletes this object and asserts
    // every water classification along the shore comes back identical.
    //
    // Who reads it: today the P3.2 beach setup, which refuses to save a scene whose fish has
    // wandered out of the shallow band, and the EditMode tests. P4.4's species and boss depth
    // gating and P4's encyclopedia habitat entry are the named future readers. Not locomotion,
    // now or later.
    [DisallowMultipleComponent]
    public sealed class DiveDepthBandSet : MonoBehaviour
    {
        // SwimVolume's top in DiveTestArea. Authored by DiveTestAreaBeachSetup from the actual
        // collider rather than typed in twice, and pinned to it by an EditMode test, so the two
        // cannot drift apart. The default is only what a freshly added component starts at.
        public const float DefaultSurfaceY = 8f;

        [SerializeField] private float surfaceY = DefaultSurfaceY;

        public float SurfaceY => surfaceY;

        public IReadOnlyList<DepthBand> Bands => DiveDepthBands.All;

        // Positive below the water line, negative above it. Standing on dry sand at y = 8.4
        // therefore reads as -0.4 and belongs to no band, which is the intended answer: being
        // on the beach is not being in shallow water.
        public float DepthAt(float worldY) => surfaceY - worldY;

        public bool TryClassify(float worldY, out DepthBand band) =>
            DiveDepthBands.TryFind(DepthAt(worldY), out band);

        // Editor setup only. There is no runtime caller: the water line does not move during a
        // dive, and a component that could be retuned mid-session would be a second authority
        // over something WaterField already owns.
        public void ConfigureSurface(float y)
        {
            surfaceY = IsFinite(y) ? y : DefaultSurfaceY;
        }

        // Mirrors ServicePointAnchor's guard: a NaN typed into the inspector would make every
        // depth NaN and silently empty the band, so it is repaired rather than propagated.
        private void OnValidate()
        {
            if (!IsFinite(surfaceY)) surfaceY = DefaultSurfaceY;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
