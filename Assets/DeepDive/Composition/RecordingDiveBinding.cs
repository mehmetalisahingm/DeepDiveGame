using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Session;
using DeepDive.World;

namespace DeepDive.Composition
{
    // Owns the lifetime of World bindings, not recording rules or price decisions.
    public sealed class RecordingDiveBinding : IRecordingSink, IDisposable
    {
        private readonly SessionManager session;
        private readonly InventoryManager inventory;
        private readonly Func<bool> isAuthority;
        private readonly HashSet<PlayerId> views = new HashSet<PlayerId>();
        private readonly Dictionary<string, (RecordingDirector Director, DiveSummary Summary)> pending =
            new Dictionary<string, (RecordingDirector, DiveSummary)>();
        private Func<RecordingResult, PlayerActionResult> payment;
        private DiveSummary? settling;
        private string sessionId, diveId;
        private bool disposed;

        public RecordingDirector Director { get; private set; }
        public int PendingDiveCount => pending.Count;
        public bool IsActive => !disposed && isAuthority() && session.State.Phase == SessionPhase.Dive &&
            !string.IsNullOrWhiteSpace(session.State.DiveId);

        // Composition-level observation hooks. The World recording rules still own what is a valid
        // take/result; these merely let P4 exploration consume the already host-verified outcome.
        // TakeRegistered is useful for remembering the observer's dive position. RecordingQueued
        // carries the persistent RecordingId minted at settlement, which is the id exploration saves.
        public event Action<RecordingTake> TakeRegistered;
        public event Action<RecordingResult> RecordingQueued;

        public RecordingDiveBinding(SessionManager session, InventoryManager inventory, Func<bool> isAuthority)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.isAuthority = isAuthority ?? throw new ArgumentNullException(nameof(isAuthority));
            session.OnSessionStateChanged += StateChanged;
            inventory.OnDiveSummaryReady += SummaryReady;
            Refresh();
        }

        // Connect Mert's real recording-payment API here when it exists. Null keeps the
        // World sink UNBOUND: a missing backend must not burn recordings for zero credits.
        public void SetPaymentHandler(Func<RecordingResult, PlayerActionResult> handler)
        {
            payment = handler;
            Refresh();
        }

        public void BindView(PlayerId player, IRecorderView view)
        {
            if (!IsActive) return;
            RecorderViews.Bind(player, view);
            views.Add(player);
        }

        public void RemoveView(PlayerId player)
        {
            if (views.Remove(player)) RecorderViews.Unbind(player);
        }

        private void StateChanged(SessionState state) => Refresh();

        public void Refresh()
        {
            if (disposed) return;
            if (sessionId != session.State.SessionId)
            {
                ReleaseDirector();
                pending.Clear();
                Director = null;
                diveId = null;
                sessionId = session.State.SessionId;
            }

            if (IsActive)
            {
                if (Director == null || diveId != session.State.DiveId)
                {
                    ReleaseDirector();
                    diveId = session.State.DiveId;
                    Director = new RecordingDirector();
                    Director.TakeRegistered += ForwardTakeRegistered;
                }
                RecordingEvaluation.Bind(Director);
            }
            else ReleaseEvaluation();

            if (isAuthority() && payment != null)
            {
                RecordingClaim.Bind(this);
                SettlePending();
            }
            else if (ReferenceEquals(RecordingClaim.Sink, this)) RecordingClaim.Unbind();
        }

        private void ForwardTakeRegistered(RecordingTake take) => TakeRegistered?.Invoke(take);

        private void SummaryReady(DiveSummary summary)
        {
            if (disposed || !isAuthority() || Director == null || summary.DiveId != diveId || Director.IsSettled || Director.ClaimCount == 0)
                return;
            pending[summary.DiveId] = (Director, summary);
            SettlePending();
        }

        private void SettlePending()
        {
            if (!ReferenceEquals(RecordingClaim.Sink, this) || payment == null) return;
            foreach (var id in new List<string>(pending.Keys))
            {
                var entry = pending[id];
                settling = entry.Summary;
                try
                {
                    // Director owns IsPayable, best-safe-record selection and replay guards.
                    entry.Director.SettleDive(entry.Summary);
                    if (entry.Director.IsSettled) pending.Remove(id);
                }
                finally { settling = null; }
            }
        }

        public PlayerActionResult TryClaim(RecordingResult result)
        {
            if (disposed || !isAuthority() || payment == null || !settling.HasValue ||
                settling.Value.DiveId != result.DiveId)
                return PlayerActionResult.InvalidState;

            var answer = payment(result);
            // Accepted is the normal path; DuplicateRequest is a retry of the same persistent
            // RecordingId. Exploration is idempotent by that id, so replaying the notification is safe
            // and lets it repair a missed observation without minting a second encyclopedia entry.
            if (answer == PlayerActionResult.Accepted || answer == PlayerActionResult.DuplicateRequest)
                RecordingQueued?.Invoke(result);
            return answer;
        }

        private void ReleaseEvaluation()
        {
            if (Director != null) RecordingEvaluation.Unbind(Director);
            foreach (var player in views) RecorderViews.Unbind(player);
            views.Clear();
        }

        private void ReleaseDirector()
        {
            ReleaseEvaluation();
            if (Director != null) Director.TakeRegistered -= ForwardTakeRegistered;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            session.OnSessionStateChanged -= StateChanged;
            inventory.OnDiveSummaryReady -= SummaryReady;
            ReleaseDirector();
            if (ReferenceEquals(RecordingClaim.Sink, this)) RecordingClaim.Unbind();
            pending.Clear();
            payment = null;
        }
    }
}
