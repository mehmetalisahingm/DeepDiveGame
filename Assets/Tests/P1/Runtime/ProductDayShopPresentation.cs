using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    public sealed partial class ProductDaySmokeDriver
    {
        private Vector3 firstShopCarryPosition;
        private bool sawShopCarryPosition;
        private int lateShopStage;
        private float lateShopObservedAt;
        private float lateShopNextPhase;
        private float lateShopSpawnSettlesAt;

        // Development-only probe: the host uses the real shelf interaction before the
        // guest process starts. No selection or position is written by the probe.
        private void LateShopTick()
        {
            var inWorld = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ContinuousTownLayout.SceneName;
            if (Time.realtimeSinceStartup > nextTrace)
            {
                nextTrace = Time.realtimeSinceStartup + 5f;
                Debug.Log($"P246_LATE slot={report.slot} phase={adapter.Session.State.Phase} world={inWorld} stage={lateShopStage} wp={waypoint} pos={local.transform.position}");
            }
            if (Host && (!inWorld || adapter.Session.State.Phase != DeepDive.Session.SessionPhase.Lobby))
            {
                if (Time.realtimeSinceStartup < lateShopNextPhase) return;
                lateShopNextPhase = Time.realtimeSinceStartup + 1f;
                if (adapter.Session.State.Phase == DeepDive.Session.SessionPhase.Lobby &&
                    adapter.Session.Roster.TryGetValue(new PlayerId(local.OwnerClientId), out var ready) && !ready)
                    adapter.ToggleReady();
                else adapter.AdvancePhase();
                return;
            }
            if (!inWorld) return;
            var marker = Arg("-p246-sync");
            if (string.IsNullOrEmpty(marker)) { Fail("Missing late-shop rendezvous path"); return; }
            if (lateShopStage == 0)
            {
                if (!Host)
                {
                    if (lateShopSpawnSettlesAt == 0) lateShopSpawnSettlesAt = Time.realtimeSinceStartup + 4f;
                    if (Time.realtimeSinceStartup < lateShopSpawnSettlesAt) return;
                    // A joining player may be placed outside the house by the live
                    // session; do not route it through the home's interior wall.
                    if (local.transform.position.z > -39f) { lateShopStage = 1; route = null; }
                }
            }
            if (lateShopStage == 0)
            {
                if (route == null) SetRoute(Outward().Take(5));
                if (!Follow()) return;
                route = null; lateShopStage = 1;
            }
            var shelf = TownStoreRoute.Shelf("camera-basic");
            if (shelf == null) { Fail("Missing camera shelf"); return; }
            if (Host)
            {
                if (lateShopStage == 1)
                {
                    var stand = TownStoreRoute.Stand(shelf);
                    var delta = stand - local.transform.position; delta.y = 0;
                    if (delta.magnitude > .17f) { MoveToward(StoreAware(Around(stand))); aimAt = 0; return; }
                    Aim(shelf.transform.position);
                    if (!Settled()) return;
                    if (local.SelectedShopItem.Value.ToString() != "camera-basic")
                    { Act(local.SubmitServiceInteractionLocal); return; }
                    var basket = local.GetComponent<ShopCarryView>();
                    if (basket == null || !basket.Visible) return;
                    report.lateShopSawSelection = report.lateShopSawBasket = true;
                    File.WriteAllText(marker + ".ready", "selected-before-join");
                    lateShopStage = 2;
                }
                Stop();
                report.lateShopJoined = manager.ConnectedClientsIds.Count == 2;
                if (report.lateShopJoined && File.Exists(marker + ".observed")) Finish();
                return;
            }
            var host = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.IsSpawned && p.OwnerClientId == 0);
            if (host == null) return;
            report.lateShopJoined = true;
            report.lateShopSawSelection = host.SelectedShopItem.Value.ToString() == "camera-basic";
            var carry = host.GetComponent<ShopCarryView>();
            if (!report.lateShopSawSelection || carry == null || !carry.Visible) return;
            var target = host.transform.position + Vector3.forward * 2f;
            var distance = target - local.transform.position; distance.y = 0;
            if (distance.magnitude > .3f) { MoveToward(StoreAware(Around(CoastTarget(target)))); return; }
            Aim(carry.Focus);
            if (lateShopObservedAt == 0) lateShopObservedAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - lateShopObservedAt < 1f) return;
            var lens = local.GetComponentInChildren<Camera>(true);
            if (!InView(lens.WorldToViewportPoint(carry.Focus))) return;
            report.lateShopSawBasket = true;
            CaptureView(".late-shop.png");
            File.WriteAllText(marker + ".observed", "guest-saw-live-selection");
            Finish();
        }
        private bool ShopObserverNear()
        {
            if (expected == 1) return true;
            var peer = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p.OwnerClientId == 1);
            return peer != null && Vector3.Distance(peer.transform.position, local.transform.position) < 4.5f;
        }
        private void WatchShopCarry()
        {
            var worker = ActionWorker;
            var shop = FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None).Single(a => a.Definition.ServiceId == TownServiceCatalog.EquipmentShopId);
            var target = shop.WorldPosition + new Vector3(1.6f + Mathf.Max(0, report.slot - 1) * 1.2f, 0, 3f);
            var carry = worker.GetComponent<ShopCarryView>();
            var focus = carry != null && carry.Visible ? carry.Focus : worker.transform.position + Vector3.up * 1.2f;
            var delta = target - local.transform.position; delta.y = 0;
            if (delta.magnitude > .25f) { MoveWatching(StoreAware(Around(CoastTarget(target))), focus); return; }
            Stop(); Aim(focus);
            localDone = report.shelfCarryInView && report.shelfCarryWalkSeen && worker.EquippedCameraTier.Value > 0;
        }
        private void CaptureShopCarry()
        {
            if (finished || report.reload || local == null || !local.IsSpawned) return;
            var worker = ActionWorker;
            var carry = worker != null ? worker.GetComponent<ShopCarryView>() : null;
            if (carry == null || !carry.Visible) return;
            report.shelfCarrySeen = true;
            var error = carry.GripError(Host);
            report.maxShelfCarryGripError = Mathf.Max(report.maxShelfCarryGripError, error);
            if (!sawShopCarryPosition) { firstShopCarryPosition = worker.transform.position; sawShopCarryPosition = true; }
            if (Vector3.Distance(firstShopCarryPosition, worker.transform.position) > .3f) report.shelfCarryWalkSeen = true;
            var lens = local.GetComponentInChildren<Camera>(true);
            if (Host && !File.Exists(output + ".shop-carry-diagnostic.png") && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                CaptureView(".shop-carry-diagnostic.png");
                Debug.Log($"P246_VIEW left={lens.WorldToViewportPoint(carry.LeftGrip)} right={lens.WorldToViewportPoint(carry.RightGrip)} product={lens.WorldToViewportPoint(carry.Focus)} error={error}");
            }
            var visible = error < .035f && InView(lens.WorldToViewportPoint(carry.LeftGrip)) &&
                InView(lens.WorldToViewportPoint(carry.RightGrip)) && InView(lens.WorldToViewportPoint(carry.Focus));
            if (!visible) return;
            report.shelfCarryInView = true;
            if (!File.Exists(output + ".shop-carry.png") && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                CaptureView(".shop-carry.png");
        }
    }
}
