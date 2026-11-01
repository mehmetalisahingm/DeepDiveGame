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
            // Two groups placed side by side must not sway in step.
            phase = Mathf.Abs(transform.position.x * 1.7f + transform.position.z * 2.3f);
        }

        private void Update()
        {
            for (var i = 0; i < clumps.Length; i++)
            {
                if (!renderers[i].isVisible) continue;
                var lean = Lean(Time.time, PeriodSeconds, AmplitudeDegrees, phase + i * 1.9f);
                clumps[i].localRotation = rest[i] * Quaternion.Euler(lean.x, 0f, lean.y);
            }
        }
    }
}
