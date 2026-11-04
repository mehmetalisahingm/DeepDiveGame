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
                report.habitatCaught.Add($"{species} oxygen={local.Oxygen.Value:0} at={local.transform.position} {BandAt(local.transform.position.y)}");
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
                if (available) Act(local.SubmitPickupLocal);
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
            if (Vector3.Distance(target, eye) > HabitatSightRange * 0.7f || !ClearFishLine(fish))
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
                if (Vector3.Distance(target, eye) > HabitatSightRange || !ClearFishLine(fish)) continue;
                var view = camera.WorldToViewportPoint(target);
                if (view.z <= 0f || view.x < 0.1f || view.x > 0.9f || view.y < 0.1f || view.y > 0.9f) continue;
                if (!fish.GetComponentsInChildren<Renderer>().Any(r => r.enabled)) continue;
                report.habitatSeen.Add(species);
                report.habitatViews.Add($"{species} {fish.name} at={fish.transform.position} {BandAt(fish.transform.position.y)} " +
                                        $"from={eye} distance={Vector3.Distance(target, eye):0.0}");
                CaptureView(".habitat-" + species + ".png");
            }
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
