using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #278 (Mert): the success card after a real turn-in. Rules only: it shows what the host already decided and invents nothing.
    public sealed class P278SuccessFeedbackTests
    {
        [Test]
        public void AcceptedFishSaleShowsBeforeAfterAndDifference()
        {
            Assert.IsTrue(SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 240, 3, 360, 0, false, out var card));
            Assert.AreEqual(120, card.Before);
            Assert.AreEqual(360, card.After);
            Assert.AreEqual(240, card.Delta);
            StringAssert.Contains("AV SATILDI", card.Title);
            StringAssert.Contains("3 av", card.Detail);
            Assert.IsFalse(card.First);
        }

        [Test]
        public void AcceptedRecordingTurnInIsASuccessToo()
        {
            Assert.IsTrue(SuccessFeedbackRules.TryBuild(ServicePointType.RecordingBuyer, true, 90, 1, 500, 1, false, out var card));
            StringAssert.Contains("KAYIT TESLIM", card.Title);
            StringAssert.Contains("1 kayit", card.Detail);
            Assert.AreEqual(410, card.Before);
        }

        [Test]
        public void TheFirstSuccessIsMarkedAndNamedAsSuch()
        {
            Assert.IsTrue(SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 60, 1, 180, 0, true, out var card));
            Assert.IsTrue(card.First);
            StringAssert.StartsWith("ILK BASARI!", card.Title);
        }

        [Test]
        public void RefusalsZeroCreditsAndOtherServicesAreNotSuccesses()
        {
            Assert.IsFalse(SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, false, 240, 3, 360, 0, true, out _), "refused");
            Assert.IsFalse(SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 0, 3, 360, 0, true, out _), "no credit");
            Assert.IsFalse(SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 240, 0, 360, 0, true, out _), "nothing handed in");
            Assert.IsFalse(SuccessFeedbackRules.TryBuild(ServicePointType.EquipmentShop, true, 240, 3, 360, 0, true, out _), "a purchase is not a success card");
        }

        [Test]
        public void NextGoalFollowsTheCameraLadderAndSaysWhatIsMissing()
        {
            StringAssert.Contains("Temel kamera (150)", SuccessFeedbackRules.NextGoal(100, 0));
            StringAssert.Contains("50 kredi eksik", SuccessFeedbackRules.NextGoal(100, 0));
            StringAssert.Contains("dukkana git", SuccessFeedbackRules.NextGoal(150, 0));
            StringAssert.Contains("Gelismis kamera (400)", SuccessFeedbackRules.NextGoal(200, 1));
            StringAssert.Contains("Profesyonel kamera (900)", SuccessFeedbackRules.NextGoal(200, 2));
            StringAssert.Contains("sandali onar", SuccessFeedbackRules.NextGoal(5000, 3));
        }

        [Test]
        public void ChimeIsAudibleAndTheFirstOneIsLonger()
        {
            var normal = SuccessFeedbackRules.CreateChime(false);
            var first = SuccessFeedbackRules.CreateChime(true);
            try
            {
                Assert.Greater(normal.length, 0.3f);
                Assert.Greater(first.length, normal.length);
                var data = new float[normal.samples];
                Assert.IsTrue(normal.GetData(data, 0));
                var peak = data.Max(Mathf.Abs);
                Assert.Greater(peak, 0.1f, "audible");
                Assert.Less(peak, 1f, "no clipping");
                Assert.Less(Mathf.Abs(data[data.Length - 1]), 0.01f, "ends quiet");
            }
            finally { Object.DestroyImmediate(normal); Object.DestroyImmediate(first); }
        }

        [Test]
        public void TheViewShowsACardForAFewSecondsAndTheFirstOneLonger()
        {
            var go = new GameObject("SuccessFeedbackTest");
            try
            {
                var view = go.AddComponent<SuccessFeedbackView>();
                Assert.IsFalse(view.Visible);
                SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 60, 1, 180, 0, true, out var first);
                view.Show(first);
                Assert.IsTrue(view.Visible);
                Assert.AreEqual(180, view.Card.After);
                Assert.Greater(SuccessFeedbackView.FirstShowSeconds, SuccessFeedbackView.ShowSeconds);
            }
            finally { Object.DestroyImmediate(go); SuccessFeedbackView.ResetFirstForTests(); }
        }
    }
}
