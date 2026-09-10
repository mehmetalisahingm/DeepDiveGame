using UnityEngine;

namespace DeepDive.World
{
    // What the swim rules need to know about the water: whether a point is inside it.
    // An interface so the motion maths can be tested against a plain box instead of a scene.
    public interface IWaterBounds
    {
        bool Contains(Vector3 point);
    }
}
