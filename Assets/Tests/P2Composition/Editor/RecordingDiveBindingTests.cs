using System.Collections.Generic;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Session;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

public sealed class RecordingDiveBindingTests
{
    private GameObject root;
    private SessionManager session;
    private InventoryManager inventory;
    private DiveInventoryBinding dive;
    private RecordingDiveBinding binding;
    private bool authority = true;
    private readonly PlayerId alice = new PlayerId(1), bob = new PlayerId(2);
    private readonly List<RecordingResult> credited = new List<RecordingResult>();

    [SetUp] public void Setup()
    {
        if (RecordingEvaluation.Sink != null) RecordingEvaluation.Unbind(RecordingEvaluation.Sink);
        DiveContext.Unbind(); CatchClaim.Unbind(); RecordingClaim.Unbind(); RecorderViews.UnbindAll();
        authority = true; credited.Clear();
        root = new GameObject("RecordingCompositionTest");
        session = root.AddComponent<SessionManager>();
        inventory = root.AddComponent<InventoryManager>();
        session.Initialize("room", "DiveTestArea"); session.Join(alice); session.Join(bob);
        // EditMode AddComponent does not run Awake; an ordinary inventory API call
        // establishes its lazy session subscription before the first phase change.
        Assert.AreEqual(InventoryActionResult.WrongPhase, inventory.TryMarkSafeReturn(alice));
        dive = new DiveInventoryBinding(session, inventory, () => authority, _ => true);
        binding = new RecordingDiveBinding(session, inventory, () => authority);
    }
    [TearDown] public void Cleanup()
    {
        binding.Dispose(); dive.Dispose();
        Object.DestroyImmediate(root);
    }
    private void EnterDive(string id = "dive-1")
    {
        session.SetReady(alice, true); session.SetReady(bob, true);
        Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
        Assert.AreEqual(SessionActionResult.Ok, session.BeginDive(id));
    }
    private PlayerActionResult Credit(RecordingResult result)
    { credited.Add(result); return PlayerActionResult.Accepted; }
    private void Register(PlayerId player, int quality)
    {
        var subject = new Subject(quality);
        Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStart(new RecordingCandidate(1, session.State.DiveId, player, subject)));
        Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStop(new RecordingCandidate(2, session.State.DiveId, player, subject)));
    }

    [Test] public void DiveOwnsEvaluationAndViews_ReturnReleasesThem()
    {
        Assert.IsNull(RecordingEvaluation.Sink);
        EnterDive();
        Assert.AreSame(binding.Director, RecordingEvaluation.Sink);
        binding.BindView(alice, new View());
        Assert.IsTrue(RecorderViews.IsBound(alice));
        session.BeginReturn();
        Assert.IsNull(RecordingEvaluation.Sink);
        Assert.IsFalse(RecorderViews.IsBound(alice));
    }

    [Test] public void SummaryPaysNextBestSafeRecorderOnce_NotOnStop()
    {
        binding.SetPaymentHandler(Credit);
        EnterDive(); Register(alice, 4); Register(bob, 2);
        Assert.IsEmpty(credited);
        inventory.TryMarkSafeReturn(bob);
        session.BeginReturn();
        Assert.AreEqual(1, credited.Count);
        Assert.AreEqual(bob, credited[0].PlayerId);
        Assert.AreEqual(2, credited[0].Quality);
        binding.Refresh(); binding.SetPaymentHandler(Credit);
        Assert.AreEqual(1, credited.Count);
    }

    [Test] public void MissingBackendRetainsOldSettlementWithoutPollutingNextDive()
    {
        EnterDive(); Register(alice, 4); inventory.TryMarkSafeReturn(alice);
        var oldDirector = binding.Director;
        session.BeginReturn();
        Assert.IsFalse(RecordingClaim.IsBound);
        Assert.AreEqual(1, binding.PendingDiveCount);
        Assert.IsFalse(oldDirector.IsSettled);
        session.CompleteReturn(); EnterDive("dive-2");
        Assert.AreNotSame(oldDirector, binding.Director);
        Assert.AreEqual(0, binding.Director.ClaimCount);
        binding.SetPaymentHandler(Credit);
        Assert.AreEqual(1, credited.Count);
        Assert.AreEqual("dive-1", credited[0].DiveId);
        Assert.AreEqual(0, binding.PendingDiveCount);
        Assert.IsFalse(binding.Director.IsSettled);
    }

    [Test] public void NonHostCannotBindEvaluationViewsOrPayment()
    {
        authority = false; binding.SetPaymentHandler(Credit);
        EnterDive(); binding.BindView(alice, new View());
        Assert.IsNull(RecordingEvaluation.Sink);
        Assert.IsFalse(RecordingClaim.IsBound);
        Assert.IsFalse(RecorderViews.IsBound(alice));
        Assert.AreEqual(PlayerActionResult.InvalidState, binding.TryClaim(default));
    }

    [Test] public void NewRoomClearsPendingClaims_DisposeClearsPayment()
    {
        EnterDive(); Register(alice, 4); session.BeginReturn();
        Assert.AreEqual(1, binding.PendingDiveCount);
        session.Initialize("other-room", "DiveTestArea");
        Assert.AreEqual(0, binding.PendingDiveCount);
        binding.SetPaymentHandler(Credit);
        Assert.IsEmpty(credited);
        binding.Dispose();
        Assert.IsFalse(RecordingClaim.IsBound);
        Assert.IsNull(RecordingEvaluation.Sink);
    }

    // Boundary doubles only: the production Director and ledger decide payability/settlement.
    private sealed class Subject : IRecordingSubject
    {
        private readonly int quality;
        public Subject(int quality) { this.quality = quality; }
        public string SubjectId => "sea_bass";
        public PlayerActionResult TryStartTake(PlayerId player, ulong requestId) => PlayerActionResult.Accepted;
        public PlayerActionResult TryStopTake(PlayerId player, ulong requestId, out RecordingTake take)
        {
            take = new RecordingTake(DiveContext.CurrentDiveId, player, SubjectId, quality, 8, .9f);
            return PlayerActionResult.Accepted;
        }
    }
    private sealed class View : IRecorderView
    {
        public bool IsActive => true;
        public Vector3 EyePosition => Vector3.zero;
        public Vector3 EyeForward => Vector3.forward;
        public float VerticalFieldOfViewDegrees => 60;
    }
}
