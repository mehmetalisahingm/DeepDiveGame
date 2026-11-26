using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #219/#155 explicit development check (-HabitatHunt): the normal product day only ever hunts the nearest sea bass,
    // so nothing proved the other habitats work. Here the host really harpoons, collects and sells one of each of the
    // five species with owner input, and every other player swims to each habitat and sees each species from its own
    // camera. Host authority, oxygen, bag and sale are the real ones. Not a playtest of fun, reach or balance.
    public sealed partial class ProductDaySmokeDriver
    {
        private const float HabitatStageSeconds = 300f;
        private const float HabitatShotRange = 12f;
        private const float HabitatPickupReach = 2.45f;
        private const float HabitatSightRange = 14f;
        private const float HabitatSurfaceFeetY = 6.5f;
        // P411FiveSpeciesRoster's prices: three at 120, grouper 180, lanternfish 320.
        private const int HabitatExpectedSale = 860;

        private static readonly string[] HabitatOrder = { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };

        private bool HabitatHunt => Arg("-p219-habitat-hunt") == "1";
        // #310 (-FishCloseUps): each visiting diver comes inside the 5 m a fish lets it near before calling a species
        // seen, and frames it there; the host frames each catch from the pickup reach.
        private bool FishCloseUps => Arg("-p310-closeups") == "1";
        private const float CloseUpSightRange = 5f;
        private readonly HashSet<string> closeUpTaken = new HashSet<string>();
        private bool habitatStageExtended;
        private int habitatIndex;
        private float nextHabitatSample;

        // True when this player has finished the habitats and is back at the shore.
        private bool HabitatTick()
        {
            if (!habitatStageExtended)
            {
                habitatStageExtended = true;
                stageDeadline = Time.realtimeSinceStartup + HabitatStageSeconds;
            }
            SampleFishAgainstTheFloor();
            SampleFishBehaviour();
            NoteFishSizes();
            NoteFishInView();
            if (Host && habitatIndex < HabitatOrder.Length) { HuntNextSpecies(); return false; }
            if (!Host && report.habitatSeen.Count < HabitatOrder.Length) { VisitNextSpecies(); return false; }
            return SwimBackToShore();
        }

        private void HuntNextSpecies()
        {
            var species = HabitatOrder[habitatIndex];
            var inventory = adapter.GetComponent<InventoryManager>();
            if (inventory.Bags.TryGetValue(new PlayerId(local.OwnerClientId), out var bag) &&
                bag.Items.Any(item => item.SpeciesId == species))
            {
                var caught = bag.Items.First(item => item.SpeciesId == species);
                report.habitatCaught.Add($"{species} oxygen={local.Oxygen.Value:0} at={local.transform.position} {BandAt(local.transform.position.y)} grams={caught.WeightGrams}");
                habitatIndex++;
                aimAt = 0;
                return;
            }

            var eye = local.RecordingEyePosition;
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None)
                .Where(f => f.IsSpawned && f.Species != null && f.Species.SpeciesId == species &&
                            (!f.IsDead || f.GetComponent<CatchObject>().IsAvailable))
                .OrderByDescending(f => f.GetComponent<CatchObject>().IsAvailable)
                .ThenBy(f => (f.transform.position - eye).sqrMagnitude).FirstOrDefault();
            if (fish == null) { Stop(); return; }

            local.SetHeldEquipmentLocal(HeldEquipmentMode.Harpoon);
            var target = fish.GetComponent<Collider>().bounds.center;
            var distance = Vector3.Distance(target, eye);
            var available = fish.GetComponent<CatchObject>().IsAvailable;
            if (distance > (available ? 2.2f : HabitatShotRange) || !ClearFishLine(fish))
            {
                SwimTo(target - (eye - local.transform.position));
                // A current can carry the diver back and forth over the stop distance. That must not restart the
                // aim while the catch is still inside the 2.5 m pickup ray.
                if (!available || distance > HabitatPickupReach) aimAt = 0;
                return;
            }

            Aim(target);
            if (Settled())
            {
                if (available)
                {
                    if (FishCloseUps && closeUpTaken.Add(species))
                    {
                        report.habitatViews.Add($"closeup catch {species} {fish.name} distance={distance:0.0} size={fish.SizeScale:0.000}");
                        CaptureView(".closeup-catch-" + species + ".png");
                    }
                    Act(local.SubmitPickupLocal);
                }
                else Act(local.SubmitHarpoonLocal);
            }
        }

        // Every other player goes to each habitat itself: a host's view proves nothing about what a guest sees.
        private void VisitNextSpecies()
        {
            var species = HabitatOrder.First(id => !report.habitatSeen.Contains(id));
            var eye = local.RecordingEyePosition;
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None)
                .Where(f => f.IsSpawned && !f.IsDead && f.Species != null && f.Species.SpeciesId == species)
                .OrderBy(f => (f.transform.position - eye).sqrMagnitude).FirstOrDefault();
            if (fish == null) { Stop(); return; }
            var target = fish.GetComponent<Collider>().bounds.center;
            if (Vector3.Distance(target, eye) > SightRangeFor(species) * 0.7f || !ClearFishLine(fish))
                SwimTo(target - (eye - local.transform.position));
            else Aim(target);
        }

        private bool SwimBackToShore()
        {
            var stand = Host ? new Vector3(-7.6f, 5.5f, -5f) : new Vector3(1f + report.slot * 1.2f, 5.3f, -4f);
            if (Vector3.Distance(local.transform.position, stand) <= 0.5f) { Stop(); return true; }
            SwimTo(stand);
            return false;
        }

        // Long legs go up and along the surface, which costs no oxygen, then straight down to the target.
        private void SwimTo(Vector3 target)
        {
            var feet = local.transform.position;
            var flat = new Vector2(target.x - feet.x, target.z - feet.z).magnitude;
            target.y = Mathf.Max(target.y, SeabedProfile.FloorHeight(target.x, target.z) + 0.3f);
            if (flat <= 6f) { MoveToward(target); return; }
            if (feet.y < HabitatSurfaceFeetY - 1f) MoveToward(new Vector3(feet.x, HabitatSurfaceFeetY, feet.z));
            else MoveToward(new Vector3(target.x, HabitatSurfaceFeetY, target.z));
        }

        // #311: the grouper's reaction (back to its rock) only shows inside its 5 m reach, so a visiting diver must come
        // that close before it counts as seen; the host's samples then record the hiding.
        private float SightRangeFor(string species) =>
            !Host && (species == "grouper" || FishCloseUps) ? CloseUpSightRange : HabitatSightRange;

        // A species counts as seen when a live, rendered fish of it is inside this camera's view with a clear line.
        private void NoteFishInView()
        {
            var camera = local.GetComponentInChildren<Camera>(true);
            var eye = local.RecordingEyePosition;
            foreach (var fish in FindObjectsByType<FishActor>(FindObjectsSortMode.None))
            {
                if (!fish.IsSpawned || fish.IsDead || fish.Species == null) continue;
                var species = fish.Species.SpeciesId;
                if (report.habitatSeen.Contains(species)) continue;
                var target = fish.GetComponent<Collider>().bounds.center;
                if (Vector3.Distance(target, eye) > SightRangeFor(species) || !ClearFishLine(fish)) continue;
                var view = camera.WorldToViewportPoint(target);
                if (view.z <= 0f || view.x < 0.1f || view.x > 0.9f || view.y < 0.1f || view.y > 0.9f) continue;
                if (!fish.GetComponentsInChildren<Renderer>().Any(r => r.enabled)) continue;
                report.habitatSeen.Add(species);
                report.habitatViews.Add($"{species} {fish.name} at={fish.transform.position} {BandAt(fish.transform.position.y)} " +
                                        $"from={eye} distance={Vector3.Distance(target, eye):0.0} size={fish.SizeScale:0.000}");
                CaptureView((FishCloseUps && !Host ? ".closeup-live-" : ".habitat-") + species + ".png");
            }
        }

        // #310: once the dive's fish are spawned, what this process shows for each: the replicated size and the scale
        // actually on the transform. The host adds the weight it rolled, so the reports can be matched by fish name.
        private bool fishSizesNoted;
        private void NoteFishSizes()
        {
            if (fishSizesNoted) return;
            var fishes = FindObjectsByType<FishActor>(FindObjectsSortMode.None).Where(f => f.IsSpawned && !f.IsDead).ToArray();
            if (fishes.Length < P411FiveSpeciesRoster.Population) return;
            fishSizesNoted = true;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var fish in fishes.OrderBy(f => f.name))
                report.fishSizes.Add(string.Format(ci, "{0} {1} size={2:0.000} scaleX={3:0.000} lengthM={4:0.000}{5}", fish.name,
                    fish.Species?.SpeciesId, fish.SizeScale, fish.transform.localScale.x, VisibleLength(fish),
                    Host ? " grams=" + fish.WeightGrams : ""));
        }

        // #311: every 0.25 s, per species, what this process sees: swim speed, how far from home (host only, where the
        // home is known), how often running or hiding (host only), and for every process the spread of each species'
        // live fish (mean distance between them), so a school reads as a school on every screen.
        private sealed class BehaviourStats { public int Samples, Fleeing, Hiding, Spreads; public float Speed, HomeDistance, Spread; }
        private readonly Dictionary<string, BehaviourStats> behaviour = new Dictionary<string, BehaviourStats>();
        private readonly Dictionary<FishActor, Vector3> lastFishAt = new Dictionary<FishActor, Vector3>();
        private float nextBehaviourSample;

        private void SampleFishBehaviour()
        {
            if (Time.realtimeSinceStartup < nextBehaviourSample) return;
            const float every = 0.25f;
            nextBehaviourSample = Time.realtimeSinceStartup + every;
            var live = FindObjectsByType<FishActor>(FindObjectsSortMode.None).Where(f => f.IsSpawned && !f.IsDead && f.Species != null).ToArray();
            foreach (var group in live.GroupBy(f => f.Species.SpeciesId))
            {
                if (!behaviour.TryGetValue(group.Key, out var stats)) behaviour[group.Key] = stats = new BehaviourStats();
                foreach (var fish in group)
                {
                    var at = fish.transform.position;
                    if (lastFishAt.TryGetValue(fish, out var before))
                    {
                        stats.Samples++;
                        stats.Speed += Vector3.Distance(before, at) / every;
                        if (Host)
                        {
                            stats.HomeDistance += Vector3.Distance(fish.Home, at);
                            if (fish.IsFleeing) stats.Fleeing++;
                            if (fish.IsHiding) stats.Hiding++;
                        }
                    }
                    lastFishAt[fish] = at;
                }
                var members = group.Select(f => f.transform.position).ToArray();
                if (members.Length > 1)
                {
                    var sum = 0f; var pairs = 0;
                    for (var i = 0; i < members.Length; i++)
                        for (var j = i + 1; j < members.Length; j++) { sum += Vector3.Distance(members[i], members[j]); pairs++; }
                    stats.Spread += sum / pairs; stats.Spreads++;
                }
            }
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            report.fishBehaviour = behaviour.OrderBy(b => b.Key).Select(b => string.Format(ci,
                "{0} style={1} samples={2} speed={3:0.00} home={4:0.00} fleeing={5:0.000} hiding={6:0.000} spread={7:0.00}",
                b.Key, live.FirstOrDefault(f => f.Species.SpeciesId == b.Key)?.Style.ToString() ?? "?", b.Value.Samples,
                b.Value.Samples > 0 ? b.Value.Speed / b.Value.Samples : 0f,
                Host && b.Value.Samples > 0 ? b.Value.HomeDistance / b.Value.Samples : -1f,
                Host && b.Value.Samples > 0 ? (float)b.Value.Fleeing / b.Value.Samples : -1f,
                Host && b.Value.Samples > 0 ? (float)b.Value.Hiding / b.Value.Samples : -1f,
                b.Value.Spreads > 0 ? b.Value.Spread / b.Value.Spreads : -1f)).ToList();
        }

        // The longest side of the fish's own model in metres, as drawn: mesh bounds in model space times the scale the
        // renderer really has. Independent of which way the fish is swimming.
        private static float VisibleLength(FishActor fish)
        {
            var length = 0f;
            foreach (var renderer in fish.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                var size = renderer.localBounds.size;
                var scale = renderer.transform.lossyScale;
                length = Mathf.Max(length, Mathf.Max(size.x * Mathf.Abs(scale.x), size.y * Mathf.Abs(scale.y), size.z * Mathf.Abs(scale.z)));
            }
            return length;
        }

        // On the host the fish position is the authority itself, and #219's bound is what keeps it above the floor.
        private void SampleFishAgainstTheFloor()
        {
            if (!Host || Time.realtimeSinceStartup < nextHabitatSample) return;
            nextHabitatSample = Time.realtimeSinceStartup + 0.25f;
            foreach (var fish in FindObjectsByType<FishActor>(FindObjectsSortMode.None))
            {
                if (!fish.IsSpawned || fish.IsDead) continue;
                report.habitatFishSamples++;
                var at = fish.transform.position;
                if (at.y < SeabedProfile.FloorHeight(at.x, at.z)) report.habitatBelowFloor++;
            }
        }

        private static string BandAt(float y) =>
            DiveDepthBands.TryFind(DiveDepthBandSet.DefaultSurfaceY - y, out var band) ? band.Id : "above-water";

        private void FinishHabitatSale()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var saved = economy.ExportSaveData("read-only", "read-only");
            report.soldCount = saved.SoldCaptureIds.Count;
            report.habitatBalance = economy.SharedBalance;
            // Selling can also finish the day's fish order, which pays its own reward on top of the sale.
            foreach (var rewardId in saved.RewardIds)
                if (ContractCatalog.TryGet(rewardId.Substring(rewardId.LastIndexOf(':') + 1), out var order))
                {
                    report.habitatRewards += order.Reward;
                    report.habitatViews.Add($"reward {rewardId} {order.Reward}");
                }
            SetStep(15);
            finishAt = Time.realtimeSinceStartup + 2f;
        }

        private bool HabitatPassed() =>
            report.errors.Count == 0 && report.players == expected && report.habitatSeen.Count == HabitatOrder.Length &&
            (!Host || (report.habitatCaught.Count == HabitatOrder.Length && report.soldCount == HabitatOrder.Length &&
                       report.habitatBalance == HabitatExpectedSale + report.habitatRewards && report.habitatBelowFloor == 0 && report.habitatFishSamples > 0));
    }
}
