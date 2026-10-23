using System.Reflection;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class RecordingHudStateTests
{
    private GameObject root;
    private RecordingNetworkBridge bridge;
    private static readonly MethodInfo Apply = typeof(RecordingNetworkBridge).GetMethod("ApplyLocalResult", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Started = typeof(RecordingNetworkBridge).GetField("recordingStartedAt", BindingFlags.Instance | BindingFlags.NonPublic);

    [SetUp] public void Setup() { root = new GameObject("RecordingHudState"); bridge = root.AddComponent<RecordingNetworkBridge>(); }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
    private void Reply(ulong request, RecordingCommand command, PlayerActionResult result, bool active) =>
        Apply.Invoke(bridge, new object[] { request, command, result, active, active ? 42UL : 0UL });

    [Test] public void RejectedStartNeverTurnsOnRecordingTally()
    {
        Reply(1, RecordingCommand.Start, PlayerActionResult.InvalidTarget, false);
        Assert.IsFalse(bridge.IsRecordingLocal);
        Assert.AreEqual(0, bridge.RecordingElapsedSeconds);
    }

    [Test] public void RejectedStopKeepsTheAcceptedRecordingAndOriginalTimer()
    {
        Reply(1, RecordingCommand.Start, PlayerActionResult.Accepted, true);
        var start = Time.unscaledTimeAsDouble - 9;
        Started.SetValue(bridge, start);
        Reply(2, RecordingCommand.Stop, PlayerActionResult.Rejected, true);
        Assert.IsTrue(bridge.IsRecordingLocal);
        Assert.AreEqual(start, Started.GetValue(bridge));
        Assert.GreaterOrEqual(bridge.RecordingElapsedSeconds, 9);
    }

    [Test] public void StaleReplyCannotChangeTallyAndAcceptedStopResetsTimer()
    {
        Reply(2, RecordingCommand.Start, PlayerActionResult.Accepted, true);
        Reply(1, RecordingCommand.Stop, PlayerActionResult.Accepted, false);
        Assert.IsTrue(bridge.IsRecordingLocal);
        Reply(3, RecordingCommand.Stop, PlayerActionResult.Accepted, false);
        Assert.IsFalse(bridge.IsRecordingLocal);
        Assert.AreEqual(0, bridge.RecordingElapsedSeconds);
    }
}
