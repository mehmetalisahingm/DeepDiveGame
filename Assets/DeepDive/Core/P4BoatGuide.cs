using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // #156: what the boat-repair quest tells the player. A crew reached 2/3 and could not tell which part was missing or where to look, so the
    // quest line now names the missing parts and points at them. Pure text/geometry, no state: which parts are done comes from the host's mirror
    // (the bit mask EconomyPlayerSync publishes), where a part lies comes from Utku's scene anchors, and the quest rules are unchanged.
    public static class BoatRepairGuide
    {
        // Unaccented on purpose, like every other HUD string (the built-in IMGUI font has no Turkish letters).
        public static string PartName(string partId) => partId switch
        {
            BoatRepairParts.Hull => "Govde tahtasi",
            BoatRepairParts.Engine => "Oturak destegi",
            BoatRepairParts.FuelTank => "Kurek baglantisi",
            _ => string.Empty
        };

        // The mask bit of a part is its index in BoatRepairParts.All (the same rule TownServiceBinding publishes and BoatPartPresentation reads).
        public static int Bit(string partId)
        {
            for (var i = 0; i < BoatRepairParts.All.Count; i++)
                if (string.Equals(BoatRepairParts.All[i], partId, StringComparison.Ordinal)) return 1 << i;
            return 0;
        }

        public static IReadOnlyList<string> Missing(int doneMask)
        {
            var missing = new List<string>(BoatRepairParts.All.Count);
            foreach (var part in BoatRepairParts.All)
                if ((doneMask & Bit(part)) == 0) missing.Add(part);
            return missing;
        }

        // 8-point compass in the scene's own frame: +z is north (toward the water, where the dive starts), +x east.
        public static string Compass(float dx, float dz)
        {
            if (dx * dx + dz * dz < 0.0001f) return "burada";
            var degrees = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);   // 0 = north, clockwise
            if (degrees < 0f) degrees += 360f;
            var index = (int)Math.Round(degrees / 45f) % 8;
            return Points[index];
        }

        private static readonly string[] Points = { "kuzey", "kuzeydogu", "dogu", "guneydogu", "guney", "guneybati", "bati", "kuzeybati" };

        // "Motor: bati, 12 m". Distance is flat metres, rounded; a part under the player's feet reads "burada".
        public static string Line(string partId, float fromX, float fromZ, float partX, float partZ)
        {
            var dx = partX - fromX; var dz = partZ - fromZ;
            var distance = (float)Math.Sqrt(dx * dx + dz * dz);
            var where = Compass(dx, dz);
            return where == "burada" ? $"{PartName(partId)}: burada" : $"{PartName(partId)}: {where}, {(int)Math.Round(distance)} m";
        }

        // Repair parts can be collected on the physical coast before/after a dive as well.
        // The host still validates range, aim, player activity and the day-closing lock.
        public const string OutsideDiveNote = "E: malzemeyi al; iskelede yuvaya E, 3 kez sol tik";
        public const string DiveNote = "E: malzemeyi al; iskelede yuvaya E, 3 kez sol tik";
    }
}
