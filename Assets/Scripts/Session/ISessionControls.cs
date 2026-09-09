using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Session
{
    public interface ISessionControls
    {
        INetworkSession Connection { get; }
        string LastAction { get; }
        event Action Changed;
        bool CreateRoom(ushort port);
        bool JoinRoom(string address, ushort port);
        void LeaveRoom();
        void ToggleReady();
        void AdvancePhase();
    }
}
