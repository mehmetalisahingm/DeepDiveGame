using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #220: after a cosmetic view disable, the replicated boat seat can still
    // be assigned without emitting another NetworkVariable change callback.
    public sealed class P49BoatSeatReactivationTests
    {
        [Test]
        public void ExistingSeatRestoresCameraVisualAndUnseatedStateClearsIt()
        {
            var root = new GameObject("P49 seat visual reactivation");
            try
            {
                var player = root.AddComponent<NetworkPlayer>();
                Assert.IsFalse(player.VisualBoatSeated);
                PlayerActionView.RestoreBoatSeatPresentation(player, true);
                Assert.IsTrue(player.VisualBoatSeated, "re-enabled view must recover existing boat seat");
                PlayerActionView.RestoreBoatSeatPresentation(player, true);
                Assert.IsTrue(player.VisualBoatSeated, "recovery is idempotent, not a new board");
                PlayerActionView.RestoreBoatSeatPresentation(player, false);
                Assert.IsFalse(player.VisualBoatSeated, "bed / non-boat state uses standing camera offset");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingNetworkPlayerIsHarmlessDuringEarlyEnable()
        {
            Assert.DoesNotThrow(() => PlayerActionView.RestoreBoatSeatPresentation(null, true));
        }
    }
}
