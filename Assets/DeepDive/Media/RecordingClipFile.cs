using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DeepDive.Core.Contracts;

namespace DeepDive.Media
{
    // A compact P4.2 prototype container: timestamped JPEG frames captured from the host-authoritative
    // recorder view. The file is intentionally engine-agnostic so integrity/idempotence can be tested
    // without Unity graphics. It is not a public media format; it is the game's local clip payload.
    public readonly struct ClipFramePacket
    {
        public readonly float TimeSeconds;
        public readonly byte[] JpegBytes;

        public ClipFramePacket(float timeSeconds, byte[] jpegBytes)
        {
            TimeSeconds = timeSeconds;
            JpegBytes = jpegBytes ?? Array.Empty<byte>();
        }
    }

    public sealed class ClipFileData
    {
        public int Width { get; }
        public int Height { get; }
        public float DurationSeconds { get; }
        public IReadOnlyList<ClipFramePacket> Frames { get; }

        public ClipFileData(int width, int height, float durationSeconds, IReadOnlyList<ClipFramePacket> frames)
        {
            Width = width;
            Height = height;
            DurationSeconds = durationSeconds;
            Frames = frames ?? Array.Empty<ClipFramePacket>();
        }
    }

    public static class RecordingClipFile
    {
        private const int Magic = 0x31434444; // "DDC1" little-endian
        private const int Version = 1;
        private const int MaxFrames = 600;
        private const int MaxFrameBytes = 2 * 1024 * 1024;
        private const int MaxFileBytes = 256 * 1024 * 1024;

        public static string ClipIdForRecording(string recordingId)
        {
            if (string.IsNullOrWhiteSpace(recordingId)) return string.Empty;
            recordingId = recordingId.Trim();
            if (recordingId.Length > 64) return string.Empty;
            for (var i = 0; i < recordingId.Length; i++)
            {
                var c = recordingId[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return string.Empty;
            }
            return "clip-" + recordingId;
        }

        public static bool TryBuildManifest(RecordingResult result, int dayNumber, float mediaDurationSeconds,
            string contentHash, long sizeBytes, out ClipManifest manifest)
        {
            manifest = default;
            var clipId = ClipIdForRecording(result.RecordingId);
            if (string.IsNullOrEmpty(clipId) || string.IsNullOrWhiteSpace(result.DiveId) ||
                string.IsNullOrWhiteSpace(result.SubjectId) || result.Quality < 1 || result.Quality > 4 ||
                result.ValidDurationSeconds <= 0f || dayNumber < 1 || mediaDurationSeconds <= 0f ||
                string.IsNullOrWhiteSpace(contentHash) || sizeBytes <= 0)
                return false;

            manifest = new ClipManifest(clipId, result.RecordingId, result.DiveId, dayNumber, result.PlayerId,
                result.SubjectId, result.Quality, mediaDurationSeconds, contentHash, sizeBytes,
                mediaReady: true, safeReturned: true);
            return true;
        }

        public static byte[] Encode(int width, int height, float durationSeconds,
            IReadOnlyList<ClipFramePacket> frames)
        {
            if (width <= 0 || width > 4096) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || height > 4096) throw new ArgumentOutOfRangeException(nameof(height));
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (frames == null || frames.Count == 0 || frames.Count > MaxFrames)
                throw new ArgumentException("A clip must contain 1..600 frames.", nameof(frames));

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write(width);
                writer.Write(height);
                writer.Write(durationSeconds);
                writer.Write(frames.Count);
                for (var i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    if (float.IsNaN(frame.TimeSeconds) || float.IsInfinity(frame.TimeSeconds) || frame.TimeSeconds < 0f)
                        throw new ArgumentException("Frame time is invalid.", nameof(frames));
                    var bytes = frame.JpegBytes ?? Array.Empty<byte>();
                    if (bytes.Length == 0 || bytes.Length > MaxFrameBytes)
                        throw new ArgumentException("Frame payload is invalid.", nameof(frames));
                    writer.Write(frame.TimeSeconds);
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                    if (stream.Length > MaxFileBytes)
                        throw new ArgumentException("Clip payload is too large.", nameof(frames));
                }
            }
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out ClipFileData data)
        {
            data = null;
            if (bytes == null || bytes.Length < 28 || bytes.Length > MaxFileBytes) return false;
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream, Encoding.UTF8, true);
                if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version) return false;
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                var duration = reader.ReadSingle();
                var count = reader.ReadInt32();
                if (width <= 0 || width > 4096 || height <= 0 || height > 4096 ||
                    float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f ||
                    count <= 0 || count > MaxFrames)
                    return false;

                var frames = new List<ClipFramePacket>(count);
                var priorTime = -1f;
                for (var i = 0; i < count; i++)
                {
                    var time = reader.ReadSingle();
                    var length = reader.ReadInt32();
                    if (float.IsNaN(time) || float.IsInfinity(time) || time < 0f || time < priorTime ||
                        length <= 0 || length > MaxFrameBytes || length > stream.Length - stream.Position)
                        return false;
                    var payload = reader.ReadBytes(length);
                    if (payload.Length != length) return false;
                    frames.Add(new ClipFramePacket(time, payload));
                    priorTime = time;
                }
                if (stream.Position != stream.Length) return false;
                data = new ClipFileData(width, height, duration, frames);
                return true;
            }
            catch (EndOfStreamException) { return false; }
            catch (IOException) { return false; }
        }

        public static string Sha256Hex(byte[] bytes)
        {
            if (bytes == null) return string.Empty;
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            var text = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++) text.Append(hash[i].ToString("x2"));
            return text.ToString();
        }
    }
}
