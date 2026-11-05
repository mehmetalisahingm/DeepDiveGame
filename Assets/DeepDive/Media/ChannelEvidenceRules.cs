using System;

namespace DeepDive.Media
{
    // #255 proposal, NOT wired into the payout yet (nothing calls it outside tests): how host-verified catch evidence could scale a publication's views. Pure and
    // deterministic. Every number is a working value for playtests. It needs two inputs that do not exist on the clip yet and that
    // belong to others (#254 Utku: the verified catch's weight and id; #253 Mehmet: the real clip), so nothing here changes a result
    // until the shared contract is agreed. See docs/plan/CHANNEL_RULES_255_PROPOSAL.md.
    public enum ChannelEvidence
    {
        // A valid recording of a subject with no verified catch of the same target: today's behaviour (100%).
        RecordingOnly = 0,
        // The same player's verified catch of the recorded target, matched once (the match is consumed by the first publication).
        MatchedCatch = 1,
        // A catch without a published recording earns no channel views; it is sold at the buyer as before.
        CatchOnly = 2
    }

    public static class ChannelEvidenceRules
    {
        public const int MaxSizeBonusPercent = 50;
        public const int MatchedCatchBonusPercent = 25;

        // 100..150: where the verified weight sits in the species' own range. 0 (unverified) is neutral, never a penalty or a bonus:
        // a clip looking big is not a weight.
        public static int SizePercent(int verifiedWeightGrams, int speciesMinGrams, int speciesMaxGrams)
        {
            if (verifiedWeightGrams <= 0 || speciesMaxGrams <= speciesMinGrams) return 100;
            var t = Math.Max(0.0, Math.Min(1.0, (verifiedWeightGrams - speciesMinGrams) / (double)(speciesMaxGrams - speciesMinGrams)));
            return 100 + (int)Math.Round(MaxSizeBonusPercent * t);
        }

        // Views multiplier in percent for one publication. CatchOnly has no publication, so 0.
        public static int ViewsPercent(ChannelEvidence evidence, int verifiedWeightGrams, int speciesMinGrams, int speciesMaxGrams)
        {
            switch (evidence)
            {
                case ChannelEvidence.CatchOnly: return 0;
                case ChannelEvidence.MatchedCatch:
                    return SizePercent(verifiedWeightGrams, speciesMinGrams, speciesMaxGrams) * (100 + MatchedCatchBonusPercent) / 100;
                default: return 100;
            }
        }

        // The payout takes this percent as ChannelResultRules.Evaluate's viewsPercent (it scales the clip's own base views only;
        // followers keep feeding views the same way). 100 = no evidence bonus = the rule without evidence.
    }
}
