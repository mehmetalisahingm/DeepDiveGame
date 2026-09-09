using UnityEngine;

namespace DeepDive.Network
{
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [Range(0, 3)] public int Slot;

        public static bool TryGet(int slot, out Pose pose)
        {
            PlayerSpawnPoint found = null;
            foreach (var point in FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
            {
                if (point.Slot != slot) continue;
                if (found != null) { pose = default; return false; }
                found = point;
            }
            pose = found == null ? default : new Pose(found.transform.position, found.transform.rotation);
            return found != null;
        }
    }
}
