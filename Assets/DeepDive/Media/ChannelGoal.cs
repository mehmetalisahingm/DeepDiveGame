using System;
using System.Globalization;

namespace DeepDive.Media
{
    // #255: the shared channel's goal, 10,000,000 followers (docs/plan/CHANNEL_GOAL_10M.md). Pure: followers in, progress out. The host's
    // follower count is the only input, so a mirror shows the same progress as the host; reaching the goal is an achievement state and
    // never ends the game. The milestone numbers are working values, not a balance or a time promise.
    public static class ChannelGoal
    {
        public const int TargetFollowers = 10000000;
        public static readonly int[] Milestones = { 100, 1000, 10000, 100000, 1000000, TargetFollowers };

        public readonly struct Progress
        {
            public readonly int Followers;
            public readonly int MilestonesReached;
            public readonly int NextMilestone;   // 0 once the goal is reached
            public readonly bool Reached;

            public Progress(int followers, int milestonesReached, int nextMilestone, bool reached)
            {
                Followers = followers;
                MilestonesReached = milestonesReached;
                NextMilestone = nextMilestone;
                Reached = reached;
            }

            // 0..1 of the final goal (linear, so early progress looks small: that is the honest scale of 10 million).
            public float Ratio => Math.Max(0f, Math.Min(1f, Followers / (float)TargetFollowers));
        }

        public static Progress Of(int followers)
        {
            var f = Math.Max(0, followers);
            var reached = 0;
            while (reached < Milestones.Length && f >= Milestones[reached]) reached++;
            var next = reached < Milestones.Length ? Milestones[reached] : 0;
            return new Progress(f, reached, next, f >= TargetFollowers);
        }

        // One line for the PC panel (ASCII like the rest of that IMGUI view).
        public static string Describe(int followers)
        {
            var p = Of(followers);
            if (p.Reached) return "KANAL  HEDEF TAMAM: " + Format(TargetFollowers) + " takipci (kanal buyumeye devam eder)";
            var percent = (p.Followers * 100.0 / TargetFollowers).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return "KANAL  hedef " + Format(TargetFollowers) + ": %" + percent + "   sonraki: " + Format(p.NextMilestone);
        }

        public static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
    }
}
