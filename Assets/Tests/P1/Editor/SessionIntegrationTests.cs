using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Session;
using DeepDive.Composition;
using Unity.Collections;
using Unity.Netcode;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P1.Tests
{
    public class SessionIntegrationTests
    {
        private sealed class Bridge : ISessionNetworkBridge
        {
            public bool IsAuthority { get; set; } = true;
            public bool Accept = true;
            public bool RequestSceneLoad(SessionState state) => Accept;
        }
        private GameObject root;
        private SessionManager session;
        private Bridge bridge;
        [SetUp] public void SetUp()
        {
            root = new GameObject("session-test"); session = root.AddComponent<SessionManager>();
            bridge = new Bridge(); session.Bridge = bridge;
            session.Initialize("room", "DiveTestArea"); session.Join(new PlayerId(0));
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test] public void CommandDecodesAfterTransportHeaderAndRejectsTruncatedPayload()
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(123UL);
            writer.WriteValueSafe(7U); writer.WriteValueSafe((byte)1);
            writer.WriteValueSafe(true); writer.WriteValueSafe(3);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong _);
            Assert.IsTrue(SessionCommand.TryRead(reader, out var sequence, out var kind, out var ready, out var revision));
            Assert.AreEqual(7U, sequence); Assert.AreEqual(1, kind);
            Assert.IsTrue(ready); Assert.AreEqual(3, revision);
            reader.Seek(9);
            Assert.IsFalse(SessionCommand.TryRead(reader, out _, out _, out _, out _));
        }

        [Test] public void SceneLoadRejectionDoesNotConsumePhaseOrRevision()
        {
            session.SetReady(new PlayerId(0), true);
            bridge.Accept = false;
            Assert.AreEqual(SessionActionResult.SceneLoadFailed, session.BeginPrep());
            Assert.AreEqual(SessionPhase.Lobby, session.State.Phase);
            Assert.AreEqual(0, session.State.Revision);
            bridge.Accept = true;
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            bridge.Accept = false;
            Assert.AreEqual(SessionActionResult.SceneLoadFailed, session.BeginDive("dive"));
            Assert.AreEqual(SessionPhase.Prep, session.State.Phase);
            Assert.AreEqual("", session.State.DiveId);
        }
        [Test] public void EveryPlayerMustBeReadyAndReturnResetsEveryone()
        {
            session.Join(new PlayerId(1)); session.SetReady(new PlayerId(0), true);
            Assert.AreEqual(SessionActionResult.PlayerInactive, session.BeginPrep());
            session.SetReady(new PlayerId(1), true);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.Ok, session.BeginDive("one"));
            Assert.AreEqual(SessionActionResult.WrongPhase, session.Join(new PlayerId(2)));
            session.BeginReturn(); session.CompleteReturn();
            Assert.AreEqual(SessionPhase.Lobby, session.State.Phase);
            Assert.IsFalse(session.Roster[new PlayerId(0)]);
            Assert.IsFalse(session.Roster[new PlayerId(1)]);
            Assert.AreEqual("", session.State.DiveId);
            Assert.AreEqual(4, session.State.Revision);
        }
        [Test] public void ClientCannotMutateAuthoritativeState()
        {
            session.SetReady(new PlayerId(0), true); bridge.IsAuthority = false;
            Assert.AreEqual(SessionActionResult.NotHost, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.NotHost, session.SetReady(new PlayerId(0), false));
            Assert.AreEqual(SessionActionResult.NotHost, session.Leave(new PlayerId(0)));
            Assert.AreEqual(SessionActionResult.NotHost, session.Join(new PlayerId(1)));
            Assert.AreEqual(0, session.State.Revision);
        }
        [Test] public void RemoteRosterReplacesDisconnectedPlayersAndRejectsOldRevision()
        {
            bridge.IsAuthority = false;
            var state = SessionState.CreateLobby("room", "DiveTestArea"); state.Revision = 4;
            session.ApplyRemoteSnapshot(state, new Dictionary<PlayerId, bool> { [new PlayerId(1)] = true });
            Assert.IsFalse(session.Roster.ContainsKey(new PlayerId(0)));
            Assert.IsTrue(session.Roster[new PlayerId(1)]);
            state.Revision = 3;
            session.ApplyRemoteSnapshot(state, new Dictionary<PlayerId, bool>());
            Assert.AreEqual(1, session.Roster.Count);
        }
    }
}
