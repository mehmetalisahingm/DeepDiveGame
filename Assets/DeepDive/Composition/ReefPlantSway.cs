using UnityEngine;

namespace DeepDive.Composition
{
    // #217: the only motion in the reef kit. Soft cosmetic sway of seagrass clumps about their base; hard coral and rock never
    // move. No physics, no network, no authority: it reads the clock only, and only while a clump is actually on screen.
    public sealed class ReefPlantSway : MonoBehaviour
    {
        public float AmplitudeDegrees = 5f;
        public float PeriodSeconds = 3.4f;
        private Transform[] clumps;
        private Renderer[] renderers;
        private Quaternion[] rest;
        private float phase;
        private Vector3 phasePosition;

        // Habitat prefabs may be instantiated at the origin and positioned
        // after Awake. Recalculate only when placed/moved so distinct clumps
        // don't sway in lockstep in the actual world.
        public static float PhaseForPosition(Vector3 position) =>
            Mathf.Repeat(position.x * 1.7f + position.z * 2.3f, Mathf.PI * 2f);

        // Pure so a test can pin the bound: a clump never leans further than the amplitude on either axis.
        public static Vector2 Lean(float time, float period, float amplitude, float phase)
        {
            var w = 2f * Mathf.PI / Mathf.Max(.1f, period);
            return new Vector2(Mathf.Sin(time * w + phase), Mathf.Sin(time * w * .83f + phase * 1.7f + 1.1f)) * amplitude;
        }

        private void Awake()
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform) if (child.GetComponent<Renderer>() != null) list.Add(child);
            clumps = list.ToArray();
            renderers = new Renderer[clumps.Length]; rest = new Quaternion[clumps.Length];
            for (var i = 0; i < clumps.Length; i++) { renderers[i] = clumps[i].GetComponent<Renderer>(); rest[i] = clumps[i].localRotation; }
            phasePosition = transform.position;
            phase = PhaseForPosition(phasePosition);
        }

        private void Update()
        {
            if (transform.position != phasePosition)
            {
                phasePosition = transform.position;
                phase = PhaseForPosition(phasePosition);
            }
            for (var i = 0; i < clumps.Length; i++)
            {
                if (!renderers[i].isVisible) continue;
                var lean = Lean(Time.time, PeriodSeconds * (1f + i * 0.06f),
                    AmplitudeDegrees, phase + i * 1.9f);
                clumps[i].localRotation = rest[i] * Quaternion.Euler(lean.x, 0f, lean.y);
            }
        }
    }
}
