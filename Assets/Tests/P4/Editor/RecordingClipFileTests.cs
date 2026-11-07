using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Media;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class RecordingClipFileTests
    {
        private static ClipFramePacket Frame(float time, byte seed) =>
            new ClipFramePacket(time, new[] { seed, (byte)(seed + 1), (byte)(seed + 2), (byte)(seed + 3) });

        [Test]
        public void RecordingIdProducesOneStableSafeClipId()
        {
            const string recording = "0123456789abcdef0123456789abcdef";
            Assert.AreEqual("clip-" + recording, RecordingClipFile.ClipIdForRecording(recording));
            Assert.AreEqual("clip-" + recording, RecordingClipFile.ClipIdForRecording("  " + recording + "  "));
            Assert.AreEqual(string.Empty, RecordingClipFile.ClipIdForRecording("../escape"));
            Assert.AreEqual(string.Empty, RecordingClipFile.ClipIdForRecording("a/b"));
            Assert.AreEqual(string.Empty, RecordingClipFile.ClipIdForRecording(""));
        }

        [Test]
        public void ClipContainerRoundTripsFramesAndRejectsTamperedStructure()
        {
            var frames = new List<ClipFramePacket> { Frame(0f, 10), Frame(0.2f, 20), Frame(0.4f, 30) };
            var bytes = RecordingClipFile.Encode(320, 180, 0.6f, frames);
            Assert.IsTrue(RecordingClipFile.TryDecode(bytes, out var decoded));
            Assert.AreEqual(320, decoded.Width);
            Assert.AreEqual(180, decoded.Height);
            Assert.AreEqual(0.6f, decoded.DurationSeconds, 0.0001f);
            Assert.AreEqual(3, decoded.Frames.Count);
            CollectionAssert.AreEqual(frames[1].JpegBytes, decoded.Frames[1].JpegBytes);

            var truncated = new byte[bytes.Length - 1];
            Buffer.BlockCopy(bytes, 0, truncated, 0, truncated.Length);
            Assert.IsFalse(RecordingClipFile.TryDecode(truncated, out _));
        }

        [Test]
        public void HashIsStableAndChangesWithPayload()
        {
            var first = RecordingClipFile.Encode(320, 180, 1f, new[] { Frame(0f, 1) });
            var second = RecordingClipFile.Encode(320, 180, 1f, new[] { Frame(0f, 2) });
            Assert.AreEqual(64, RecordingClipFile.Sha256Hex(first).Length);
            Assert.AreEqual(RecordingClipFile.Sha256Hex(first), RecordingClipFile.Sha256Hex(first));
            Assert.AreNotEqual(RecordingClipFile.Sha256Hex(first), RecordingClipFile.Sha256Hex(second));
        }

        [Test]
        public void ManifestComesOnlyFromARealPaidSafeRecordingAndKeepsHostOwnership()
        {
            var player = new PlayerId(7);
            var result = new RecordingResult("rec_123", "dive-9", player, "sea_bass", 3, 7.5f);
            Assert.IsTrue(RecordingClipFile.TryBuildManifest(result, 4, 9f, new string('a', 64), 4096, out var manifest));
            Assert.AreEqual("clip-rec_123", manifest.ClipId);
            Assert.AreEqual(result.RecordingId, manifest.RecordingId);
            Assert.AreEqual(player, manifest.Owner);
            Assert.AreEqual("sea_bass", manifest.SubjectId);
            Assert.AreEqual(3, manifest.Quality);
            Assert.AreEqual(9f, manifest.DurationSeconds);
            Assert.IsTrue(manifest.MediaReady);
            Assert.IsTrue(manifest.SafeReturned);
            Assert.IsTrue(manifest.IsCommercialCandidate);

            Assert.IsFalse(RecordingClipFile.TryBuildManifest(
                new RecordingResult("", "dive-9", player, "sea_bass", 3, 7.5f), 4, 9f, "hash", 10, out _));
            Assert.IsFalse(RecordingClipFile.TryBuildManifest(
                new RecordingResult("rec", "dive-9", player, "sea_bass", 0, 7.5f), 4, 9f, "hash", 10, out _));
            Assert.IsFalse(RecordingClipFile.TryBuildManifest(result, 4, 0f, "hash", 10, out _));
        }

        [Test]
        public void DecodeRejectsTamperedFrameAfterClaimedDuration()
        {
            var bytes = RecordingClipFile.Encode(320, 180, 1f,
                new[] { Frame(.2f, 10), Frame(.8f, 20) });
            Assert.IsTrue(RecordingClipFile.TryDecode(bytes, out _),
                "untampered clip is accepted");

            // Header: magic, version, width, height, duration, frame count
            // (24 bytes), then first float timestamp. Keep the payload intact.
            var outOfDuration = (byte[])bytes.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(1.5f), 0, outOfDuration, 24, 4);
            Assert.IsFalse(RecordingClipFile.TryDecode(outOfDuration, out _),
                "decoder must not play a frame beyond the advertised duration");

            var validBoundary = RecordingClipFile.Encode(320, 180, 1f,
                new[] { Frame(.2f, 10), Frame(1f, 20) });
            Assert.IsTrue(RecordingClipFile.TryDecode(validBoundary, out _),
                "last frame at the clip boundary remains valid");
        }

        [Test]
        public void EncodeRefusesEmptyNonMonotonicOrOutOfDurationInput()
        {
            Assert.Throws<ArgumentException>(() => RecordingClipFile.Encode(320, 180, 1f, Array.Empty<ClipFramePacket>()));
            Assert.Throws<ArgumentException>(() => RecordingClipFile.Encode(
                320, 180, 1f, new[] { Frame(0.5f, 1), Frame(0.1f, 2) }),
                "encoder must not write a file the decoder would reject");
            Assert.Throws<ArgumentException>(() => RecordingClipFile.Encode(
                320, 180, .3f, new[] { Frame(0f, 1), Frame(.5f, 2) }),
                "a captured frame cannot claim time beyond the clip's own playable duration");
            Assert.Throws<ArgumentException>(() => RecordingClipFile.Encode(
                320, 180, 1f, new[] { Frame(float.NaN, 1) }));
            var valid = RecordingClipFile.Encode(320, 180, .4f,
                new[] { Frame(0f, 1), Frame(.2f, 2), Frame(.4f, 3) });
            Assert.IsTrue(RecordingClipFile.TryDecode(valid, out var decoded));
            Assert.AreEqual(.4f, decoded.Frames[decoded.Frames.Count - 1].TimeSeconds, .0001f);
        }
    }
}
