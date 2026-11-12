using System.Collections.Generic;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    // #278: read-only player feedback is driven by a real transition from queued to host-settled.
    // Tests use the same publication and media snapshots that the host mirrors to real clients.
    public sealed class P278ChannelSettlementFeedbackTests
    {
        private static PublicationSave Publication(string clip, string title, bool settled,
            int views = 0, int followers = 0, int income = 0)
        {
            var publicationId = ChannelIds.PublicationId(clip);
            return new PublicationSave
            {
                ClipId = clip,
                PublicationId = publicationId,
                Title = title,
                QueuedDay = 1,
                SettledId = settled ? ChannelIds.SettleId(publicationId) : "",
                Views = views,
                FollowersGained = followers,
                Income = income
            };
        }

        [Test]
        public void QueuedPublicationShowsVerifiedBeforeAfterOnlyWhenItSettles()
        {
            var queued = new MediaSaveData { Followers = 100 };
            queued.Publications.Add(Publication("clip1", "Ilk yayin", false));
            var oldPending = ChannelSettlementFeedbackRules.Pending(queued);
            Assert.AreEqual(1, oldPending.Count);
            Assert.AreEqual(0, ChannelSettlementFeedbackRules.NewlySettled(queued, oldPending, 100).Count);

            var settled = new MediaSaveData { Followers = 126 };
            settled.Publications.Add(Publication("clip1", "Ilk yayin", true, 900, 26, 75));
            var result = ChannelSettlementFeedbackRules.NewlySettled(settled, oldPending, 100);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Ilk yayin", result.Title);
            Assert.AreEqual(900, result.Views);
            Assert.AreEqual(26, result.FollowersGained);
            Assert.AreEqual(75, result.Income);
            Assert.AreEqual(100, result.FollowersBefore);
            Assert.AreEqual(126, result.FollowersAfter);
            Assert.AreEqual(0, ChannelSettlementFeedbackRules.Pending(settled).Count);
            Assert.AreEqual(0, ChannelSettlementFeedbackRules.NewlySettled(
                settled, ChannelSettlementFeedbackRules.Pending(settled), 126).Count,
                "same publication cannot celebrate again on later mirror revisions");
        }

        [Test]
        public void ReloadAndLateJoinDoNotCelebrateOldSettledHistory()
        {
            var historic = new MediaSaveData { Followers = 500 };
            historic.Publications.Add(Publication("historic", "Once", true, 4500, 100, 600));
            var emptyPreviousQueue = ChannelSettlementFeedbackRules.Pending(historic);
            Assert.IsEmpty(emptyPreviousQueue);
            Assert.AreEqual(0, ChannelSettlementFeedbackRules.NewlySettled(
                historic, emptyPreviousQueue, 100).Count);
            Assert.AreEqual(0, ChannelSettlementFeedbackRules.NewlySettled(
                historic, new HashSet<string>(), 100).Count);
        }

        [Test]
        public void FailedDayCloseDoesNotCelebrateAndSuccessfulRetryDoesSoOnce()
        {
            var tracker = new ChannelSettlementFeedbackTracker();
            var queued = new MediaSaveData { Followers = 20 };
            queued.Publications.Add(Publication("retry", "Kayit tekrar", false));

            var settledInMemory = new MediaSaveData { Followers = 26 };
            settledInMemory.Publications.Add(Publication("retry", "Kayit tekrar", true, 100, 6, 50));

            Assert.AreEqual(0, tracker.Observe(queued, 1, 1, 0).Count);
            // ChannelAuthority.SettleThrough already changed the mirror, but SaveNow failed.
            Assert.AreEqual(0, tracker.Observe(settledInMemory, 2, 1, 0).Count);
            Assert.AreEqual(0, tracker.Observe(settledInMemory, 2, 1, 0).Count,
                "an unchanged speculative revision must not be shown as saved");

            // A successful RetryClose advances the host day and publishes the durable summary,
            // without necessarily changing the already mirrored media revision.
            var card = tracker.Observe(settledInMemory, 2, 2, 1);
            Assert.AreEqual(1, card.Count);
            Assert.AreEqual(20, card.FollowersBefore);
            Assert.AreEqual(26, card.FollowersAfter);
            Assert.AreEqual(50, card.Income);
            Assert.AreEqual(0, tracker.Observe(settledInMemory, 2, 2, 1).Count);
            Assert.AreEqual(0, tracker.Observe(settledInMemory, 3, 2, 1).Count,
                "replayed media revisions cannot celebrate twice");
        }

        [Test]
        public void DurableDayCanArriveBeforeSettledMediaWithoutLosingTheEvent()
        {
            var tracker = new ChannelSettlementFeedbackTracker();
            var queued = new MediaSaveData { Followers = 4 };
            queued.Publications.Add(Publication("late-mirror", "Gecikmis ayna", false));
            var settled = new MediaSaveData { Followers = 9 };
            settled.Publications.Add(Publication("late-mirror", "Gecikmis ayna", true, 50, 5, 8));

            Assert.AreEqual(0, tracker.Observe(queued, 10, 1, 0).Count);
            Assert.AreEqual(0, tracker.Observe(queued, 10, 2, 1).Count,
                "acknowledgement without a matching settlement has nothing to celebrate");
            Assert.AreEqual(1, tracker.Observe(settled, 11, 2, 1).Count);
            Assert.AreEqual(0, tracker.Observe(settled, 11, 2, 1).Count);
        }

        [Test]
        public void OlderSummaryCannotConfirmCurrentDayOrReplayHistory()
        {
            var tracker = new ChannelSettlementFeedbackTracker();
            var queued = new MediaSaveData { Followers = 10 };
            var pending = Publication("today", "Bugun", false);
            pending.QueuedDay = 2;
            queued.Publications.Add(pending);
            var settled = new MediaSaveData { Followers = 13 };
            var settledPublication = Publication("today", "Bugun", true, 20, 3, 6);
            settledPublication.QueuedDay = 2;
            settled.Publications.Add(settledPublication);

            Assert.AreEqual(0, tracker.Observe(queued, 1, 2, 1).Count);
            Assert.AreEqual(0, tracker.Observe(settled, 2, 2, 1).Count,
                "the previous day's durable summary cannot certify a new settlement");
            Assert.AreEqual(1, tracker.Observe(settled, 2, 3, 2).Count);

            tracker.Reset(); // disconnect and join on the persisted campaign
            Assert.AreEqual(0, tracker.Observe(settled, 2, 3, 2).Count);
            Assert.AreEqual(0, tracker.Observe(settled, 3, 3, 2).Count);
        }

        [Test]
        public void MultipleSettlementsAreGroupedWithoutDoubleCountingPendingItems()
        {
            var queued = new MediaSaveData { Followers = 10 };
            queued.Publications.Add(Publication("one", "A", false));
            queued.Publications.Add(Publication("two", "B", false));
            var previous = ChannelSettlementFeedbackRules.Pending(queued);

            var next = new MediaSaveData { Followers = 35 };
            next.Publications.Add(Publication("one", "A", true, 60, 10, 4));
            next.Publications.Add(Publication("two", "B", true, 90, 15, 6));
            next.Publications.Add(Publication("historic", "Old", true, 999, 100, 500));
            next.Publications.Add(Publication("three", "Queued", false));

            var card = ChannelSettlementFeedbackRules.NewlySettled(next, previous, 10);
            Assert.AreEqual(2, card.Count);
            Assert.AreEqual(150, card.Views);
            Assert.AreEqual(25, card.FollowersGained);
            Assert.AreEqual(10, card.Income);
            Assert.AreEqual(10, card.FollowersBefore);
            Assert.AreEqual(35, card.FollowersAfter);
            Assert.AreEqual(1, ChannelSettlementFeedbackRules.Pending(next).Count);

            next.Publications[0].SettledId = "invalid";
            Assert.AreEqual(1, ChannelSettlementFeedbackRules.NewlySettled(next, previous, 10).Count,
                "only host settlement ids can trigger the result card");
        }
    }
}
