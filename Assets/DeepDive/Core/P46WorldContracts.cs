using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.Core.Contracts
{
    // P4.6 (#138) world seam: where a player stands, as a coarse zone, and the stable names of the
    // places other systems will look up. Byte values are frozen; a renumbering breaks saved and
    // mirrored state that may carry them later.
    public enum PlayerZone : byte
    {
        Unknown = 0,
        Home = 1,
        Town = 2,
        Coast = 3,
        Dock = 4,
        Sea = 5
    }

    // Ids of the authored world anchors. The ids are the contract; where each one stands is
    // World/Composition's business and may move without touching a consumer.
    public static class WorldAnchorIds
    {
        public const string HomeRoot = "home-root";
        // The #143 house has an open front and no door: this is the entry point on the facade line.
        public const string HomeDoor = "home-door";
        public const string HomeStorage = "home-storage";
        public const string HomePc = "home-pc";
        public const string TownSquare = "town-square";

        public const int HomeBedCount = 4;
        public const int HomeSpawnCount = 4;
        public const int TownServiceSlotCount = 4;

        private const string HomeBedPrefix = "home-bed-";
        private const string HomeSpawnPrefix = "home-spawn-";
        private const string TownServiceSlotPrefix = "town-service-slot-";

        private static readonly string[] all = BuildAll();

        // Out of range -> "" so a bad index can never alias a real anchor.
        public static string HomeBed(int index) => Indexed(HomeBedPrefix, index, HomeBedCount);

        public static string HomeSpawn(int index) => Indexed(HomeSpawnPrefix, index, HomeSpawnCount);

        public static string TownServiceSlot(int index) =>
            Indexed(TownServiceSlotPrefix, index, TownServiceSlotCount);

        // Fixed order: home-root, home-door, beds, storage, pc, spawns, town-square, service slots.
        public static IReadOnlyList<string> All => all;

        private static string Indexed(string prefix, int index, int count) =>
            index >= 0 && index < count ? prefix + index : "";

        private static string[] BuildAll()
        {
            var ids = new List<string> { HomeRoot, HomeDoor };
            for (var i = 0; i < HomeBedCount; i++) ids.Add(HomeBed(i));
            ids.Add(HomeStorage);
            ids.Add(HomePc);
            for (var i = 0; i < HomeSpawnCount; i++) ids.Add(HomeSpawn(i));
            ids.Add(TownSquare);
            for (var i = 0; i < TownServiceSlotCount; i++) ids.Add(TownServiceSlot(i));
            return ids.ToArray();
        }
    }

    // Read-only seam: feet position -> zone. Pure query; no per-player state, moves nobody,
    // and is not a water authority (Sea comes from WaterField's own rules).
    public interface IPlayerZoneSource
    {
        PlayerZone ResolveZone(Vector3 feetPosition);
    }
}
