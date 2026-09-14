using System;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeepDive.World.Tests
{
    // SpecialEventPresenter.Apply, driven directly: nothing here spawns a NetworkObject or runs
    // Update, so these tests say what each cue does to whatever components are assigned - not
    // that a live client actually glows, which needs the scene and a running session. In
    // particular a ParticleSystem or AudioSource is only shown not to throw or log here; whether
    // it visibly plays or is heard is not something EditMode can tell.
    public class SpecialEventPresenterTests
    {
        private GameObject host;
        private RecordingEventDefinition definition;

        private static readonly SpecialEventCue[] AllCues =
            (SpecialEventCue[])Enum.GetValues(typeof(SpecialEventCue));

        [SetUp]
        public void CreateObjects()
        {
            host = new GameObject("SpecialEventPresenterTests");
            definition = ScriptableObject.CreateInstance<RecordingEventDefinition>();
        }

        [TearDown]
        public void DestroyObjects()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(definition);
        }

        // The rule that keeps the scene green until Mert delivers: an empty field is skipped,
        // and nothing - not an error, not a warning, not an info line - reaches the console.
        [Test]
        public void EveryCueIsSilentWithNothingAssigned()
        {
            foreach (var cue in AllCues)
            {
                SpecialEventPresenter.Apply(cue, null, null, null, null);
                SpecialEventPresenter.Apply(cue, null, null, null, definition);
            }

            LogAssert.NoUnexpectedReceived();
        }

        // The shipped Bioluminescence asset has no start clip yet. Unity warns when PlayOneShot
        // is handed a null clip, so the presenter has to check before it gets there.
        [Test]
        public void AnAudioSourceWithoutAClipStaysSilent()
        {
            var audio = host.AddComponent<AudioSource>();
            Assert.IsNull(definition.StartClip);

            SpecialEventPresenter.Apply(SpecialEventCue.ShowWithStartSound, null, null, audio, definition);
            SpecialEventPresenter.Apply(SpecialEventCue.ShowWithStartSound, null, null, audio, null);

            Assert.IsFalse(audio.isPlaying);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ShowingLightsTheLampInTheSignalColour()
        {
            foreach (var cue in new[] { SpecialEventCue.ShowWithStartSound, SpecialEventCue.ShowSilently })
            {
                var light = host.AddComponent<Light>();
                light.enabled = false;
                light.color = Color.red;

                SpecialEventPresenter.Apply(cue, null, light, null, definition);

                Assert.IsTrue(light.enabled, cue.ToString());
                AssertColour(definition.SignalColor, light.color, cue.ToString());
                Object.DestroyImmediate(light);
            }
        }

        [Test]
        public void FadingAndHidingPutTheLampOut()
        {
            foreach (var cue in new[] { SpecialEventCue.Fade, SpecialEventCue.HideAtOnce })
            {
                var light = host.AddComponent<Light>();
                light.enabled = true;

                SpecialEventPresenter.Apply(cue, null, light, null, definition);

                Assert.IsFalse(light.enabled, cue.ToString());
                Object.DestroyImmediate(light);
            }
        }

        [Test]
        public void NoneLeavesTheLampAlone()
        {
            var light = host.AddComponent<Light>();
            light.enabled = true;
            light.color = Color.red;

            SpecialEventPresenter.Apply(SpecialEventCue.None, null, light, null, definition);

            Assert.IsTrue(light.enabled);
            AssertColour(Color.red, light.color, "None");
        }

        // Without a definition there is no signal colour to apply, and a guessed default would
        // hide whatever colour the light was authored with.
        [Test]
        public void WithoutADefinitionTheLampKeepsItsOwnColour()
        {
            var light = host.AddComponent<Light>();
            light.enabled = false;
            light.color = Color.red;

            SpecialEventPresenter.Apply(SpecialEventCue.ShowSilently, null, light, null, null);

            Assert.IsTrue(light.enabled);
            AssertColour(Color.red, light.color, "no definition");
        }

        // Utku, 14 September 2026: the signal colour is the light's only. The particle asset is
        // Mert's, and the presenter starts and stops it without touching how it looks.
        [Test]
        public void TheParticlesKeepTheirAuthoredColour()
        {
            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = Color.red;

            foreach (var cue in AllCues)
                SpecialEventPresenter.Apply(cue, particles, null, null, definition);

            AssertColour(Color.red, particles.main.startColor.color, "particle start colour");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EveryCueIsSilentWithEverythingAssigned()
        {
            var particles = host.AddComponent<ParticleSystem>();
            var light = host.AddComponent<Light>();
            var audio = host.AddComponent<AudioSource>();

            foreach (var cue in AllCues)
                SpecialEventPresenter.Apply(cue, particles, light, audio, definition);

            LogAssert.NoUnexpectedReceived();
        }

        // RequireComponent, rather than a null check in Update, is what makes "presenter with no
        // runner" impossible to build - so it is not a setup fault anybody has to log.
        [Test]
        public void ThePresenterCannotBeAddedWithoutItsRunner()
        {
            var required = (RequireComponent[])Attribute.GetCustomAttributes(
                typeof(SpecialEventPresenter), typeof(RequireComponent));

            Assert.IsTrue(Array.Exists(required, r => r.m_Type0 == typeof(SpecialEventRunner)
                                                     || r.m_Type1 == typeof(SpecialEventRunner)
                                                     || r.m_Type2 == typeof(SpecialEventRunner)),
                "SpecialEventPresenter must require SpecialEventRunner on the same object");
        }

        // Presentation only: no network state, no RPCs, no NetworkBehaviour slot on the event's
        // NetworkObject. If this ever has to become a NetworkBehaviour, something that is not
        // presentation has moved into it.
        [Test]
        public void ThePresenterCarriesNoNetworkState()
        {
            Assert.IsFalse(typeof(NetworkBehaviour).IsAssignableFrom(typeof(SpecialEventPresenter)));
        }

        private static void AssertColour(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.0001f, message + " (r)");
            Assert.AreEqual(expected.g, actual.g, 0.0001f, message + " (g)");
            Assert.AreEqual(expected.b, actual.b, 0.0001f, message + " (b)");
        }
    }
}
