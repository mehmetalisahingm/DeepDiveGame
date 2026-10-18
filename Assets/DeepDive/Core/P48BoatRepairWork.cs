using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    public enum BoatRepairCommand : byte { Grab = 1, Place = 2, Hammer = 3, Cancel = 4 }

    // World identity only. NetworkPlayer resolves this from the real host ray.
    public interface IBoatRepairSlot { int PartIndex { get; } }

    public static class BoatRepairActions
    {
        public static Func<PlayerId, BoatRepairCommand, int, ulong, TransactionResult> Handler { get; private set; }
        public static void Bind(Func<PlayerId, BoatRepairCommand, int, ulong, TransactionResult> handler) => Handler = handler;
        public static void Unbind(Func<PlayerId, BoatRepairCommand, int, ulong, TransactionResult> handler)
        { if (Handler == handler) Handler = null; }
        public static TransactionResult Request(PlayerId player, BoatRepairCommand command, int part, ulong request) =>
            Handler != null ? Handler(player, command, part, request) : TransactionResult.Reject(request, "InvalidState", 0);
    }

    public readonly struct BoatRepairWork
    {
        public readonly int PartIndex, Hits;
        public readonly bool Placed;
        public readonly double LastHitAt;
        public BoatRepairWork(int part, bool placed, int hits, double lastHitAt)
        { PartIndex = part; Placed = placed; Hits = hits; LastHitAt = lastHitAt; }
    }

    // Transient reservations only. The injected host economy owns acquired/completed
    // material and atomic save/rollback; client ids and half-finished blows are not saved.
    public sealed class BoatRepairWorkSession
    {
        public const int RequiredHits = 3;
        public const double HitInterval = .45;
        private readonly Func<int, bool> acquired, completed;
        private readonly Func<PlayerId, int, ulong, TransactionResult> acquire, finish;
        private readonly Dictionary<PlayerId, BoatRepairWork> workers = new Dictionary<PlayerId, BoatRepairWork>();
        private readonly Dictionary<(PlayerId, ulong), (BoatRepairCommand Command, int Part, TransactionResult Result)> replies =
            new Dictionary<(PlayerId, ulong), (BoatRepairCommand, int, TransactionResult)>();
        private readonly Queue<(PlayerId, ulong)> replyOrder = new Queue<(PlayerId, ulong)>();
        private readonly Dictionary<PlayerId, ulong> lastRequest = new Dictionary<PlayerId, ulong>();

        public BoatRepairWorkSession(Func<int, bool> acquired, Func<int, bool> completed,
            Func<PlayerId, int, ulong, TransactionResult> acquire, Func<PlayerId, int, ulong, TransactionResult> finish)
        { this.acquired = acquired; this.completed = completed; this.acquire = acquire; this.finish = finish; }

        public bool TryGet(PlayerId player, out BoatRepairWork work) => workers.TryGetValue(player, out work);
        public bool IsReserved(int part)
        { foreach (var work in workers.Values) if (work.PartIndex == part) return true; return false; }
        public void Release(PlayerId player) => workers.Remove(player);

        public TransactionResult Execute(PlayerId player, BoatRepairCommand command, int part, ulong request, double now)
        {
            if (request == 0 || double.IsNaN(now) || double.IsInfinity(now)) return Reject(request, "InvalidState");
            var key = (player, request);
            if (replies.TryGetValue(key, out var old))
                return old.Command == command && old.Part == part ? old.Result : Reject(request, "RequestMismatch");
            if (lastRequest.TryGetValue(player, out var last) && request <= last) return Reject(request, "StaleRequest");
            lastRequest[player] = request;
            var result = Apply(player, command, part, request, now);
            // A failed disk write leaves the same operation retryable. There is no
            // mutation or consumed material to remember as a successful last blow.
            if (result.ReasonCode == "SaveFailed") { lastRequest[player] = request - 1; return result; }
            replies[key] = (command, part, result); replyOrder.Enqueue(key);
            while (replyOrder.Count > 128) replies.Remove(replyOrder.Dequeue());
            return result;
        }

        private TransactionResult Apply(PlayerId player, BoatRepairCommand command, int part, ulong request, double now)
        {
            if (command == BoatRepairCommand.Cancel)
            { workers.Remove(player); return TransactionResult.Ok(request, 0); }
            if (part < 0 || part >= BoatRepairParts.All.Count) return Reject(request, "InvalidTarget");
            if (completed(part)) return Reject(request, "AlreadyProcessed");
            if (command == BoatRepairCommand.Grab)
            {
                if (workers.ContainsKey(player)) return Reject(request, "HandsBusy");
                if (IsReserved(part)) return Reject(request, "PartInUse");
                if (!acquired(part))
                {
                    var result = acquire(player, part, request);
                    if (!result.Accepted) return result;
                }
                workers[player] = new BoatRepairWork(part, false, 0, double.NegativeInfinity);
                return TransactionResult.Ok(request, 0);
            }
            if (!workers.TryGetValue(player, out var work) || work.PartIndex != part) return Reject(request, "WrongPart");
            if (command == BoatRepairCommand.Place)
            {
                if (work.Placed) return Reject(request, "AlreadyPlaced");
                workers[player] = new BoatRepairWork(part, true, 0, double.NegativeInfinity);
                return TransactionResult.Ok(request, 0);
            }
            if (command != BoatRepairCommand.Hammer || !work.Placed) return Reject(request, "NotPlaced");
            if (now - work.LastHitAt < HitInterval) return Reject(request, "TooFast");
            if (work.Hits + 1 == RequiredHits)
            {
                var result = finish(player, part, request);
                if (result.Accepted) workers.Remove(player);
                return result;
            }
            workers[player] = new BoatRepairWork(part, true, work.Hits + 1, now);
            return TransactionResult.Ok(request, 0);
        }
        private static TransactionResult Reject(ulong request, string reason) => TransactionResult.Reject(request, reason, 0);
    }
}
