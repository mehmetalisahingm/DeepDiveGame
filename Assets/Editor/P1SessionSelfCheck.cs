using System;
using DeepDive.Core;
using DeepDive.Session;
using DeepDive.Session.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    // Editor-only, headless check for the P1-C SessionManager state machine (issue #14). Mirrors
    // the P0Build.cs batchmode pattern so it can run via
    // -batchmode -quit -executeMethod DeepDive.Editor.P1SessionSelfCheck.Run
    public static class P1SessionSelfCheck
    {
        public const string ScenePath = "Assets/P1/Scenes/P1Session.unity";

        [MenuItem("DeepDive/Run P1 Session Self-Check")]
        public static void Run()
        {
            ValidateScene();
            RunStateMachineCheck();
        }

        private static void ValidateScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var host = GameObject.Find("P1Session");
            if (host == null)
                throw new InvalidOperationException("P1 session self-check failed: P1Session GameObject not found in " + ScenePath);

            var session = host.GetComponent<SessionManager>();
            if (session == null)
                throw new InvalidOperationException("P1 session self-check failed: SessionManager script did not resolve (missing script reference?).");

            var ui = host.GetComponent<SessionRoomUI>();
            if (ui == null)
                throw new InvalidOperationException("P1 session self-check failed: SessionRoomUI script did not resolve (missing script reference?).");

            Debug.Log("P1_SESSION_SCENE_OK | " + scene.path);
        }

        private static void RunStateMachineCheck()
        {
            var go = new GameObject("P1SessionSelfCheckRig");
            try
            {
                var session = go.AddComponent<SessionManager>();
                var playerA = new PlayerId("A");
                var playerB = new PlayerId("B");

                session.Initialize("check-session", "check-region");
                Require(session.State.Phase == SessionPhase.Lobby, "Initialize should start in Lobby");

                Require(session.BeginPrep() == SessionActionResult.PlayerInactive, "BeginPrep with nobody joined should be rejected");

                Require(session.Join(playerA) == SessionActionResult.Ok, "First join should succeed");
                Require(session.Join(playerA) == SessionActionResult.AlreadyProcessed, "Duplicate join should be AlreadyProcessed");
                Require(session.Join(playerB) == SessionActionResult.Ok, "Second join should succeed");

                Require(session.BeginPrep() == SessionActionResult.PlayerInactive, "BeginPrep should be rejected while a player is not ready");

                Require(session.SetReady(playerA, true) == SessionActionResult.Ok, "SetReady should succeed in Lobby");
                Require(session.SetReady(playerB, true) == SessionActionResult.Ok, "SetReady should succeed in Lobby");

                Require(session.BeginDive("x") == SessionActionResult.WrongPhase, "BeginDive from Lobby should be WrongPhase");

                var revisionBeforePrep = session.State.Revision;
                Require(session.BeginPrep() == SessionActionResult.Ok, "BeginPrep should succeed once everyone is ready");
                Require(session.State.Phase == SessionPhase.Prep, "Phase should be Prep after BeginPrep");
                Require(session.State.Revision == revisionBeforePrep + 1, "Revision should increment on transition");

                Require(session.SetReady(playerA, false) == SessionActionResult.WrongPhase, "SetReady outside Lobby should be WrongPhase");

                Require(session.BeginDive("dive-1") == SessionActionResult.Ok, "BeginDive should succeed from Prep");
                Require(session.State.Phase == SessionPhase.Dive, "Phase should be Dive after BeginDive");
                Require(session.State.DiveId == "dive-1", "DiveId should be set by BeginDive");

                Require(session.BeginReturn() == SessionActionResult.Ok, "BeginReturn should succeed from Dive");
                Require(session.State.Phase == SessionPhase.Return, "Phase should be Return after BeginReturn");

                Require(session.CompleteReturn() == SessionActionResult.Ok, "CompleteReturn should succeed from Return");
                Require(session.State.Phase == SessionPhase.Lobby, "Phase should be back to Lobby after CompleteReturn");
                Require(session.State.DiveId == string.Empty, "DiveId should be cleared after CompleteReturn");
                Require(!session.Roster[playerA], "Ready flags should reset after CompleteReturn");

                Require(session.Leave(playerA) == SessionActionResult.Ok, "Leave should succeed for a joined player");
                Require(session.Leave(playerA) == SessionActionResult.PlayerInactive, "Leave for an unknown player should be PlayerInactive");

                Debug.Log("P1_SESSION_CHECK_SUCCEEDED");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("P1 session self-check failed: " + message);
        }
    }
}
