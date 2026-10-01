using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.Tests.P4
{
    public sealed class HomeInteractionContractTests
    {
        [TearDown]
        public void TearDown()
        {
            HomeStorageInteraction.Unbind(OpenStorage);
        }

        [Test]
        public void P4Home_HasExactlyFourStableBedIds()
        {
            Assert.That(DayIds.BedCount, Is.EqualTo(4));
            Assert.That(DayIds.Beds.Count, Is.EqualTo(4));
            for (var i = 0; i < DayIds.Beds.Count; i++)
            {
                Assert.That(DayIds.IsBed(DayIds.Beds[i]), Is.True);
                for (var j = i + 1; j < DayIds.Beds.Count; j++)
                    Assert.That(DayIds.Beds[i], Is.Not.EqualTo(DayIds.Beds[j]));
            }
        }

        [Test]
        public void StoragePhysicalSeam_IsClosedWhenNoAuthorityIsBound()
        {
            HomeStorageInteraction.Unbind(OpenStorage);
            var result = HomeStorageInteraction.TryOpen(new PlayerId(7), 12);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.ReasonCode, Is.EqualTo("StorageUnavailable"));
        }

        [Test]
        public void StoragePhysicalSeam_ForwardsPlayerAndRequestWithoutOwningStorage()
        {
            HomeStorageInteraction.Bind(OpenStorage);
            var result = HomeStorageInteraction.TryOpen(new PlayerId(3), 44);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RequestId, Is.EqualTo(44));
            Assert.That(result.Revision, Is.EqualTo(3));
        }

        private static TransactionResult OpenStorage(PlayerId player, ulong requestId) =>
            TransactionResult.Ok(requestId, (int)player.Value);
    }
}
