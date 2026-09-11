using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.Inventory
{
    public sealed class PlayerBag
    {
        public readonly List<CaptureResult> Items = new List<CaptureResult>();
        public int WeightGrams { get; internal set; }
        public bool SafelyReturned { get; internal set; }
    }

    // Owns bag/weight capacity, catch pickup, safe return and dive summary (docs/plan/
    // CONTRACTS.md: "Canta/depo ve ekipman sahipligi | Mert" and "DiveSummary ... | Mert").
    //
    // Mirrors SessionManager's original shape (P1-C, before network integration added
    // Bridge/authority checks): a pure state authority meant to be driven by host-side
    // composition once Mehmet's pickup request (P2-A, issue #20) and Utku's catch
    // validation (P2-B, issue #21) exist. Nothing here decides network authority; the
    // same authority guard SessionManager grew during P1 integration should be added
    // here when the real pickup-request transport is wired.
    [RequireComponent(typeof(SessionManager))]
    public class InventoryManager : MonoBehaviour
    {
        // 20kg placeholder; retune with Mehmet/Utku once real species weights exist (P2-B).
        public const int CapacityGrams = 20000;

        public event Action<PlayerId> OnBagChanged;
        public event Action<DiveSummary> OnDiveSummaryReady;

        private SessionManager _session;
        private readonly Dictionary<PlayerId, PlayerBag> _bags = new Dictionary<PlayerId, PlayerBag>();
        private readonly HashSet<string> _claimedCaptureIds = new HashSet<string>();
        private string _trackedDiveId = string.Empty;
        private SessionPhase _previousPhase = SessionPhase.Lobby;
        private bool _subscribed;

        public IReadOnlyDictionary<PlayerId, PlayerBag> Bags => _bags;

        // Pure projection of one player's bag, for anything (e.g. InventoryPlayerSync) that
        // needs to mirror it without depending on PlayerBag's mutable internals.
        public (int WeightGrams, int ItemCount, bool SafelyReturned) SnapshotFor(PlayerId player) =>
            _bags.TryGetValue(player, out var bag)
                ? (bag.WeightGrams, bag.Items.Count, bag.SafelyReturned)
                : (0, 0, false);

        // Resolved lazily instead of in Awake(): AddComponent does not guarantee Awake has run
        // by the time a caller (e.g. an EditMode test right after AddComponent) uses this.
        private SessionManager Session
        {
            get
            {
                if (_session == null) _session = GetComponent<SessionManager>();
                EnsureSubscribed();
                return _session;
            }
        }

        private void Awake() => EnsureSubscribed();
        private void OnEnable() => EnsureSubscribed();

        private void EnsureSubscribed()
        {
            if (_subscribed) return;
            if (_session == null) _session = GetComponent<SessionManager>();
            if (_session == null) return;
            // Catch up first: Join()/phase changes may have already happened (e.g. AddComponent
            // does not guarantee Awake ran before a caller's next line) before we could listen.
            _previousPhase = _session.State.Phase;
            _trackedDiveId = _session.State.Phase == SessionPhase.Dive ? _session.State.DiveId : string.Empty;
            _session.OnSessionStateChanged += HandleSessionStateChanged;
            _session.OnRosterChanged += HandleRosterChanged;
            _subscribed = true;
            HandleRosterChanged(_session.Roster);
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _session.OnSessionStateChanged -= HandleSessionStateChanged;
            _session.OnRosterChanged -= HandleRosterChanged;
            _subscribed = false;
        }

        // Called by host-side composition once a pickup request is validated by Utku's catch
        // registry (docs/plan/CONTRACTS.md "Av alma" akisi, adim 3-4). Capacity is checked
        // last so a rejected pickup never claims the catch: it stays available on the ground.
        public InventoryActionResult TryAddCatch(PlayerId player, CaptureResult capture)
        {
            if (Session.State.Phase != SessionPhase.Dive) return InventoryActionResult.WrongPhase;
            if (!_bags.TryGetValue(player, out var bag)) return InventoryActionResult.PlayerInactive;
            if (capture.DiveId != Session.State.DiveId) return InventoryActionResult.InvalidTarget;
            if (_claimedCaptureIds.Contains(capture.CaptureId)) return InventoryActionResult.AlreadyClaimed;
            if (bag.WeightGrams + capture.WeightGrams > CapacityGrams) return InventoryActionResult.InventoryFull;

            _claimedCaptureIds.Add(capture.CaptureId);
            bag.Items.Add(capture);
            bag.WeightGrams += capture.WeightGrams;
            OnBagChanged?.Invoke(player);
            return InventoryActionResult.Ok;
        }

        // Called when a player reaches the dive's exit while still active (not passive/out of
        // oxygen). Only safely-returned bags survive FinalizeDive; the rest are lost (D07).
        public InventoryActionResult TryMarkSafeReturn(PlayerId player)
        {
            if (Session.State.Phase != SessionPhase.Dive) return InventoryActionResult.WrongPhase;
            if (!_bags.TryGetValue(player, out var bag)) return InventoryActionResult.PlayerInactive;
            if (bag.SafelyReturned) return InventoryActionResult.AlreadyClaimed;

            bag.SafelyReturned = true;
            OnBagChanged?.Invoke(player);
            return InventoryActionResult.Ok;
        }

        private void HandleRosterChanged(IReadOnlyDictionary<PlayerId, bool> roster)
        {
            foreach (var player in roster.Keys)
                if (!_bags.ContainsKey(player)) _bags[player] = new PlayerBag();

            // Only reconcile removals in Lobby: a disconnect mid-dive must still count as a
            // loss in FinalizeDive, not silently disappear from the summary.
            if (_session.State.Phase != SessionPhase.Lobby) return;
            var stale = new List<PlayerId>();
            foreach (var player in _bags.Keys)
                if (!roster.ContainsKey(player)) stale.Add(player);
            foreach (var player in stale) _bags.Remove(player);
        }

        private void HandleSessionStateChanged(SessionState state)
        {
            if (state.Phase == SessionPhase.Dive && _previousPhase != SessionPhase.Dive)
            {
                // New dive: clear claim tracking and every bag (CONTRACTS: eski diveId'ye ait
                // av/islem istegi yeniden kullanilamaz).
                _trackedDiveId = state.DiveId;
                _claimedCaptureIds.Clear();
                foreach (var player in new List<PlayerId>(_bags.Keys))
                    ResetBag(player);
            }
            else if (state.Phase == SessionPhase.Return && _previousPhase == SessionPhase.Dive)
            {
                FinalizeDive(); // Bags stay intact so Return-phase UI can show the summary.
            }
            else if (state.Phase == SessionPhase.Lobby && _previousPhase == SessionPhase.Return)
            {
                foreach (var player in new List<PlayerId>(_bags.Keys))
                    ResetBag(player);
            }

            _previousPhase = state.Phase;
        }

        private void ResetBag(PlayerId player)
        {
            if (!_bags.TryGetValue(player, out var bag))
            {
                bag = new PlayerBag();
                _bags[player] = bag;
            }

            bag.Items.Clear();
            bag.WeightGrams = 0;
            bag.SafelyReturned = false;
            OnBagChanged?.Invoke(player);
        }

        private void FinalizeDive()
        {
            var safe = new List<PlayerId>();
            var preserved = new List<string>();
            var lost = new List<string>();
            foreach (var pair in _bags)
            {
                var target = pair.Value.SafelyReturned ? preserved : lost;
                foreach (var item in pair.Value.Items)
                    target.Add(item.CaptureId);
                if (pair.Value.SafelyReturned)
                    safe.Add(pair.Key);
            }

            var summary = new DiveSummary(_trackedDiveId, safe, preserved, lost, Guid.NewGuid().ToString("N"));
            OnDiveSummaryReady?.Invoke(summary);
        }
    }
}
