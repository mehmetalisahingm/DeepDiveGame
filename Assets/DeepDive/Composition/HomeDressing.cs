using DeepDive.Economy;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Media;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #259: the shared house's art. The home rig (HomeSceneSetup) keeps the physical targets (beds, storage, PC) and the town data keeps the walls;
    // this places the baked, collider-free dressing prefab (Resources/CityKit/SharedHouse) over them: plaster and tile outside, a pitched roof,
    // a porch with a sign and lanterns, a wood floor, wainscot, windows with curtains, pendant lamps, rug, a pennant/gear wall, a lifebuoy, the PC
    // corner with its monitor and one coloured blanket per bed. Visual only: it never adds a collider, a NetworkObject or an interaction, so no
    // route, bed, storage or PC behaviour can change. Idempotent, and the same on host and guest (it is scene art, not state).
    public static class HomeDressing
    {
        public const string Resource = "CityKit/SharedHouse";
        public const string RootName = "SharedHouseDressing";
        public const string MissingCode = "P259_HOME_DRESSING_MISSING";

        public static GameObject Place(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            var existing = HomeSceneSetup.FindRoot(scene, RootName);
            if (existing != null)
            {
                if (existing.GetComponent<HomeChannelBoardDisplay>() == null)
                    existing.AddComponent<HomeChannelBoardDisplay>();
                return existing;
            }
            var prefab = Resources.Load<GameObject>(Resource);
            if (prefab == null)
            {
                Debug.LogError(MissingCode + " " + Resource);
                return null;
            }
            var house = Object.Instantiate(prefab);
            house.name = RootName;
            SceneManager.MoveGameObjectToScene(house, scene);
            house.transform.SetPositionAndRotation(ContinuousTownLayout.HomeOrigin, Quaternion.identity);
            house.AddComponent<HomeChannelBoardDisplay>();
            return house;
        }
    }

    // #259/#255: the physical PC corner shows host-verified followers and the next real milestone; it should reflect the actual shared
    // channel rather than showing a forever-static decorative "KANAL" sign.
    // Strictly visual: polls the existing host-approved mirror, creates no new
    // NetworkObject, RPC, income, subscriber count, save field or collision.
    [DisallowMultipleComponent]
    public sealed class HomeChannelBoardDisplay : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private TextMesh boardText;
        private float nextRefresh;

        public static string Label(bool hasMirror, int followers)
        {
            if (!hasMirror) return "KANAL"; // no progress before an actual host mirror is available
            var progress = ChannelGoal.Of(followers);
            return "KANAL\n" + ChannelGoal.Format(progress.Followers) + " TAKIPCI\n" +
                (progress.Reached ? "HEDEF TAMAM" : "SONRAKI: " + ChannelGoal.Format(progress.NextMilestone));
        }

        // The physical wall panel has a 1.28 m face. The long 10M milestone
        // must fit without spilling outside the board when the text changes.
        public static float CharacterSize(string label)
        {
            var longest = 0;
            var line = 0;
            foreach (var c in label ?? "")
            {
                if (c == '\n') { longest = Mathf.Max(longest, line); line = 0; }
                else line++;
            }
            longest = Mathf.Max(longest, line);
            return Mathf.Min(0.06f, 0.98f / Mathf.Max(1, longest));
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            if (boardText == null)
            {
                var board = transform.Find("PcCorner/ChannelBoard");
                if (board == null) return;
                boardText = board.GetComponentInChildren<TextMesh>(true);
                if (boardText == null) return;
            }
            var mirror = MediaNetworkBinding.Mirrored;
            var label = Label(MediaNetworkBinding.HasMirror && mirror != null,
                mirror != null ? mirror.Followers : 0);
            if (boardText.text != label)
            {
                boardText.text = label;
                boardText.characterSize = CharacterSize(label);
            }
        }
    }

    // The shared night (0 day .. 1 night) from the already replicated day, the same source the town lanterns and the coast sky read.
    public static class SharedDaylight
    {
        private static EconomyPlayerSync source;

        public static float Night()
        {
            if (source == null || !source.IsSpawned)
            {
                source = null;
                foreach (var sync in Object.FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None))
                {
                    if (!sync.IsSpawned) continue;
                    if (source == null) source = sync;
                    if (sync.IsOwner) { source = sync; break; }
                }
            }
            if (source == null) return 0f;
            return DaylightModel.Evaluate(source.DayClockMinute.Value, (DayPhase)source.DayPhaseValue.Value, source.DayWeatherSeed.Value).NightFactor;
        }
    }
}
