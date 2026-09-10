using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class CatchStateTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        // Stands in for Composition's bridge to Mert's InventoryManager. Counting the calls is
        // how the tests prove the bag is never asked twice for the same request.
        private sealed class FakeBag : ICatchClaimSink
        {
            public PlayerActionResult Answer = PlayerActionResult.Accepted;
            public int Calls { get; private set; }
            public readonly List<string> ClaimedCaptureIds = new List<string>();

            public PlayerActionResult TryClaim(PlayerId player, CaptureResult capture)
            {
                Calls++;
                if (Answer == PlayerActionResult.Accepted) ClaimedCaptureIds.Add(capture.CaptureId);
                return Answer;
            }
        }

        private static CaptureResult Capture(string captureId = "capture-1", string diveId = "dive-1") =>
            new CaptureResult(captureId, diveId, "reef-bass", 850, 42);

        private static CatchState ArmedCatch()
        {
            var state = new CatchState();
            Assert.IsTrue(state.Hold(Capture()));
            return state;
        }

        [Test]
        public void OnlyAnAcceptedClaimConsumesTheCatch()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };

            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 1, out var consume));
            Assert.IsTrue(consume);
            Assert.IsTrue(state.IsClaimed);
            Assert.IsFalse(state.IsAvailable);
            Assert.AreEqual(new[] { "capture-1" }, bag.ClaimedCaptureIds);
        }

        [Test]
        public void FullBagLeavesTheCatchOnTheGround()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.InventoryFull };

            Assert.AreEqual(PlayerActionResult.InventoryFull, state.TryClaim(bag, Alice, 1, out var consume));
            Assert.IsFalse(consume, "a refused claim must never consume the catch");
            Assert.IsFalse(state.IsClaimed);
            Assert.IsTrue(state.IsAvailable, "the catch stays collectable after a full bag");
        }

        [Test]
        public void NoRejectionFromTheBagEverConsumesTheCatch()
        {
            foreach (var answer in new[]
                     {
                         PlayerActionResult.InventoryFull, PlayerActionResult.InvalidState,
                         PlayerActionResult.InvalidTarget, PlayerActionResult.TooFast,
                         PlayerActionResult.DuplicateRequest, PlayerActionResult.Rejected
                     })
            {
                var state = ArmedCatch();
                var bag = new FakeBag { Answer = answer };

                Assert.AreEqual(answer, state.TryClaim(bag, Alice, 1, out var consume));
                Assert.IsFalse(consume, $"{answer} must leave the catch on the ground");
                Assert.IsTrue(state.IsAvailable, $"{answer} must leave the catch collectable");
            }
        }

        [Test]
        public void UnboundSinkIsRefusedAndNothingIsConsumed()
        {
            var state = ArmedCatch();

            Assert.AreEqual(PlayerActionResult.Rejected, state.TryClaim(null, Alice, 1, out var consume));
            Assert.IsFalse(consume, "a missing bag binding must never be treated as a successful pickup");
            Assert.IsFalse(state.IsClaimed);
            Assert.IsTrue(state.IsAvailable);
        }

        [Test]
        public void ARefusalFromAnUnboundSinkIsNotRememberedSoALaterAttemptStillWorks()
        {
            var state = ArmedCatch();
            Assert.AreEqual(PlayerActionResult.Rejected, state.TryClaim(null, Alice, 1, out _));

            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };
            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 1, out var consume));
            Assert.IsTrue(consume);
            Assert.AreEqual(1, bag.Calls);
        }

        [Test]
        public void ReplayedRequestReturnsTheEarlierResultWithoutAskingTheBagAgain()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.InventoryFull };

            Assert.AreEqual(PlayerActionResult.InventoryFull, state.TryClaim(bag, Alice, 5, out _));
            Assert.AreEqual(PlayerActionResult.InventoryFull, state.TryClaim(bag, Alice, 5, out var consume));
            Assert.IsFalse(consume);
            Assert.AreEqual(1, bag.Calls, "the same request must not reach the bag twice");
        }

        [Test]
        public void ReplayedRequestAfterASuccessDoesNotAddTheCatchTwice()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };

            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 5, out var first));
            Assert.IsTrue(first);
            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 5, out var second));
            Assert.IsFalse(second, "the catch is already gone; it must not be consumed a second time");
            Assert.AreEqual(1, bag.Calls);
            Assert.AreEqual(1, bag.ClaimedCaptureIds.Count);
        }

        [Test]
        public void RetryingWithANewRequestIdAsksTheBagAgainAfterAFullBag()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.InventoryFull };
            Assert.AreEqual(PlayerActionResult.InventoryFull, state.TryClaim(bag, Alice, 1, out _));

            bag.Answer = PlayerActionResult.Accepted;
            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 2, out var consume));
            Assert.IsTrue(consume, "a full bag is not permanent; a fresh request may still succeed");
            Assert.AreEqual(2, bag.Calls);
        }

        [Test]
        public void TwoDiversRacingForTheSameCatchOnlyFillOneBag()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };

            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 1, out var aliceConsumes));
            Assert.AreEqual(PlayerActionResult.InvalidTarget, state.TryClaim(bag, Bob, 1, out var bobConsumes));
            Assert.IsTrue(aliceConsumes);
            Assert.IsFalse(bobConsumes);
            Assert.AreEqual(1, bag.Calls, "the second diver must not reach the bag at all");
            Assert.AreEqual(1, bag.ClaimedCaptureIds.Count);
        }

        [Test]
        public void AnUnarmedCatchCannotBePickedUp()
        {
            var state = new CatchState();
            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };

            Assert.IsFalse(state.IsAvailable);
            Assert.AreEqual(PlayerActionResult.InvalidTarget, state.TryClaim(bag, Alice, 1, out var consume));
            Assert.IsFalse(consume);
            Assert.AreEqual(0, bag.Calls, "a live fish is not a catch; the bag must not be asked");
        }

        [Test]
        public void HoldRefusesACaptureWithoutIdsAndCannotArmTwice()
        {
            var state = new CatchState();
            Assert.IsFalse(state.Hold(default));
            Assert.IsFalse(state.Hold(new CaptureResult("", "dive-1", "reef-bass", 850, 42)));
            Assert.IsFalse(state.Hold(new CaptureResult("capture-1", "", "reef-bass", 850, 42)));
            Assert.IsFalse(state.IsArmed);

            Assert.IsTrue(state.Hold(Capture()));
            Assert.IsFalse(state.Hold(Capture("capture-2")), "an armed catch must not be re-armed");
            Assert.AreEqual("capture-1", state.Capture.CaptureId);
        }

        [Test]
        public void ResetClearsTheCatchAndItsHandledRequestsForTheNextDive()
        {
            var state = ArmedCatch();
            var bag = new FakeBag { Answer = PlayerActionResult.Accepted };
            state.TryClaim(bag, Alice, 1, out _);

            state.Reset();
            Assert.IsFalse(state.IsArmed);
            Assert.IsFalse(state.IsClaimed);
            Assert.IsFalse(state.IsAvailable);

            Assert.IsTrue(state.Hold(Capture("capture-2", "dive-2")));
            Assert.AreEqual(PlayerActionResult.Accepted, state.TryClaim(bag, Alice, 1, out var consume));
            Assert.IsTrue(consume, "the old dive's request id must not block the new dive");
        }
    }

    public class CatchClaimBindingTests
    {
        private sealed class FakeBag : ICatchClaimSink
        {
            public PlayerActionResult TryClaim(PlayerId player, CaptureResult capture) => PlayerActionResult.Accepted;
        }

        [TearDown]
        public void ClearBinding() => CatchClaim.Unbind();

        [Test]
        public void SinkStartsUnboundAndBindingIsReversible()
        {
            CatchClaim.Unbind();
            Assert.IsFalse(CatchClaim.IsBound);
            Assert.IsNull(CatchClaim.Sink);

            CatchClaim.Bind(new FakeBag());
            Assert.IsTrue(CatchClaim.IsBound);

            CatchClaim.Unbind();
            Assert.IsFalse(CatchClaim.IsBound);
        }
    }
}
