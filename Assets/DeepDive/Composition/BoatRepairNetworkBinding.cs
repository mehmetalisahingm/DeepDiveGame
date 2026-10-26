using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Trip;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // Mechanical prototype at the existing berth. No scene asset, movement or trip authority is replaced.
    // P4.8-B (#188): when World authored the beach cradle (RepairAnchorIds, DiveTestAreaRepairCradleSetup) the
    // worksite lies there instead, on dry Beach_Platform with a low solid hull, and the crew stands on the
    // repair-work anchors. Without those anchors it stays at the berth exactly as before.
    public sealed class BoatRepairNetworkBinding : MonoBehaviour
    {
        private NetworkManager manager;
        private SessionNetworkAdapter adapter;
        private EconomyManager economy;
        private BoatRepairWorkSession work;
        private readonly HashSet<PlayerId> workers = new HashSet<PlayerId>();
        private readonly List<PlayerId> released = new List<PlayerId>();
        private GameObject site;
        private readonly GameObject[] boards = new GameObject[3], racks = new GameObject[3];
        private readonly List<Material> materials = new List<Material>();
        private readonly List<GameObject> nails = new List<GameObject>();
        private readonly List<TextMesh> labels = new List<TextMesh>();
        private BoatTripPlayerSync trip;
        public static BoatRepairNetworkBinding Instance { get; private set; }
        public static Vector3 SlotLocal(int i) => new Vector3((i - 1) * .65f, .75f, -2.15f);
        public static Vector3 RackLocal(int i) => new Vector3(-.65f, .65f, -4.1f - i * .8f);
        public static Vector3 StandLocal(int i) => new Vector3((i - 1) * .65f, 0, i == 1 ? -4.2f : -3.8f);
        // Low enough to stay under the slot triggers (y .35-1.15) so the E/hammer ray still reaches them
        // (PickupRaycastRules treats solid geometry as an occluder), high enough not to be stepped over (stepOffset .3).
        public const float CradleHullHeight = .4f;
        // A leg counts as blocked when it passes within HitPad of the hull (capsule radius .35 + .05); a detour
        // then runs RoutePad off the rim: past a crew member on a south-side work point (.45 m off the rim) plus
        // two padded capsules (.86 m each side of contact).
        private const float HitPad = .4f;
        public const float RoutePad = 1.4f;
        public Vector3 SlotPosition(int i) => site.transform.TransformPoint(SlotLocal(i));
        public Vector3 RackPosition(int i) => drop != null
            ? drop.TransformPoint(new Vector3((i - 1) * .8f, .65f, 0f))
            : site.transform.TransformPoint(RackLocal(i));
        public Vector3 StandPosition(int i) => OnCradle && i >= 0 && i < workAnchors.Length && workAnchors[i] != null
            ? workAnchors[i].position : site.transform.TransformPoint(StandLocal(i));
        public bool OnCradle => cradle != null;
        // The hull collider is only there while the broken boat is shown (site active on the cradle).
        public bool Blocking => OnCradle && site != null && site.activeSelf;
        private Transform cradle, drop;
        private GameObject trestle;
        private readonly Transform[] workAnchors = new Transform[RepairAnchorIds.WorkCount];
        private float anchorRetryAt;
        private BoxCollider hull;
        private float nailRootZ=-.075f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
        }
        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<BoatRepairNetworkBinding>() == null)
                session.gameObject.AddComponent<BoatRepairNetworkBinding>();
        }
        private void Awake()
        { Instance = this; manager = GetComponent<NetworkManager>(); adapter = GetComponent<SessionNetworkAdapter>(); }
        private bool Host => manager != null && manager.IsListening && manager.IsServer && economy != null;
        private bool Has(int index, bool done) => (done ? economy.BoatRepair.CompletedPartIds : economy.UnusedBoatParts).Contains(BoatRepairParts.All[index]);
        private void Update()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            var local = manager != null ? manager.LocalClient?.PlayerObject : null;
            if (local != null && trip == null) trip = local.GetComponent<BoatTripPlayerSync>();
            var inWorld = SceneManager.GetSceneByName("DiveTestArea").isLoaded && trip != null && trip.IsSpawned;
            if (inWorld)
            {
                if (site == null) BuildSite();
                ResolveAnchors();
                if (OnCradle) site.transform.SetPositionAndRotation(cradle.position, cradle.rotation);
                else site.transform.SetPositionAndRotation(trip.BoatWorldPosition.Value, Quaternion.Euler(0, trip.BoatYaw.Value, 0));
                hull.enabled = OnCradle;
                var progress = local.GetComponent<EconomyPlayerSync>();
                site.SetActive(!trip.BoatVisible.Value && progress != null && progress.BoatPartsMask.Value != 7);
                if (trestle != null) trestle.SetActive(site.activeSelf);
            }
            else
            {
                if (site != null) site.SetActive(false);
                if (trestle != null) trestle.SetActive(false);
            }
            if (Host)
            {
                if (work == null) work = new BoatRepairWorkSession(i => Has(i, false), i => Has(i, true),
                    (p, i, r) => BoatPartClaim.TryClaimFound(p, BoatRepairParts.All[i], r),
                    (p, i, r) => economy.TryFinishBoatPart(p, BoatRepairParts.All[i], r));
                BoatRepairActions.Bind(Handle);
                released.Clear();
                foreach (var id in workers)
                {
                    var player = Player(id);
                    if (!inWorld || player == null || player.Passive.Value || player.Seated.Value || DayLock.IsLocked ||
                        (work.TryGet(id, out var active) && active.Placed && Vector3.Distance(player.RecordingEyePosition, SlotPosition(active.PartIndex)) > 3f))
                    { work.Release(id); released.Add(id); }
                }
                foreach (var id in released) workers.Remove(id);
                foreach (var client in manager.ConnectedClientsList)
                {
                    var player = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkPlayer>() : null;
                    if (player == null) continue;
                    var has = work.TryGet(new PlayerId(client.ClientId), out var state);
                    player.PublishRepairWork(has ? state.PartIndex : -1, has && state.Placed, has ? state.Hits : 0);
                }
            }
            else { BoatRepairActions.Unbind(Handle); work = null; workers.Clear(); }
            if (inWorld) RefreshSite(local.GetComponent<EconomyPlayerSync>());
        }
        // Scene lookups are not free, so a missing cradle is retried once a second rather than every frame.
        private void ResolveAnchors()
        {
            if (cradle != null || Time.unscaledTime < anchorRetryAt) return;
            anchorRetryAt = Time.unscaledTime + 1f;
            if (!WorldAnchors.TryFind(RepairAnchorIds.Cradle, out var found)) return;
            cradle = found.transform;
            drop = WorldAnchors.TryFind(RepairAnchorIds.Drop, out var safeDrop) ? safeDrop.transform : null;
            if (trestle == null) trestle = BoatRepairSetArt.AttachTrestle(drop);
            for (var i = 0; i < workAnchors.Length; i++)
                workAnchors[i] = WorldAnchors.TryFind(RepairAnchorIds.Work(i), out var work) ? work.transform : null;
        }

        // A dry walk from `from` to `to` that keeps RoutePad off the cradle hull: straight when the line is clear,
        // otherwise along a detour line RoutePad off the hull's south (town) side, the side the beach has room on:
        // onto that line level with `from`, along it to level with `to` (clamped to the hull's ends), then `to`.
        // Never back toward a corner already passed, so a caller can recompute it every frame. Without a blocking
        // hull (berth, or the boat already repaired): just `to`. Starts on the north (water) side are not handled:
        // the dry strip there is narrower than a diver.
        public List<Vector3> RouteAround(Vector3 from, Vector3 to)
        {
            var route = new List<Vector3>();
            if (Blocking && BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size))
            {
                var hitW = size.Width * .5f + HitPad;
                var hitL = size.Length * .5f + HitPad;
                var a = site.transform.InverseTransformPoint(from);
                var b = site.transform.InverseTransformPoint(to);
                if (Blocked(a, b, hitW, hitL))
                {
                    // Local +x is the hull's south side at the S1 yaw (90); pick it from the cradle frame, not the world.
                    var side = Vector3.Dot(site.transform.right, Vector3.back) >= 0f ? 1f : -1f;
                    var line = side * (size.Width * .5f + RoutePad);
                    var end = size.Length * .5f + RoutePad;
                    route.Add(Flat(site.transform.TransformPoint(new Vector3(line, 0f, Mathf.Clamp(a.z, -end, end))), to.y));
                    route.Add(Flat(site.transform.TransformPoint(new Vector3(line, 0f, Mathf.Clamp(b.z, -end, end))), to.y));
                }
            }
            route.Add(to);
            return route;
        }
        private static bool Blocked(Vector3 a, Vector3 b, float halfW, float halfL)
        {
            for (var t = 0f; t <= 1f; t += .02f)
            {
                var q = Vector3.Lerp(a, b, t);
                if (Mathf.Abs(q.x) < halfW && Mathf.Abs(q.z) < halfL) return true;
            }
            return false;
        }
        // The next RouteAround waypoint more than .3 m (flat) from `from`, for callers that steer every frame.
        public Vector3 NextAround(Vector3 from, Vector3 to)
        {
            foreach (var w in RouteAround(from, to))
                if (new Vector2(w.x - from.x, w.z - from.z).magnitude > .3f) return w;
            return to;
        }
        private static Vector3 Flat(Vector3 p, float y) { p.y = y; return p; }
        private NetworkPlayer Player(PlayerId id) => manager.ConnectedClients.TryGetValue(id.Value, out var client) && client.PlayerObject != null
            ? client.PlayerObject.GetComponent<NetworkPlayer>() : null;
        private TransactionResult Handle(PlayerId id, BoatRepairCommand command, int index, ulong request)
        {
            var p = Host ? Player(id) : null;
            if (p == null || !p.IsSpawned || site == null || !site.activeSelf || p.Passive.Value || p.Seated.Value ||
                p.RecordingPresentation.Value || DayLock.IsLocked || adapter.Session.State.Phase == SessionPhase.Lobby)
                return TransactionResult.Reject(request, "InvalidState", 0);
            if (command != BoatRepairCommand.Cancel && (index < 0 || index > 2)) return TransactionResult.Reject(request, "InvalidTarget", 0);
            if ((command == BoatRepairCommand.Place || command == BoatRepairCommand.Hammer) &&
                (p.Swimming.Value || Vector3.Distance(p.RecordingEyePosition, SlotPosition(index)) > 2.65f))
                return TransactionResult.Reject(request, "OutOfRange", 0);
            var result = work.Execute(id, command, index, request, Time.unscaledTimeAsDouble);
            if (work.TryGet(id, out var state)) { workers.Add(id); p.PublishRepairWork(state.PartIndex, state.Placed, state.Hits); }
            else { workers.Remove(id); p.PublishRepairWork(-1, false, 0); }
            return result;
        }
        private void BuildSite()
        {
            labels.Clear(); nails.Clear();
            foreach(var old in materials) if(old!=null)Destroy(old);
            materials.Clear();
            site = new GameObject("StarterBoatRepairWorksite");
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var hullSize);
            hull = site.AddComponent<BoxCollider>();
            hull.center = new Vector3(0f, CradleHullHeight * .5f, 0f);
            hull.size = new Vector3(hullSize.Width, CradleHullHeight, hullSize.Length);
            hull.enabled = false;
            var wood = MakeMaterial(new Color(.28f, .14f, .065f));
            var fresh = MakeMaterial(new Color(.66f, .43f, .21f));
            var iron = MakeMaterial(new Color(.13f, .15f, .17f));
            var repairArt=Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            var surface=repairArt!=null&&repairArt.RepairPlacedPlankPrefab!=null
                ?RepairPropPlacement.Marker(repairArt.RepairPlacedPlankPrefab.transform,"FastenerSurface"):null;
            var contact=repairArt!=null&&repairArt.RepairNailPrefab!=null
                ?RepairPropPlacement.Marker(repairArt.RepairNailPrefab.transform,"ContactPoint"):null;
            if(surface!=null&&contact!=null)
                nailRootZ=repairArt.RepairPlacedPlankPrefab.transform.InverseTransformPoint(surface.position).z-
                    repairArt.RepairNailPrefab.transform.InverseTransformPoint(contact.position).z;
            // Authored broken hull (#189); the primitive stand-in below only shows when that art is missing.
            if (!BoatRepairSetArt.AttachDamagedHull(site.transform))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("CrackedGunwale", new Vector3(side * 1.02f, .55f, .2f), new Vector3(.14f, .28f, 3.8f), wood);
                    Box("BowSide", new Vector3(side * .55f, .5f, 1.85f), new Vector3(.13f, .4f, 1.25f), wood).transform.localRotation = Quaternion.Euler(0, side * -47, 0);
                }
                for (int i = 0; i < 5; i++) Box("OldFloor", new Vector3((i-2)*.36f, .15f, .1f), new Vector3(.3f,.12f,3.4f-i%2*.4f), wood);
                Box("OldSeat", new Vector3(0,.65f,.65f),new Vector3(1.9f,.13f,.4f),wood);
            }
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject("RepairSlot-" + i); slot.transform.SetParent(site.transform,false);
                slot.transform.localPosition = new Vector3((i-1)*.65f,.75f,-2.15f);
                slot.AddComponent<BoatRepairSlotTarget>().Index = i;
                var trigger = slot.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(.59f,.8f,.15f);
                var label = new GameObject("SlotLabel-" + i); label.transform.SetParent(site.transform,false);
                label.transform.localPosition=slot.transform.localPosition+Vector3.up*.65f;
                var text=label.AddComponent<TextMesh>();text.text=BoatRepairGuide.PartName(BoatRepairParts.All[i]);
                text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.012f;text.color=new Color(1,.88f,.55f);labels.Add(text);
                boards[i] = BoatRepairSetArt.Part(i,site.transform);
                if (boards[i] == null)
                    boards[i] = repairArt!=null && repairArt.RepairPlacedPlankPrefab!=null
                        ? Instantiate(repairArt.RepairPlacedPlankPrefab,site.transform,false)
                        : Box("RepairPlank-"+i,slot.transform.localPosition,new Vector3(.58f,.7f,.1f),fresh);
                boards[i].transform.localPosition=slot.transform.localPosition;
                var tips=new Transform[3];
                for(int n=0;n<3;n++)
                {
                    var nail=repairArt!=null && repairArt.RepairNailPrefab!=null
                        ? Instantiate(repairArt.RepairNailPrefab,site.transform,false)
                        : Box("Nail-"+i+"-"+n,Vector3.zero,new Vector3(.035f,.035f,.05f),iron);
                    nail.name="Nail-"+i+"-"+n;
                    nail.transform.localPosition=slot.transform.localPosition+new Vector3((n-1)*.15f,.23f,nailRootZ);
                    nails.Add(nail);tips[n]=nail.transform;
                }
                slot.AddComponent<RepairWorkContactTarget>().Bind(i,tips);
                bool authoredRack=repairArt!=null && repairArt.RepairPlankPrefab!=null;
                racks[i]=authoredRack?Instantiate(repairArt.RepairPlankPrefab,site.transform,false):
                    Box("RecoveredMaterial-"+i,new Vector3(-.65f,.65f,-4.1f-i*.8f),new Vector3(.6f,.14f,.45f),fresh);
                racks[i].name="RecoveredMaterial-"+i;
                racks[i].AddComponent<BoatRepairRackTarget>().Index=i;
                var pickup=racks[i].AddComponent<BoxCollider>(); pickup.isTrigger=true;
                if(authoredRack)pickup.size=new Vector3(.78f,.24f,.12f);
            }
        }
        private Material MakeMaterial(Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); m.color=color; materials.Add(m); return m; }
        private GameObject Box(string name,Vector3 at,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(site.transform,false);
            obj.transform.localPosition=at; obj.transform.localScale=size; Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
        }
        private void RefreshSite(EconomyPlayerSync progress)
        {
            if(progress==null)return;
            var players=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            var local=progress.GetComponent<NetworkPlayer>();
            var camera=local!=null?local.GetComponentInChildren<Camera>(true):null;
            var selected=local!=null?local.RepairPartIndex.Value:-1;
            if(selected<0 && camera!=null)
            {
                var best=.9f;
                for(int i=0;i<3;i++)
                {
                    var direction=SlotPosition(i)-camera.transform.position;
                    var alignment=Vector3.Dot(direction.normalized,camera.transform.forward);
                    if(direction.magnitude<4f && alignment>best && (progress.BoatPartsMask.Value&(1<<i))==0)
                    {best=alignment;selected=i;}
                }
            }
            for(int i=0;i<3;i++)
            {
                bool reserved=false,placed=false; int hits=0;
                foreach(var player in players)
                    if(player.IsSpawned && player.RepairPartIndex.Value==i){reserved=true;placed=player.RepairPlaced.Value;hits=player.RepairHits.Value;break;}
                bool done=(progress.BoatPartsMask.Value&(1<<i))!=0;
                labels[i].text=BoatRepairGuide.PartName(BoatRepairParts.All[i])+(done?" - TAMAM":placed?$" - {hits}/3":" - E");
                labels[i].gameObject.SetActive(!done && i==selected && camera!=null && Vector3.Distance(camera.transform.position,SlotPosition(i))<4f);
                if(camera!=null)labels[i].transform.rotation=camera.transform.rotation;
                boards[i].SetActive(done||placed);
                for(int n=0;n<3;n++)
                {
                    var nail=nails[i*3+n];nail.SetActive(done||placed);
                    // All nails can be seen on placement; each host-accepted stroke
                    // drives one head flush. Rejected actions never advance the model.
                    var raised=!done&&n>=hits?.045f:0f;
                    nail.transform.localPosition=SlotLocal(i)+new Vector3((n-1)*.15f,.23f,nailRootZ-raised);
                }
                racks[i].SetActive(!done&&!reserved&&(progress.BoatMaterialsMask.Value&(1<<i))!=0);
                racks[i].transform.position = RackPosition(i);
            }
        }
        private void OnDestroy()
        { BoatRepairActions.Unbind(Handle); if(Instance==this)Instance=null; if(site!=null)Destroy(site); foreach(var m in materials)Destroy(m); }
    }
    public sealed class BoatRepairSlotTarget : MonoBehaviour, IBoatRepairSlot
    { public int Index; public int PartIndex => Index; }
    public sealed class BoatRepairRackTarget : MonoBehaviour, IBoatPartPickupTarget
    { public int Index; public string PartId => BoatRepairParts.All[Index]; }
}
