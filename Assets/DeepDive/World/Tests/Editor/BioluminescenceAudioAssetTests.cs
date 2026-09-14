using NUnit.Framework;
using UnityEditor;

namespace DeepDive.World.Tests
{
    public class BioluminescenceAudioAssetTests
    {
        private const string DefinitionPath =
            "Assets/DeepDive/World/Events/Definitions/Bioluminescence.asset";

        [Test]
        public void ShippedEventHasAResolvableStartCue()
        {
            var definition = AssetDatabase.LoadAssetAtPath<RecordingEventDefinition>(DefinitionPath);

            Assert.IsNotNull(definition, "missing shipped bioluminescence definition");
            Assert.IsNotNull(definition.StartClip, "P3 special event needs a basic audible start cue");
            Assert.IsFalse(string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(definition.StartClip)),
                "start cue must resolve to a project asset");
        }
    }
}
