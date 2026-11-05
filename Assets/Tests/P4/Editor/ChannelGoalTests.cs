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
        public void TheEvidenceRuleWithNoBonusIsExactlyTheCurrentPayout()
        {
            foreach (var quality in new[] { 0, 1, 2, 3, 4 })
            foreach (var novel in new[] { false, true })
            foreach (var followers in new[] { 0, 1, 99, 5000, 1000000 })
            {
                ChannelResultRules.Evaluate(quality, novel, followers, out var v, out var f, out var i);
                ChannelEvidenceRules.Evaluate(quality, novel, followers, 100, out var v2, out var f2, out var i2);
                Assert.AreEqual((v, f, i), (v2, f2, i2), $"q{quality} novel={novel} followers={followers}");
            }
            ChannelEvidenceRules.Evaluate(4, false, 0, 187, out var big, out _, out _);
            ChannelResultRules.Evaluate(4, false, 0, out var plain, out _, out _);
            Assert.Greater(big, plain);
        }

        // Finding for #255: under the CURRENT rules (views = 40*q^2 (x1.5 for a new subject) + followers/2, followers += views/20) and the
        // archive's lifetime cap of ChannelAuthority.MaxArchivedClips clips (a full archive refuses new clips), even 128 platinum clips
        // reach a tiny fraction of 10,000,000. This characterises the present behaviour; it is not a balance claim.
        [Test]
        public void CurrentRulesCannotReachTheGoalWithinTheArchiveCap()
        {
            int Simulate(int publications, bool firstOfEachFiveNovel, out int reachedAt)
            {
                var followers = 0;
                reachedAt = 0;
                for (var n = 1; n <= publications; n++)
                {
                    ChannelResultRules.Evaluate(4, firstOfEachFiveNovel && n <= 5, followers, out _, out var gained, out _);
                    followers += gained;
                    if (followers >= ChannelGoal.TargetFollowers) { reachedAt = n; break; }
                }
                return followers;
            }

            var ceiling = Simulate(ChannelAuthority.MaxArchivedClips, true, out var reachedWithinCap);
            TestContext.Out.WriteLine($"P411_CHANNEL_CEILING publications={ChannelAuthority.MaxArchivedClips} followers={ceiling} percentOfGoal={ceiling * 100.0 / ChannelGoal.TargetFollowers:0.00}");
            Assert.AreEqual(0, reachedWithinCap, "the goal is not reachable inside the archive cap with all-platinum clips and every subject new");
            Assert.Less(ceiling, ChannelGoal.TargetFollowers / 100, "under 1% of the goal");

            Simulate(100000, true, out var uncappedReachedAt);
            TestContext.Out.WriteLine($"P411_CHANNEL_UNCAPPED_REACHED_AT publications={uncappedReachedAt}");
            Assert.Greater(uncappedReachedAt, ChannelAuthority.MaxArchivedClips, "without the cap it would take more publications than the archive can ever hold");
        }
    }
}
