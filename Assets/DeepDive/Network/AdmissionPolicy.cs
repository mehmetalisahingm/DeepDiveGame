using System.Collections.Generic;

namespace DeepDive.Network
{
    public sealed class AdmissionPolicy
    {
        public const int MaxPlayers = 4;
        private readonly Dictionary<ulong, int> slots = new Dictionary<ulong, int>();
        public int Count => slots.Count;

        // Reserve during approval, not only after connection: simultaneous joins count too.
        public bool TryReserve(ulong id, bool joinsAllowed, bool protocolMatches, out int slot, out string reason)
        {
            slot = -1;
            reason = "";
            if (!protocolMatches) { reason = "ProtocolMismatch"; return false; }
            if (!joinsAllowed) { reason = "WrongPhase"; return false; }
            if (slots.TryGetValue(id, out slot)) return true;
            if (slots.Count >= MaxPlayers) { reason = "RoomFull"; return false; }
            for (slot = 0; slot < MaxPlayers; slot++)
                if (!slots.ContainsValue(slot)) { slots.Add(id, slot); return true; }
            return false;
        }

        public int SlotOf(ulong id) => slots.TryGetValue(id, out var slot) ? slot : -1;
        public void Release(ulong id) => slots.Remove(id);
        public void Clear() => slots.Clear();
    }
}
