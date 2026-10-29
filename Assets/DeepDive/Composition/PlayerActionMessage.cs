using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Composition
{
    public enum PlayerVisualAction : byte { None, Collect, Deliver, Board, Disembark }

    // Optional, host-originated cosmetic extension. No inventory, root, reward or seat writes.
    public struct PlayerActionMessage
    {
        public ulong PlayerObject, Sequence;
        public double At;
        public PlayerVisualAction Kind;
        public Vector3 Source;
        public FixedString64Bytes Species;
        public int Count;
        public const int MaxBytes=160;
        public bool Valid(double now) => Sequence>0 && (Kind==PlayerVisualAction.Collect || Kind==PlayerVisualAction.Deliver) &&
            !double.IsNaN(At) && !double.IsInfinity(At) && now-At>=-.25 && now-At<1.8 &&
            Finite(Source.x) && Finite(Source.y) && Finite(Source.z) && Source.sqrMagnitude<100000000 &&
            Count>0 && Count<=4096;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public void Write(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(PlayerObject);writer.WriteValueSafe(Sequence);writer.WriteValueSafe(At);
            writer.WriteValueSafe((byte)Kind);writer.WriteValueSafe(Source);writer.WriteValueSafe(Species);writer.WriteValueSafe(Count);
        }
        public static bool Read(ref FastBufferReader reader,out PlayerActionMessage value)
        {
            value=default;
            if(reader.Length<43 || reader.Length>MaxBytes)return false;
            try
            {
                reader.ReadValueSafe(out value.PlayerObject);reader.ReadValueSafe(out value.Sequence);reader.ReadValueSafe(out value.At);
                reader.ReadValueSafe(out byte kind);value.Kind=(PlayerVisualAction)kind;
                reader.ReadValueSafe(out value.Source);reader.ReadValueSafe(out value.Species);reader.ReadValueSafe(out value.Count);
                return reader.Position==reader.Length;
            }
            catch(Exception error) when(error is OverflowException || error is InvalidOperationException || error is ArgumentException)
            {value=default;return false;}
        }
    }

    public sealed class PlayerActionReceipt
    {
        private readonly Dictionary<ulong,ulong> last=new Dictionary<ulong,ulong>();
        public bool Accept(ulong sender,PlayerActionMessage message,double now)
        {
            if(sender!=NetworkManager.ServerClientId || !message.Valid(now) ||
                (last.TryGetValue(message.PlayerObject,out var seen)&&message.Sequence<=seen))return false;
            last[message.PlayerObject]=message.Sequence;return true;
        }
    }
}
