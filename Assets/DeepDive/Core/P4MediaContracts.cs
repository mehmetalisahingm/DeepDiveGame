using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.2 (#100/#101/#102): the recorded-clip archive, the in-game channel and the seams between them.
    // docs/plan/CONTRACTS.md "RecordingClipManifest" (Mehmet media, Utku result -> Mert archive/publish) and
    // "PublicationState" (Mert). UnityEngine-free like every Core contract.
    //
    // OWNERSHIP NOTE: ClipManifest is the CONSUMER-SIDE shape Mert's archive needs, written from the CONTRACTS
    // row so #102 can start before #100 exists. Mehmet owns what a clip is and when one is valid; if his
    // capture needs a different field set, this struct follows his PR, not the other way round.
    public readonly struct ClipManifest
    {
        public readonly string ClipId;
        public readonly string RecordingId;      // the P3 recording this clip belongs to ("" = not a commercial candidate)
        public readonly string DiveId;
        public readonly int DayNumber;
        public readonly PlayerId Owner;          // the camera holder; host-resolved, never client-claimed
        public readonly string SubjectId;        // species/event id from Utku's verification ("" = empty frame)
        public readonly int Quality;             // 0 = not a commercial candidate, 1..4 as RecordingResult
        public readonly float DurationSeconds;
        public readonly string ContentHash;
        public readonly long SizeBytes;
        public readonly bool MediaReady;         // manifest complete: expected size/hash/duration all arrived
        public readonly bool SafeReturned;       // the recording made it back to safety (D07)

        public ClipManifest(string clipId, string recordingId, string diveId, int dayNumber, PlayerId owner,
            string subjectId, int quality, float durationSeconds, string contentHash, long sizeBytes,
            bool mediaReady, bool safeReturned)
        {
            ClipId = clipId ?? string.Empty;
            RecordingId = recordingId ?? string.Empty;
            DiveId = diveId ?? string.Empty;
            DayNumber = dayNumber;
            Owner = owner;
            SubjectId = subjectId ?? string.Empty;
            Quality = quality;
            DurationSeconds = durationSeconds;
            ContentHash = contentHash ?? string.Empty;
            SizeBytes = sizeBytes;
            MediaReady = mediaReady;
            SafeReturned = safeReturned;
        }

        public bool IsWellFormed => !string.IsNullOrWhiteSpace(ClipId) && DurationSeconds > 0f && SizeBytes >= 0;

        // Publishable = a complete, safely returned clip of a real subject with a commercial quality.
        public bool IsCommercialCandidate => MediaReady && SafeReturned && !string.IsNullOrWhiteSpace(RecordingId) &&
                                             !string.IsNullOrWhiteSpace(SubjectId) && Quality >= 1 && Quality <= 4;
    }

    public enum ClipArchiveOutcome : byte
    {
        Invalid = 0,
        Added = 1,
        AlreadyArchived = 2,
        ArchiveFull = 3
    }

    // Mehmet's host-authoritative capture hands a finished clip to the archive here. Bound by the archive owner
    // (Mert's media binding) while it is the host; unbound = no archive on this machine, the clip is refused.
    public static class ClipArchive
    {
        public static Func<ClipManifest, ClipArchiveOutcome> SubmitHandler { get; private set; }

        public static void Bind(Func<ClipManifest, ClipArchiveOutcome> submit) => SubmitHandler = submit;

        public static void Unbind(Func<ClipManifest, ClipArchiveOutcome> submit)
        {
            if (SubmitHandler == submit) SubmitHandler = null;
        }

        public static ClipArchiveOutcome TrySubmit(in ClipManifest manifest) =>
            SubmitHandler != null ? SubmitHandler(manifest) : ClipArchiveOutcome.Invalid;
    }

    // Mehmet's playback seam as the PC screen consumes it: can this machine play this clip, and start it.
    // Unbound = no player yet; the PC shows the clip's verified metadata and says so, never a fake video.
    public static class ClipPlayback
    {
        public static Func<string, bool> CanPlayHandler { get; private set; }
        public static Func<string, bool> PlayHandler { get; private set; }

        public static void Bind(Func<string, bool> canPlay, Func<string, bool> play)
        {
            CanPlayHandler = canPlay;
            PlayHandler = play;
        }

        public static void Unbind(Func<string, bool> canPlay)
        {
            if (CanPlayHandler != canPlay) return;
            CanPlayHandler = null;
            PlayHandler = null;
        }

        public static bool CanPlay(string clipId) => CanPlayHandler != null && CanPlayHandler(clipId);
        public static bool TryPlay(string clipId) => PlayHandler != null && PlayHandler(clipId);
    }

    public static class ChannelIds
    {
        // One clip, one publication: the id is a pure function of the clip, so a retry, a second player at the
        // PC or a reload can only ever name the SAME publication.
        public static string PublicationId(string clipId) => "pub-" + clipId;

        // The day-close settlement of a publication; written with the payout so it is paid once.
        public static string SettleId(string publicationId) => "settle-" + publicationId;

        public const int MaxTitleLength = 48;
    }

    // ---- save shapes -----------------------------------------------------------------------------------------

    [Serializable]
    public sealed class ClipSave
    {
        public string ClipId = "";
        public string RecordingId = "";
        public string DiveId = "";
        public int DayNumber;
        public ulong OwnerPlayerId;
        public string SubjectId = "";
        public int Quality;
        public float DurationSeconds;
        public string ContentHash = "";
        public long SizeBytes;
        public bool MediaReady;
        public bool SafeReturned;

        public static ClipSave From(in ClipManifest m) => new ClipSave
        {
            ClipId = m.ClipId, RecordingId = m.RecordingId, DiveId = m.DiveId, DayNumber = m.DayNumber,
            OwnerPlayerId = m.Owner.Value, SubjectId = m.SubjectId, Quality = m.Quality, DurationSeconds = m.DurationSeconds,
            ContentHash = m.ContentHash, SizeBytes = m.SizeBytes, MediaReady = m.MediaReady, SafeReturned = m.SafeReturned
        };

        public ClipManifest ToManifest() => new ClipManifest(ClipId, RecordingId, DiveId, DayNumber, new PlayerId(OwnerPlayerId),
            SubjectId, Quality, DurationSeconds, ContentHash, SizeBytes, MediaReady, SafeReturned);
    }

    // CONTRACTS "PublicationState": publicationId/clipId, rights, title, queuedDay/resultDay, views/followers/income, settledId.
    [Serializable]
    public sealed class PublicationSave
    {
        public string PublicationId = "";
        public string ClipId = "";
        public string RecordingId = "";
        public ulong OwnerPlayerId;
        public string SubjectId = "";
        public string Title = "";
        public int QueuedDay;
        public int ResultDay;
        public int Views;
        public int FollowersGained;
        public int Income;
        public string SettledId = "";
    }

    [Serializable]
    public sealed class MediaSaveData
    {
        public List<ClipSave> Clips = new List<ClipSave>();
        public List<PublicationSave> Publications = new List<PublicationSave>();
        public int Followers;
    }

    public interface IMediaPersistence
    {
        MediaSaveData ExportMedia();
        bool RestoreMedia(MediaSaveData data);
    }
}
