using System;
using System.Collections.Generic;

namespace DeepDive.Media
{
    // #255: an evicted clip's media file may only be deleted once the campaign save that no longer lists it is on disk. Until then the
    // previous save still names the clip, so deleting early could leave a saved campaign pointing at a missing file after a failed write or
    // a crash. Ids queued here are deleted by the next Flush, which the host calls only after a SUCCESSFUL save; a failed save keeps them
    // queued for the next one.
    public sealed class PendingMediaDeletes
    {
        private readonly List<string> _ids = new List<string>();

        public int Count => _ids.Count;

        public void Add(string clipId)
        {
            if (!string.IsNullOrWhiteSpace(clipId) && !_ids.Contains(clipId)) _ids.Add(clipId);
        }

        // Deletes every queued clip's media through the given action and empties the queue. A delete that throws stays queued.
        public int Flush(Action<string> delete)
        {
            if (delete == null) return 0;
            var done = 0;
            for (var i = _ids.Count - 1; i >= 0; i--)
            {
                try { delete(_ids[i]); _ids.RemoveAt(i); done++; }
                catch (Exception) { }
            }
            return done;
        }
    }
}
