using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // Session-scoped: zero is the host, not an invalid ID. Never persist this as a user ID.
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly ulong Value;
        public PlayerId(ulong value) => Value = value;
        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public enum ConnectionStatus { Offline, StartingHost, Connecting, Connected, Disconnecting }

    public readonly struct SceneLoadResult
    {
        public readonly ulong RequestId;
        public readonly string SceneName;
        public readonly bool Succeeded;
        public readonly string Error;
        public SceneLoadResult(ulong requestId, string sceneName, bool succeeded, string error)
        {
            RequestId = requestId;
            SceneName = sceneName;
            Succeeded = succeeded;
            Error = error;
        }
    }

    // Connection mechanics only. Mert owns SessionState, ready flags and transition policy.
    public interface INetworkSession
    {
        ConnectionStatus Status { get; }
        string LastError { get; }
        bool IsHost { get; }
        bool IsSceneLoading { get; }
        PlayerId? LocalPlayerId { get; }
        IReadOnlyList<PlayerId> Players { get; }
        event Action Changed;
        event Action<SceneLoadResult> SceneLoaded;
        bool StartHost(ushort port = 7777, string listenAddress = "0.0.0.0");
        bool Join(string address, ushort port = 7777);
        void Leave();
        bool SetJoinAllowed(bool allowed);
        bool TryLoadScene(string sceneName, ulong requestId);
    }
}
