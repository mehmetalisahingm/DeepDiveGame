using System;

namespace DeepDive.Core
{
    // Placeholder pending Mehmet's real network PlayerId definition (docs/plan/CONTRACTS.md:
    // "PlayerId | ... | Mehmet -> tumu | P1"). Wraps a session-scoped connection id; not the
    // persistent campaign owner id. Session/UI code depends on this shape only.
    [Serializable]
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly string Value;

        public PlayerId(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("PlayerId value cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;

        public static bool operator ==(PlayerId a, PlayerId b) => a.Equals(b);
        public static bool operator !=(PlayerId a, PlayerId b) => !a.Equals(b);
    }
}
