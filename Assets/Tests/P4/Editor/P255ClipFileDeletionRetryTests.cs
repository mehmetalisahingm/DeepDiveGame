using System;
using System.IO;
using DeepDive.Composition;
using DeepDive.Media;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class P255ClipFileDeletionRetryTests
    {
        [Test]
        public void InvalidClipPathDoesNotPretendThatDeletionSucceeded()
        {
            var pending = new PendingMediaDeletes();
            pending.Add("invalid/clip-id");
            Assert.AreEqual(0, pending.Flush(RecordingMediaCaptureBinding.DeleteClipFile));
            Assert.AreEqual(1, pending.Count, "the failure must be retained for a later successful save");
        }

        [Test]
        public void ValidEvictedMediaFileIsRemovedOnlyWhenFlushed()
        {
            var clipId = "p255-delete-" + Guid.NewGuid().ToString("N");
            var path = Path.Combine(Application.persistentDataPath, "DeepDiveClips", clipId + ".ddclip");
            var pending = new PendingMediaDeletes();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
                pending.Add(clipId);

                Assert.IsTrue(File.Exists(path), "queued eviction must not delete before a successful save");
                Assert.AreEqual(1, pending.Flush(RecordingMediaCaptureBinding.DeleteClipFile));
                Assert.IsFalse(File.Exists(path));
                Assert.AreEqual(0, pending.Count);
                Assert.AreEqual(0, pending.Flush(RecordingMediaCaptureBinding.DeleteClipFile));
                Assert.DoesNotThrow(() => RecordingMediaCaptureBinding.DeleteClipFile(clipId),
                    "deletion is idempotent if a clip has already gone");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}
