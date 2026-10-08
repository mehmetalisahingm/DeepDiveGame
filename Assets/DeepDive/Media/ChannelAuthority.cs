using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Media
{
    // Host-only clip archive + in-game channel (#102, CONTRACTS "Izlenebilir medya ve kanal").
    // Pure C#: no scene, no network, no clock. Everything that decides money or rights is here or in
    // EconomyManager; the PC UI and the mirror only show what this class holds.
    //
    // Rules:
    //  - Archive: a clip is added once per clipId (a capture retry is AlreadyArchived); full archive refuses.
    //  - Publish: only the clip's owner, only a commercial candidate (complete, safely returned, real subject,
    //    quality 1..4), only while the day is running, only if the commercial right is still free. The
    //    publication id is a pure function of the clip, so a second click, a second player or a reload can only
    //    name the same publication: nothing is created twice.
    //  - The commercial right is taken in the SAME step as the publication (via the rights callback, which
    //    EconomyManager implements): a recording paid at the NPC cannot be published, a published one can no
    //    longer be paid at the NPC.
    //  - Results: a publication queued on day N is evaluated once when day N closes and credited then (the
    //    morning of N+1), by fixed host rules; the settle id is stored with it so a replay or a reload never
    //    pays twice. The client never reports views, followers or money.
    public sealed class ChannelAuthority : IMediaPersistence
    {
        public const int MaxArchivedClips = 128;
        private const byte OpPublish = 1;

        public interface IRights
        {
            // Atomically claim the channel right for this recording; false = already used at the NPC (or unknown).
            bool TryClaimForChannel(string recordingId);
            // Undo a claim whose publication could not be written (disk error): the NPC path is open again.
            void ReleaseChannelClaim(string recordingId);
            // Pay a settled publication once (idempotent by settleId), without its own disk write: the day
            // close persists everything in one transaction.
            bool CreditChannelIncome(string settleId, int amount);
        }

        public event Action OnChanged;
        // P4.5-C: a publication was accepted AND written (host). The manifest is the host's verified record of the clip (sponsors read it).
        public event Action<PublicationSave, ClipManifest> OnPublished;

        private readonly Dictionary<string, ClipManifest> _clips = new Dictionary<string, ClipManifest>(StringComparer.Ordinal);
        private readonly List<string> _clipOrder = new List<string>();
        private readonly Dictionary<string, PublicationSave> _publications = new Dictionary<string, PublicationSave>(StringComparer.Ordinal);
        private readonly List<string> _publicationOrder = new List<string>();
        private readonly HashSet<string> _publishedSubjects = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> _processed =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();

        private IRights _rights;
        private Func<bool> _dayLocked = () => false;
        private Func<int> _dayNumber = () => 1;
        private Func<bool> _persist = () => true;
        private Action<string> _queued;
        private int _followers;
        private int _revision;

        public int Revision => _revision;
        public int Followers => _followers;
        public int ClipCount => _clipOrder.Count;

        // persist writes the campaign file once for a publication (right + publication together); queued tells the
        // day ledger a publication was queued today.
        public void Configure(IRights rights, Func<int> dayNumber, Func<bool> dayLocked, Func<bool> persist = null, Action<string> queued = null)
        {
            _persist = persist ?? (() => true);
            _queued = queued;
            _rights = rights;
            _dayNumber = dayNumber ?? (() => 1);
            _dayLocked = dayLocked ?? (() => false);
        }

        // ---- archive ----------------------------------------------------------------------------------------

        public ClipArchiveOutcome Submit(ClipManifest manifest)
        {
            if (!manifest.IsWellFormed) return ClipArchiveOutcome.Invalid;
            if (_clips.ContainsKey(manifest.ClipId)) return ClipArchiveOutcome.AlreadyArchived;
            if (_clipOrder.Count >= MaxArchivedClips) return ClipArchiveOutcome.ArchiveFull;
            _clips[manifest.ClipId] = manifest;
            _clipOrder.Add(manifest.ClipId);
            Changed();
            return ClipArchiveOutcome.Added;
        }

        public bool TryGetClip(string clipId, out ClipManifest manifest)
        {
            manifest = default;
            return clipId != null && _clips.TryGetValue(clipId, out manifest);
        }

        public IReadOnlyList<ClipManifest> Clips()
        {
            var list = new List<ClipManifest>(_clipOrder.Count);
            foreach (var id in _clipOrder) list.Add(_clips[id]);
            return list;
        }

        public bool IsPublished(string clipId) => clipId != null && _publications.ContainsKey(ChannelIds.PublicationId(clipId));

        // ---- publish ----------------------------------------------------------------------------------------

        public TransactionResult TryPublish(PlayerId player, string clipId, string title, ulong requestId)
        {
            var key = (player, requestId, OpPublish);
            if (_processed.TryGetValue(key, out var replay)) return replay;
            // Refusals that can change by themselves (the day reopens) are not cached.
            if (_dayLocked()) return TransactionResult.Reject(requestId, "DayClosing", _revision);

            TransactionResult result;
            if (requestId == 0 || string.IsNullOrWhiteSpace(clipId) || !_clips.TryGetValue(clipId, out var clip))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (!clip.Owner.Equals(player))
                result = TransactionResult.Reject(requestId, "NotOwner", _revision);
            else if (!clip.MediaReady)
                result = TransactionResult.Reject(requestId, "MediaNotReady", _revision);
            else if (!clip.IsCommercialCandidate)
                result = TransactionResult.Reject(requestId, "NotPublishable", _revision);
            else if (IsPublished(clipId))
                result = TransactionResult.Reject(requestId, "PublicationAlreadyQueued", _revision);
            else if (_rights == null || !_rights.TryClaimForChannel(clip.RecordingId))
                result = TransactionResult.Reject(requestId, "RightsConsumed", _revision);
            else
            {
                var publication = new PublicationSave
                {
                    PublicationId = ChannelIds.PublicationId(clipId),
                    ClipId = clipId,
                    RecordingId = clip.RecordingId,
                    OwnerPlayerId = clip.Owner.Value,
                    SubjectId = clip.SubjectId,
                    Title = CleanTitle(title, clip),
                    QueuedDay = _dayNumber()
                };
                _publications[publication.PublicationId] = publication;
                _publicationOrder.Add(publication.PublicationId);
                if (!_persist())
                {
                    // Nothing half-done survives a failed write; the same request id may be retried.
                    _publications.Remove(publication.PublicationId);
                    _publicationOrder.Remove(publication.PublicationId);
                    _rights.ReleaseChannelClaim(clip.RecordingId);
                    return TransactionResult.Reject(requestId, "SaveFailed", _revision);
                }
                Changed();
                _queued?.Invoke(publication.PublicationId);
                result = TransactionResult.Ok(requestId, _revision);
                _processed[key] = result;
                OnPublished?.Invoke(publication, clip);   // after the result is cached: a listener can never make the answer change
                return result;
            }

            _processed[key] = result;
            return result;
        }

        private static string CleanTitle(string title, in ClipManifest clip)
        {
            var t = (title ?? string.Empty).Trim();
            if (t.Length == 0) t = clip.SubjectId + " - gun " + clip.DayNumber;
            return t.Length > ChannelIds.MaxTitleLength ? t.Substring(0, ChannelIds.MaxTitleLength) : t;
        }

        public IReadOnlyList<PublicationSave> Publications()
        {
            var list = new List<PublicationSave>(_publicationOrder.Count);
            foreach (var id in _publicationOrder) list.Add(Copy(_publications[id]));
            return list;
        }

        public int QueuedCount(int dayNumber)
        {
            var n = 0;
            foreach (var p in _publications.Values)
                if (p.QueuedDay == dayNumber && string.IsNullOrEmpty(p.SettledId)) n++;
            return n;
        }

        // ---- day-close results --------------------------------------------------------------------------------

        // Called once per close, inside the close transaction. Every publication queued on or before the closing
        // day and not yet settled gets its result; the payout is keyed by the settle id so a retry pays nothing.
        public int SettleThrough(int closingDay)
        {
            var settled = 0;
            foreach (var id in _publicationOrder)
            {
                var p = _publications[id];
                if (!string.IsNullOrEmpty(p.SettledId) || p.QueuedDay > closingDay) continue;
                if (!_clips.TryGetValue(p.ClipId, out var clip)) continue;

                var novel = !_publishedSubjects.Contains(p.SubjectId);
                ChannelResultRules.Evaluate(clip.Quality, novel, _followers, out var views, out var followers, out var income);
                var settleId = ChannelIds.SettleId(p.PublicationId);
                if (_rights != null && income > 0 && !_rights.CreditChannelIncome(settleId, income)) continue;

                p.Views = views;
                p.FollowersGained = followers;
                p.Income = income;
                p.ResultDay = closingDay + 1;
                p.SettledId = settleId;
                _followers += followers;
                _publishedSubjects.Add(p.SubjectId);
                settled++;
            }
            if (settled > 0) Changed();
            return settled;
        }

        // ---- persistence -------------------------------------------------------------------------------------

        public MediaSaveData ExportMedia()
        {
            var data = new MediaSaveData { Followers = _followers };
            foreach (var id in _clipOrder) data.Clips.Add(ClipSave.From(_clips[id]));
            foreach (var id in _publicationOrder) data.Publications.Add(Copy(_publications[id]));
            return data;
        }

        // A publication without its clip, or a second publication of one clip, is dropped (never forced in).
        public bool RestoreMedia(MediaSaveData data)
        {
            if (data == null) return false;
            _clips.Clear();
            _clipOrder.Clear();
            _publications.Clear();
            _publicationOrder.Clear();
            _publishedSubjects.Clear();
            _processed.Clear();
            _followers = Math.Max(0, data.Followers);

            if (data.Clips != null)
                foreach (var c in data.Clips)
                {
                    if (c == null) continue;
                    var m = c.ToManifest();
                    if (!m.IsWellFormed || _clips.ContainsKey(m.ClipId) || _clipOrder.Count >= MaxArchivedClips) continue;
                    _clips[m.ClipId] = m;
                    _clipOrder.Add(m.ClipId);
                }

            if (data.Publications != null)
                foreach (var p in data.Publications)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.ClipId) || !_clips.ContainsKey(p.ClipId)) continue;
                    var id = ChannelIds.PublicationId(p.ClipId);
                    if (_publications.ContainsKey(id)) continue;
                    var copy = Copy(p);
                    copy.PublicationId = id;
                    _publications[id] = copy;
                    _publicationOrder.Add(id);
                    if (!string.IsNullOrEmpty(copy.SettledId)) _publishedSubjects.Add(copy.SubjectId);
                }

            Changed();
            return true;
        }

        private static PublicationSave Copy(PublicationSave p) => new PublicationSave
        {
            PublicationId = p.PublicationId, ClipId = p.ClipId, RecordingId = p.RecordingId, OwnerPlayerId = p.OwnerPlayerId,
            SubjectId = p.SubjectId, Title = p.Title, QueuedDay = p.QueuedDay, ResultDay = p.ResultDay, Views = p.Views,
            FollowersGained = p.FollowersGained, Income = p.Income, SettledId = p.SettledId ?? string.Empty
        };

        private void Changed()
        {
            _revision++;
            OnChanged?.Invoke();
        }
    }

    // Fixed host rules (CONTRACTS P4.2 #6: "hostun sabitlenmis kalite/yenilik/risk kurallari"). Working values,
    // tuned in playtests like every price in the plan; deterministic so a replay reproduces the same result.
    public static class ChannelResultRules
    {
        public static void Evaluate(int quality, bool novelSubject, int currentFollowers, out int views, out int followersGained, out int income)
        {
            var q = Math.Max(0, Math.Min(4, quality));
            var baseViews = q == 0 ? 0 : 40 * q * q;               // 40 / 160 / 360 / 640
            if (novelSubject) baseViews = baseViews * 3 / 2;          // a first video of a subject draws more
            views = baseViews + currentFollowers / 2;                 // followers watch new posts
            followersGained = views / 20;
            income = views / 4;                                       // credits
        }
    }
}
