using Unity.Netcode;

namespace DeepDive.Composition
{
    public static class SessionCommand
    {
        public static bool TryRead(FastBufferReader reader, out uint sequence, out byte kind, out bool ready, out int revision)
        {
            sequence = 0; kind = 0; ready = false; revision = 0;
            // Named-message readers retain the transport header before Position.
            if (reader.Length - reader.Position != 10) return false;
            reader.ReadValueSafe(out sequence); reader.ReadValueSafe(out kind);
            reader.ReadValueSafe(out ready); reader.ReadValueSafe(out revision);
            return kind == 1 || kind == 2;
        }
    }
}
