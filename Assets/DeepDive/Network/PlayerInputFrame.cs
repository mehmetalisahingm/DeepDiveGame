using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Network
{
    public struct PlayerInputFrame : INetworkSerializable
    {
        public uint Sequence;
        public Vector3 Move;
        public float Yaw;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        { serializer.SerializeValue(ref Sequence); serializer.SerializeValue(ref Move); serializer.SerializeValue(ref Yaw); }
    }

    public sealed class ServerInputBuffer
    {
        private bool received;
        private uint sequence;
        private double receivedAt;
        private PlayerInputFrame current;

        public bool Accept(PlayerInputFrame frame, double now)
        {
            if (!Finite(frame.Move.x) || !Finite(frame.Move.y) || !Finite(frame.Move.z) || !Finite(frame.Yaw)) return false;
            if (received && unchecked((int)(frame.Sequence - sequence)) <= 0) return false;
            frame.Move = Vector3.ClampMagnitude(frame.Move, 1f);
            frame.Yaw = Mathf.Repeat(frame.Yaw, 360f);
            received = true; sequence = frame.Sequence; receivedAt = now; current = frame;
            return true;
        }

        public PlayerInputFrame Read(double now)
        {
            var result = current;
            if (!received || now - receivedAt > 0.25) result.Move = Vector3.zero;
            return result;
        }

        public void ClearMotion() => current.Move = Vector3.zero;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
