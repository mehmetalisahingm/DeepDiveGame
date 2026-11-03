using System;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.Composition
{
    // Presentation of the host's unpaid selection. No stock, inventory, animation
    // event or RPC can grant an item; the existing cashier remains the authority.
    [DefaultExecutionOrder(400), DisallowMultipleComponent]
    public sealed class ShopCarryView : MonoBehaviour
    {
        private NetworkPlayer player;
        private PlayerPresentationView hands;
        private PlayerActionView actions;
        private GameObject basket;
        private Transform product, left, right;
        private string shown = "";
        public string ProductId => Visible ? shown : "";
        public bool Visible => basket != null && basket.activeInHierarchy;
        public Vector3 Focus => product != null ? product.position + Vector3.up * .06f : transform.position;
        public Vector3 LeftGrip => left != null ? left.position : Vector3.zero;
        public Vector3 RightGrip => right != null ? right.position : Vector3.zero;
        public float GripError(bool owner) => hands != null && Visible
            ? hands.ActionGripError(owner, LeftGrip, RightGrip) : float.PositiveInfinity;

        private void Awake()
        {
            player = GetComponent<NetworkPlayer>();
            hands = GetComponent<PlayerPresentationView>();
            actions = GetComponent<PlayerActionView>();
        }
        public static Pose HeldPose(Transform frame) => new Pose(frame.TransformPoint(new Vector3(0, 1.30f, .29f)), frame.rotation);

        public static GameObject CreateBasket(Transform parent, string itemId)
        {
            if (Array.IndexOf(ShopShelfAnchor.ItemIds, itemId) < 0) return null;
            var prefab = Resources.Load<GameObject>(PlayerActionView.CrateResource);
            if (prefab == null) return null;
            var root = new GameObject("UnpaidShoppingBasket"); root.transform.SetParent(parent, false);
            var tray = Instantiate(prefab, root.transform, false);
            tray.name = "Tray";
            // A shallow version of the existing authored wooden market crate.
            // Products keep their own metre scale as a sibling, not under this scale.
            tray.transform.localScale = new Vector3(.82f, .55f, 1f);
            var sample = new GameObject("SelectedProduct"); sample.transform.SetParent(root.transform, false);
            ShopShelfPresentation.AddSample(sample.transform, itemId,
                Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName));
            if (itemId.StartsWith("camera-")) sample.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var renderers = sample.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                sample.transform.localPosition = Vector3.up * (-.06f + bounds.size.y * .5f);
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return root;
        }

        private void LateUpdate()
        {
            var valid = player != null && player.IsSpawned && !player.Passive.Value && !player.Seated.Value &&
                !player.Swimming.Value && !player.RepairHandsBusy && !player.RecordingPresentation.Value &&
                (actions == null || actions.ActiveKind == PlayerVisualAction.None);
            var id = valid ? player.SelectedShopItem.Value.ToString() : "";
            if (id.Length == 0 || Array.IndexOf(ShopShelfAnchor.ItemIds, id) < 0)
            { Hide(); return; }
            if (shown != id || basket == null)
            {
                Clear(); basket = CreateBasket(transform, id);
                if (basket == null) return;
                shown = id; product = basket.transform.Find("SelectedProduct");
                foreach (var marker in basket.GetComponentsInChildren<Transform>())
                {
                    if (marker.name == "Grip_Left") left = marker;
                    if (marker.name == "Grip_Right") right = marker;
                }
            }
            if (hands == null) hands = GetComponent<PlayerPresentationView>();
            var frame = hands != null ? hands.ActionBodyFrame : null;
            if (frame == null || left == null || right == null) { Hide(); return; }
            var pose = HeldPose(frame);
            basket.SetActive(true); basket.transform.SetPositionAndRotation(pose.position, pose.rotation);
            hands.ApplyActionGrips(left.position, right.position, left.rotation, right.rotation);
        }
        private void Hide() { if (basket != null) basket.SetActive(false); }
        private void Clear()
        {
            if (basket != null)
            {
                basket.SetActive(false);
                if (Application.isPlaying) Destroy(basket); else DestroyImmediate(basket);
            }
            basket = null; product = left = right = null; shown = "";
        }
        private void OnDisable() => Hide();
        private void OnDestroy() => Clear();
    }
}
