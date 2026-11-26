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

    [TestCase(true,true,true,false,true)]
    [TestCase(false,true,true,false,false)]
    [TestCase(true,false,true,false,false)]
    [TestCase(true,true,false,false,false)]
    [TestCase(true,true,true,true,false)]
    public void DelayedNetworkResultsRequireALiveGuestInDive(bool listening,bool diving,bool hasOwner,bool authority,bool expected)
    {
        Assert.AreEqual(expected,RecordingNetworkBridge.CanReceiveRecordingResult(listening,diving,hasOwner,authority));
    }

    [Test] public void LostOwnerResetClearsTimerAndStatus()
    {
        Reply(1,RecordingCommand.Start,PlayerActionResult.Accepted,true);
        var reset=typeof(RecordingNetworkBridge).GetMethod("ResetLocalPresentation", BindingFlags.Instance | BindingFlags.NonPublic);
        reset.Invoke(bridge,null);
        Assert.IsFalse(bridge.IsRecordingLocal);
        Assert.AreEqual(0,bridge.RecordingElapsedSeconds);
        Assert.AreEqual(string.Empty,bridge.CurrentStatus);
    }

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
