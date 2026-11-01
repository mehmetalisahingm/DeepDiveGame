using System.Reflection;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #220: visual feedback is local and must not survive NGO respawn/rehost.
    // Never reset request sequence or host-side economic authority here.
    public sealed class PlayerFeedbackSessionResetTests
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static FieldInfo Field(string name)
        {
            var field = typeof(NetworkPlayer).GetField(name, Flags);
            Assert.NotNull(field, name);
            return field;
        }

        [Test]
        public void RehostClearsOldSuccessHudWithoutRewindingActionRequestSequence()
        {
            var go = new GameObject("p49-feedback-rehost");
            try
            {
                var player = go.AddComponent<NetworkPlayer>();
                Field("observedActionRequestId").SetValue(player, (ulong)41);
                Field("actionSequence").SetValue(player, (ulong)79);
                Field("actionMessage").SetValue(player, "Previous room pickup success");
                Field("actionCue").SetValue(player, PlayerFeedbackCue.PickupAccepted);
                Field("actionMessageUntil").SetValue(player, 300f);
                Field("shotFeedbackUntil").SetValue(player, 300f);

                var reset = typeof(NetworkPlayer).GetMethod("ResetTransientActionFeedback", Flags);
                Assert.NotNull(reset);
                reset.Invoke(player, null);
                // Repeated room-boundary callbacks must be harmless.
                reset.Invoke(player, null);

                Assert.AreEqual((ulong)0, Field("observedActionRequestId").GetValue(player));
                Assert.AreEqual("", Field("actionMessage").GetValue(player));
                Assert.AreEqual(default(PlayerFeedbackCue), Field("actionCue").GetValue(player));
                Assert.AreEqual(0f, Field("actionMessageUntil").GetValue(player));
                Assert.AreEqual(0f, Field("shotFeedbackUntil").GetValue(player));
                Assert.AreEqual((ulong)79, Field("actionSequence").GetValue(player),
                    "request/anti-replay authority is not cosmetic state");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
