using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Session;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

public class DiveInventoryBindingTests
{
    private GameObject root;
    private SessionManager session;
    private InventoryManager inventory;
    private DiveInventoryBinding binding;
    private bool authority, active;
    private readonly PlayerId alice = new PlayerId(1), bob = new PlayerId(2);

    [SetUp] public void Setup()
    {
        DiveContext.Unbind(); CatchClaim.Unbind();
        root = new GameObject("P2CompositionTest");
        session = root.AddComponent<SessionManager>();
        inventory = root.AddComponent<InventoryManager>();
        session.Initialize("room", "DiveTestArea");
        session.Join(alice); session.Join(bob);
        authority = active = true;
        binding = new DiveInventoryBinding(session, inventory, () => authority, _ => active);
    }

    [TearDown] public void Cleanup()
    {
        binding.Dispose();
        DiveContext.Unbind(); CatchClaim.Unbind();
        Object.DestroyImmediate(root);
    }

    private void Dive(string id)
    {
        session.SetReady(alice, true); session.SetReady(bob, true);
        Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
        Assert.AreEqual(SessionActionResult.Ok, session.BeginDive(id));
    }

    private CaptureResult Capture(int weight = 500)
    {
        Assert.IsTrue(CaptureBuilder.TryCreate(DiveContext.Source, "fish-1", weight, 7, out var capture));
        return capture;
    }

    [Test] public void KillCaptureClaim_ReplayAndSecondDiverOnlyFillOneBag()
    {
        Dive("one");
        var fish = new FishHealth(1);
        Assert.AreEqual(FishHitOutcome.Killed, fish.ApplyDamage(alice, 1, 1));
        var state = new CatchState();
        Assert.IsTrue(state.Hold(Capture()));
        Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(CatchClaim.Sink, alice, 2, out var consume));
        Assert.IsTrue(consume);
        Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(CatchClaim.Sink, alice, 2, out consume));
        Assert.IsFalse(consume);
        Assert.AreEqual(PlayerActionResult.InvalidTarget, state.TryClaim(CatchClaim.Sink, bob, 1, out consume));
        Assert.IsFalse(consume);
        Assert.AreEqual(1, inventory.Bags[alice].Items.Count);
        Assert.AreEqual(0, inventory.Bags[bob].Items.Count);
    }

    [Test] public void FullBagLeavesCatchForAnotherDiver()
    {
        Dive("one");
        Assert.AreEqual(PlayerActionResult.Accepted, binding.TryClaim(alice, Capture(InventoryManager.CapacityGrams)));
        var state = new CatchState(); state.Hold(Capture());
        Assert.AreEqual(PlayerActionResult.InventoryFull, state.TryClaim(CatchClaim.Sink, alice, 1, out var consume));
        Assert.IsFalse(consume); Assert.IsTrue(state.IsAvailable);
        Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(CatchClaim.Sink, bob, 1, out consume));
        Assert.IsTrue(consume);
    }

    [Test] public void ReturnUnbindsAndSecondDiveRejectsOldCapture()
    {
        Assert.IsNull(DiveContext.Source); Assert.IsNull(CatchClaim.Sink);
        Dive("one"); var old = Capture();
        Assert.AreEqual(PlayerActionResult.Accepted, binding.TryClaim(alice, old));
        session.BeginReturn();
        Assert.IsNull(DiveContext.Source); Assert.IsNull(CatchClaim.Sink);
        Assert.AreEqual("", binding.CurrentDiveId);
        session.CompleteReturn(); Dive("two");
        Assert.AreEqual("two", DiveContext.CurrentDiveId);
        Assert.AreEqual(PlayerActionResult.InvalidTarget, binding.TryClaim(alice, old));
        Assert.AreEqual(0, inventory.Bags[alice].Items.Count);
        Assert.AreEqual(PlayerActionResult.Accepted, binding.TryClaim(alice, Capture()));
    }

    [Test] public void InactiveDiverAndLostAuthorityCannotAddCatch()
    {
        Dive("one"); var capture = Capture(); active = false;
        Assert.AreEqual(PlayerActionResult.InvalidState, binding.TryClaim(alice, capture));
        active = true; authority = false; binding.Refresh();
        Assert.IsNull(DiveContext.Source); Assert.IsNull(CatchClaim.Sink);
        Assert.AreEqual(PlayerActionResult.InvalidState, binding.TryClaim(alice, capture));
    }

    [Test] public void OldBindingDisposalDoesNotClearReplacement()
    {
        Dive("one");
        using (var replacement = new DiveInventoryBinding(session, inventory, () => authority, _ => active))
        {
            binding.Dispose();
            Assert.AreSame(replacement, DiveContext.Source);
            Assert.AreSame(replacement, CatchClaim.Sink);
        }
        Assert.IsNull(DiveContext.Source); Assert.IsNull(CatchClaim.Sink);
    }

    [TestCase(InventoryActionResult.Ok, PlayerActionResult.Accepted)]
    [TestCase(InventoryActionResult.InventoryFull, PlayerActionResult.InventoryFull)]
    [TestCase(InventoryActionResult.WrongPhase, PlayerActionResult.InvalidState)]
    [TestCase(InventoryActionResult.PlayerInactive, PlayerActionResult.InvalidState)]
    [TestCase(InventoryActionResult.InvalidTarget, PlayerActionResult.InvalidTarget)]
    [TestCase(InventoryActionResult.AlreadyClaimed, PlayerActionResult.InvalidTarget)]
    [TestCase((InventoryActionResult)999, PlayerActionResult.Rejected)]
    public void InventoryResultsFailClosed(InventoryActionResult input, PlayerActionResult expected)
        => Assert.AreEqual(expected, DiveInventoryBinding.Map(input));
}
