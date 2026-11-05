using DeepDive.Media;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    // #255: the 10,000,000-follower goal model, the size/match evidence rule (proposal, unwired) and a characterisation of what the CURRENT
    // payout rules can reach. The ceiling test documents a finding, not a balance: change the rules and it is the test to revisit.
    public sealed class ChannelGoalTests
    {
        [Test]
        public void ProgressNamesTheNextMilestoneAndReachesTheGoalWithoutEndingAnything()
        {
            Assert.AreEqual(0, ChannelGoal.Of(0).MilestonesReached);
            Assert.AreEqual(100, ChannelGoal.Of(0).NextMilestone);
            Assert.AreEqual(0, ChannelGoal.Of(-5).Followers, "never negative");
            Assert.AreEqual(0, ChannelGoal.Of(99).MilestonesReached);
            Assert.AreEqual(1, ChannelGoal.Of(100).MilestonesReached);
            Assert.AreEqual(1000, ChannelGoal.Of(100).NextMilestone);
            Assert.AreEqual(5, ChannelGoal.Of(9999999).MilestonesReached);
            Assert.AreEqual(ChannelGoal.TargetFollowers, ChannelGoal.Of(9999999).NextMilestone);
            Assert.IsFalse(ChannelGoal.Of(9999999).Reached);

            var done = ChannelGoal.Of(ChannelGoal.TargetFollowers);
            Assert.IsTrue(done.Reached);
            Assert.AreEqual(6, done.MilestonesReached);
            Assert.AreEqual(0, done.NextMilestone);
            Assert.AreEqual(1f, done.Ratio);
            Assert.IsTrue(ChannelGoal.Of(ChannelGoal.TargetFollowers + 12345).Reached, "the channel keeps growing past the goal");
            Assert.AreEqual(1f, ChannelGoal.Of(int.MaxValue).Ratio);
        }

        [Test]
        public void TheGoalLineIsReadableAndHonestAboutScale()
        {
            Assert.AreEqual("KANAL  hedef 10.000.000: %0,0   sonraki: 100", ChannelGoal.Describe(0));
            Assert.AreEqual("KANAL  hedef 10.000.000: %0,3   sonraki: 100.000", ChannelGoal.Describe(28900));
            StringAssert.Contains("HEDEF TAMAM", ChannelGoal.Describe(ChannelGoal.TargetFollowers));
            Assert.AreEqual("1.234.567", ChannelGoal.Format(1234567));
        }

        [Test]
        public void SizeBonusFollowsTheVerifiedWeightInsideTheSpeciesRangeAndUnverifiedIsNeutral()
        {
            Assert.AreEqual(100, ChannelEvidenceRules.SizePercent(0, 2000, 6000), "no verified weight: no bonus, no penalty");
            Assert.AreEqual(100, ChannelEvidenceRules.SizePercent(2000, 2000, 6000));
            Assert.AreEqual(125, ChannelEvidenceRules.SizePercent(4000, 2000, 6000));
            Assert.AreEqual(150, ChannelEvidenceRules.SizePercent(6000, 2000, 6000));
            Assert.AreEqual(150, ChannelEvidenceRules.SizePercent(99999, 2000, 6000), "outside the species range clamps, it cannot inflate");
            Assert.AreEqual(100, ChannelEvidenceRules.SizePercent(500, 2000, 6000), "below the range clamps to the base");
            Assert.AreEqual(100, ChannelEvidenceRules.SizePercent(3000, 3000, 3000), "an invalid range is neutral");
        }

        [Test]
        public void OnlyAMatchedCatchRaisesViewsAndACatchWithoutARecordingHasNoChannelEffect()
        {
            Assert.AreEqual(100, ChannelEvidenceRules.ViewsPercent(ChannelEvidence.RecordingOnly, 6000, 2000, 6000), "a recording alone is today's rule");
            Assert.AreEqual(125, ChannelEvidenceRules.ViewsPercent(ChannelEvidence.MatchedCatch, 0, 2000, 6000), "matched, weight unverified: only the match bonus");
            Assert.AreEqual(187, ChannelEvidenceRules.ViewsPercent(ChannelEvidence.MatchedCatch, 6000, 2000, 6000), "150% size * 125% match");
            Assert.AreEqual(0, ChannelEvidenceRules.ViewsPercent(ChannelEvidence.CatchOnly, 6000, 2000, 6000));
        }

        [Test]
        public void TheEvidencePercentOnlyScalesTheClipsOwnViewsAndNoBonusIsTheSameAsTheThreeArgumentRule()
        {
            foreach (var quality in new[] { 0, 1, 2, 3, 4 })
            foreach (var novel in new[] { false, true })
            foreach (var followers in new[] { 0, 1, 99, 5000, 1000000 })
            {
                ChannelResultRules.Evaluate(quality, novel, followers, out var v, out var f, out var i);
                ChannelResultRules.Evaluate(quality, novel, followers, 100, out var v2, out var f2, out var i2);
                Assert.AreEqual((v, f, i), (v2, f2, i2), $"q{quality} novel={novel} followers={followers}");
            }
            ChannelResultRules.Evaluate(4, false, 0, 187, out var big, out _, out _);
            ChannelResultRules.Evaluate(4, false, 0, 100, out var plain, out _, out _);
            Assert.AreEqual(640, plain);
            Assert.AreEqual(1196, big, "187% of the 640 base views");
            ChannelResultRules.Evaluate(4, false, 100000, 187, out var withFollowers, out _, out _);
            Assert.AreEqual(1196 + 120000, withFollowers, "followers feed views the same way, the evidence scales only the clip's own views");
        }

        [Test]
        public void ANewChannelStartsWhereTheOldRulesDid()
        {
            // With no followers the payout is unchanged by #255: the first clips of a campaign earn what they always did.
            ChannelResultRules.Evaluate(3, true, 0, out var views, out var followers, out var income);
            Assert.AreEqual(540, views);
            Assert.AreEqual(27, followers);
            Assert.AreEqual(135, income);
        }

        [Test]
        public void IncomeStaysOnThePriceScaleEvenForAHugeChannel()
        {
            ChannelResultRules.Evaluate(4, true, ChannelGoal.TargetFollowers, out var views, out _, out var income);
            Assert.Greater(views, 10000000, "a ten million channel draws millions of views");
            Assert.LessOrEqual(income, (960 + ChannelResultRules.IncomeFeedbackCapViews) / 4, "but a post pays at most a clip's own views plus the capped feedback");
            Assert.Less(income, 1000, "well under the dearest purchases (a few thousand credits)");
        }

        // #255: the goal must be reachable. Pure rule simulation (no archive involved): how many publications of one quality it takes, and that
        // it is neither instant nor out of reach. A working-value characterisation, not a time promise: days depend on real play.
        [Test]
        public void TheGoalIsReachableButNotInstant()
        {
            int Publications(int quality)
            {
                var followers = 0;
                for (var n = 1; n <= 5000; n++)
                {
                    ChannelResultRules.Evaluate(quality, n <= 5, followers, out _, out var gained, out _);
                    followers += gained;
                    if (followers >= ChannelGoal.TargetFollowers) return n;
                }
                return 0;
            }

            var platinum = Publications(4);
            var silver = Publications(2);
            TestContext.Out.WriteLine($"P411_CHANNEL_REACH platinum={platinum} silver={silver} feedback={ChannelResultRules.FeedbackPercent}");
            Assert.Greater(platinum, 60, "not instant");
            Assert.Greater(silver, platinum, "a lower quality takes longer");
            Assert.Less(silver, 500, "still reachable with ordinary clips");
        }
    }
}
