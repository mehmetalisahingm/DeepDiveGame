using System;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Companion for the existing -Record integrated smoke. It does not replace any product authority:
    // the real EconomyManager purchases the selected camera before the dive, DayManager's real host
    // clock is advanced to 23:00, and the normal RecordingNetworkBridge/RecordingSubject path does the take.
    //
    // The World P4.3-B1 type is discovered by reflection so this support harness can land independently;
    // once #114 is present it also prints the real tier light-gate result (Basic=TooDark, Pro=Accepted).
    public sealed class P4CameraCapabilitySmokeControl : MonoBehaviour
    {
        private const int NightMinute = 23 * 60;
        private const float FastMinutesPerSecond = 480f;
        private const ulong ProfessionalPurchaseRequest = 9430301;

        private SessionNetworkAdapter adapter;
        private bool cameraReady;
        private bool nightReady;
        private bool evidenceLogged;
        private PlayerId recorder;

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        private string Mode => Arg("-p4-camera-smoke", "").Trim().ToLowerInvariant();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var mode = Arg("-p4-camera-smoke", "").Trim().ToLowerInvariant();
            if (mode != "basic" && mode != "professional") return;
            var go = new GameObject("P4CameraCapabilitySmokeControl");
            DontDestroyOnLoad(go);
            go.AddComponent<P4CameraCapabilitySmokeControl>();
        }

        private void Update()
        {
            if (adapter == null) adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null || !adapter.IsAuthority) return;

            var state = adapter.Session.State;
            if (state.Phase == SessionPhase.Prep && !adapter.Connection.IsSceneLoading)
                PrepareCamera();

            if (state.Phase == SessionPhase.Dive && !adapter.Connection.IsSceneLoading)
                PrepareNightAndEvidence();
        }

        private void PrepareCamera()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            if (economy == null) return;

            P4EquipmentCatalog.Apply(economy);
            var guests = adapter.Session.Roster.Keys.Where(k => k.Value != 0).OrderBy(k => k.Value).ToArray();
            recorder = guests.Length > 0 ? guests[0] : new PlayerId(0);

            // IntegratedSmokeDriver seeds the real Basic purchase first. Wait for that instead of
            // bypassing the purchase authority or inventing a second loadout source.
            if (!economy.LoadoutFor(recorder).Contains(EconomyManager.CameraBasicId)) return;

            if (Mode == "professional" &&
                !economy.LoadoutFor(recorder).Contains(P4EquipmentCatalog.CameraProfessionalId))
            {
                // Smoke-only balance seed, same pattern as the existing -Record driver. Export/restore
                // preserves Basic ownership; the actual Professional acquisition is still TryPurchase.
                var seed = economy.ExportSaveData("p4-camera-smoke", "p4-camera-smoke");
                seed.SharedBalance = Math.Max(seed.SharedBalance, P4EquipmentCatalog.CameraProfessionalPrice);
                if (!economy.TryRestore(seed))
                {
                    Debug.LogError("P4_CAMERA_SMOKE_SETUP_FAIL restore");
                    enabled = false;
                    return;
                }

                P4EquipmentCatalog.Apply(economy);
                var purchase = economy.TryPurchase(recorder, P4EquipmentCatalog.CameraProfessionalId,
                    ProfessionalPurchaseRequest);
                if (!purchase.Accepted)
                {
                    Debug.LogError($"P4_CAMERA_SMOKE_SETUP_FAIL pro-purchase reason={purchase.ReasonCode}");
                    enabled = false;
                    return;
                }
            }

            cameraReady = true;
            Debug.Log($"P4_CAMERA_SMOKE_CAMERA_READY mode={Mode} recorder={recorder.Value} " +
                      $"owned={string.Join(",", economy.LoadoutFor(recorder))}");
        }

        private void PrepareNightAndEvidence()
        {
            if (!cameraReady) return;
            var day = FindFirstObjectByType<DayManager>();
            if (day == null) return;

            if (!nightReady)
            {
                if (day.Engine.ClockMinute < NightMinute)
                {
                    day.Engine.GameMinutesPerRealSecond = FastMinutesPerSecond;
                    return;
                }

                day.Engine.GameMinutesPerRealSecond = 0f;
                nightReady = true;
            }

            if (evidenceLogged) return;
            var provider = DayLock.StateProvider;
            if (provider == null)
            {
                Debug.LogError("P4_CAMERA_SMOKE_NIGHT_FAIL day-provider-unbound");
                evidenceLogged = true;
                return;
            }

            var subject = FindObjectsByType<RecordingSubject>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.GetComponent<FishActor>() != null);
            var player = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.IsSpawned && x.OwnerClientId == recorder.Value);
            if (subject == null || player == null) return;

            var depth = 0f;
            var field = FindFirstObjectByType<WaterField>();
            if (field != null) WaterDepth.TryDepthAt(field.Bodies, subject.transform.position, out depth);

            var daylight = DaylightModel.Evaluate(provider());
            var rulesType = typeof(RecordingSubject).Assembly.GetType("DeepDive.World.RecordingCameraRules");
            var rulesState = "missing";
            var light01 = Mathf.Clamp01(daylight.AmbientMultiplier);
            var basicGate = "unknown";
            var proGate = "unknown";

            if (rulesType != null)
            {
                var depthFactorMethod = rulesType.GetMethod("DepthFactor");
                var litEnoughMethod = rulesType.GetMethod("IsLitEnough");
                if (depthFactorMethod != null && litEnoughMethod != null)
                {
                    var depthFactor = (float)depthFactorMethod.Invoke(null, new object[] { depth });
                    light01 *= depthFactor;
                    var basicLit = (bool)litEnoughMethod.Invoke(null, new object[] { CameraTier.Basic, light01 });
                    var proLit = (bool)litEnoughMethod.Invoke(null, new object[] { CameraTier.Professional, light01 });
                    basicGate = basicLit ? "Accepted" : "TooDark";
                    proGate = proLit ? "Accepted" : "TooDark";
                    rulesState = "bound";
                }
            }

            Debug.Log($"P4_CAMERA_SMOKE_NIGHT_READY mode={Mode} minute={day.Engine.ClockMinute} " +
                      $"provider=True tier={player.CurrentCameraTier} depth={depth:F2} light={light01:F3} " +
                      $"rules={rulesState} basicGate={basicGate} proGate={proGate}");
            evidenceLogged = true;
        }
    }
}
