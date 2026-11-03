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
            if (delta.magnitude > .25f) { MoveWatching(Around(CoastTarget(target)), focus); return; }
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
