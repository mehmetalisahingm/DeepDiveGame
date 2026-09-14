using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // What the framing rules need to know about a diver's camera, and nothing more - the same
    // narrowing IWaterBounds does for the swim rules. An interface so the maths can be tested
    // against a plain pose instead of a live camera, and so World never has to reach into
    // Mehmet's player or camera classes.
    //
    // Mehmet owns the camera; the pose has to be read every host tick while a take is open,
    // which is why this is a live view rather than a value copied at start.
    public interface IRecorderView
    {
        // False while the diver cannot film at all (no camera equipped, dead, cutscene). A take
        // that loses its view banks no time; it does not fail retroactively.
        bool IsActive { get; }

        Vector3 EyePosition { get; }
        Vector3 EyeForward { get; }
        float VerticalFieldOfViewDegrees { get; }
    }

    // Per-player binding point. DiveContext and CatchClaim each bind a single authority, but a
    // camera belongs to one diver, so this one is keyed by player.
    //
    // Composition owns the wiring: it is the only assembly that references both DeepDive.World
    // and DeepDive.Network, so it is the only place that can adapt Mehmet's camera to this
    // interface. An unbound player simply cannot record, which is the safe direction.
    public static class RecorderViews
    {
        private static readonly Dictionary<ulong, IRecorderView> Views = new Dictionary<ulong, IRecorderView>();

        public static int Count => Views.Count;

        public static void Bind(PlayerId player, IRecorderView view)
        {
            if (view == null)
            {
                Unbind(player);
                return;
            }
            Views[player.Value] = view;
        }

        public static void Unbind(PlayerId player) => Views.Remove(player.Value);

        // Composition must call this when the session ends: a view belonging to a disconnected
        // diver would otherwise keep a stale camera alive across dives.
        public static void UnbindAll() => Views.Clear();

        public static bool IsBound(PlayerId player) => Views.ContainsKey(player.Value);

        // True only for a bound view that is currently able to film, so callers get one check
        // instead of a null test followed by an IsActive test.
        public static bool TryGetActive(PlayerId player, out IRecorderView view)
        {
            if (!Views.TryGetValue(player.Value, out view) || view == null || !view.IsActive)
            {
                view = null;
                return false;
            }
            return true;
        }
    }
}
