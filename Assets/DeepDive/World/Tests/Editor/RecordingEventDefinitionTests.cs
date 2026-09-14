using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class RecordingEventDefinitionTests
    {
        private RecordingEventDefinition definition;
        private RecordingQualityTable quality;

        [SetUp]
        public void CreateDefinition()
        {
            quality = ScriptableObject.CreateInstance<RecordingQualityTable>();
            definition = ScriptableObject.CreateInstance<RecordingEventDefinition>();
            Set("eventId", "event_test");
            SetObject("quality", quality);
        }

        [TearDown]
        public void DestroyDefinition()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(quality);
        }

        private void Set(string field, string value)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Set(string field, float value)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetObject(string field, Object value)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void AFullyConfiguredEventIsValid()
        {
            Assert.IsTrue(definition.IsValid(out var error), error);
        }

        // The id is what reaches Mert's price table as RecordingResult.SubjectId, so an event
        // without one could never be paid for.
        [Test]
        public void AnEventWithNoIdIsRefused()
        {
            Set("eventId", "   ");

            Assert.IsFalse(definition.IsValid(out var error));
            StringAssert.Contains("eventId", error);
        }

        [Test]
        public void TheEventIdIsTrimmedSoItMatchesThePriceRow()
        {
            Set("eventId", "  event_bioluminescence  ");

            Assert.AreEqual("event_bioluminescence", definition.EventId);
        }

        [Test]
        public void DisplayNameIsUiOnlyAndFallsBackToTheId()
        {
            Set("displayName", "");
            Assert.AreEqual(definition.EventId, definition.DisplayName);

            Set("displayName", "Biyoluminesans");
            Assert.AreEqual("Biyoluminesans", definition.DisplayName);
            Assert.AreEqual("event_test", definition.EventId, "the display name is never the identity");
        }

        [Test]
        public void AnEventWithNoQualityTableCannotBeFilmed()
        {
            SetObject("quality", null);

            Assert.IsFalse(definition.IsValid(out var error));
            StringAssert.Contains("quality table", error);
        }

        [Test]
        public void AnUnusableQualityTableIsReportedWithItsOwnReason()
        {
            var broken = ScriptableObject.CreateInstance<RecordingQualityTable>();
            var serialized = new SerializedObject(broken);
            serialized.FindProperty("tiers").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetObject("quality", broken);

            Assert.IsFalse(definition.IsValid(out var error));
            StringAssert.Contains("quality table:", error);

            Object.DestroyImmediate(broken);
        }

        [Test]
        public void ANonsenseTriggerDelayIsRefused()
        {
            foreach (var delay in new[] { -1f, float.NaN, float.PositiveInfinity })
            {
                Set("triggerDelaySeconds", delay);
                Assert.IsFalse(definition.IsValid(out var error), "delay " + delay);
                StringAssert.Contains("triggerDelaySeconds", error);
            }
        }

        [Test]
        public void AWindowWithNoLengthIsRefused()
        {
            foreach (var window in new[] { 0f, -5f, float.NaN, float.PositiveInfinity })
            {
                Set("windowSeconds", window);
                Assert.IsFalse(definition.IsValid(out var error), "window " + window);
                StringAssert.Contains("windowSeconds", error);
            }
        }

        [Test]
        public void ASubjectWithNoSizeIsRefused()
        {
            foreach (var radius in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Set("subjectRadiusMetres", radius);
                Assert.IsFalse(definition.IsValid(out var error), "radius " + radius);
                StringAssert.Contains("subjectRadiusMetres", error);
            }
        }

        // Mert has not delivered the particle/light/sound yet, and the event must still work
        // without them: a silent event is a working event, not a setup fault.
        [Test]
        public void AMissingSignalClipIsLegal()
        {
            Assert.IsNull(definition.StartClip);
            Assert.IsTrue(definition.IsValid(out var error), error);
        }

        [Test]
        public void TheRuleObjectsCarryTheDefinitionsNumbers()
        {
            Set("triggerDelaySeconds", 45f);
            Set("windowSeconds", 20f);

            var schedule = definition.CreateSchedule();
            var window = definition.CreateWindow();

            Assert.AreEqual(45f, schedule.DelaySeconds, 0.0001f);
            Assert.AreEqual(20f, window.DurationSeconds, 0.0001f);
            Assert.IsTrue(schedule.IsValid);
            Assert.IsTrue(window.IsValid);
        }

        // The asset holds numbers, never live state: two callers must not share one window.
        [Test]
        public void EachCallerGetsItsOwnRuleObjects()
        {
            Assert.AreNotSame(definition.CreateWindow(), definition.CreateWindow());
            Assert.AreNotSame(definition.CreateSchedule(), definition.CreateSchedule());
        }
    }

    // The shipped asset, as authored by DeepDive/P3/Recording event: bioluminescence. Separate
    // from the rules above so a retune of the asset cannot quietly change what those claim.
    public class BioluminescenceEventAssetTests
    {
        private const string DefinitionPath =
            "Assets/DeepDive/World/Events/Definitions/Bioluminescence.asset";

        private RecordingEventDefinition definition;

        [SetUp]
        public void LoadDefinition()
        {
            definition = AssetDatabase.LoadAssetAtPath<RecordingEventDefinition>(DefinitionPath);
            Assert.IsNotNull(definition, "missing asset: run DeepDive/P3/Recording event: bioluminescence");
        }

        [Test]
        public void TheShippedEventIsValid()
        {
            Assert.IsTrue(definition.IsValid(out var error), error);
        }

        // Mehmet, 14 September 2026. These are the agreed numbers, not tuning: changing them is
        // a design decision, and this test is where that shows up.
        [Test]
        public void TheShippedEventCarriesTheAgreedScheduleAndId()
        {
            Assert.AreEqual("event_bioluminescence", definition.EventId);
            Assert.AreEqual(45f, definition.TriggerDelaySeconds, 0.0001f, "45 s after the dive starts");
            Assert.AreEqual(20f, definition.WindowSeconds, 0.0001f, "open for 20 s");
        }

        [Test]
        public void TheShippedEventHasItsOwnQualityTable()
        {
            var quality = definition.Quality;

            Assert.IsNotNull(quality);
            Assert.IsTrue(quality.IsValid(out var error), error);
            Assert.AreEqual(4, quality.TierCount, "Mert prices four rungs, per CONTRACTS.md");
            Assert.AreNotEqual("DefaultRecordingQuality", quality.name,
                "the plankton cluster is not framed like the fish");
        }

        // A cluster is much bigger than the fish, and the band it is filmed in has to reflect
        // that or the whole event grades as one long too-close shot.
        [Test]
        public void TheShippedEventIsFramedAsSomethingLargerThanAFish()
        {
            var framing = definition.Quality.Framing;

            Assert.Greater(definition.SubjectRadiusMetres, 1f);
            Assert.Greater(framing.MinDistanceMetres, 1.5f, "closer than this it overfills the frame");
            Assert.Greater(framing.MaxDistanceMetres, framing.MinDistanceMetres);
            Assert.Greater(framing.IdealFrameFill, framing.MinFrameFill);
        }

        // Platinum needs 8 seconds of held framing and the window is 20 seconds long, so the
        // top rung has to be reachable inside one appearance of the event.
        [Test]
        public void TheTopTierIsReachableInsideTheWindow()
        {
            var topTier = definition.Quality.Tiers[definition.Quality.TierCount - 1];

            Assert.Less(topTier.MinValidSeconds, definition.WindowSeconds,
                "a tier nobody could ever reach is not a tier");
        }

        // The start clip is Mert's to deliver; an empty one is legal and stays silent, which is
        // why only the colour is asserted here.
        [Test]
        public void TheSignalColourIsBlueTurquoise()
        {
            var colour = definition.SignalColor;

            Assert.Greater(colour.b, colour.r, "blue/turquoise, per Mehmet");
            Assert.Greater(colour.g, colour.r);
        }
    }
}
