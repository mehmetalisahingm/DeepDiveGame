using UnityEngine;

namespace DeepDive.Network
{
    // Separate model roots share the approved boat pose. These blockout meshes can be replaced
    // by final art without changing fleet ownership, seat placement or network state.
    public sealed class BoatHullPresentation : MonoBehaviour
    {
        private readonly GameObject[] models = new GameObject[3];
        public BoatHullKind PresentedHull { get; private set; }

        public void Initialize(Material material)
        {
            if (models[0] != null) return;
            for (var i = 0; i < models.Length; i++)
            {
                var kind = (BoatHullKind)i;
                var model = new GameObject(kind.ToString());
                model.transform.SetParent(transform, false);
                models[i] = model;
                BoatHullSeatRules.TryGetDimensions(kind, out var size);
                Part(model, material, "Deck", Vector3.zero, new Vector3(size.Width, 0.4f, size.Length));
                Part(model, material, "PortRail", new Vector3(-size.Width / 2, 0.3f, 0), new Vector3(0.16f, 0.6f, size.Length));
                Part(model, material, "StarboardRail", new Vector3(size.Width / 2, 0.3f, 0), new Vector3(0.16f, 0.6f, size.Length));
                for (var seat = 0; seat < 4; seat++)
                {
                    BoatHullSeatRules.TryResolveLocalOffset(kind, BoatSeatLayoutRules.SeatIds[seat], out var offset);
                    Part(model, material, "Seat_" + seat, offset - Vector3.up * 0.1f, new Vector3(0.7f, 0.2f, 0.6f));
                }
                if (kind == BoatHullKind.Motorboat)
                {
                    Part(model, material, "OutboardMotor", new Vector3(0, 0.45f, -size.Length / 2), new Vector3(0.7f, 1.1f, 0.7f));
                    Part(model, material, "Console", new Vector3(0, 0.6f, 2.1f), new Vector3(1.2f, 1.2f, 0.6f));
                }
                if (kind == BoatHullKind.ResearchVessel)
                {
                    Part(model, material, "Cabin", new Vector3(0, 1, 3.1f), new Vector3(2.5f, 2, 2));
                    Part(model, material, "CabinRoof", new Vector3(0, 2.1f, 3.1f), new Vector3(3, 0.2f, 2.4f));
                    Part(model, material, "RadarMast", new Vector3(0, 2.8f, 3.1f), new Vector3(0.15f, 1.4f, 0.15f));
                    Part(model, material, "Radar", new Vector3(0, 3.5f, 3.1f), new Vector3(1.4f, 0.15f, 0.2f));
                }
            }
            ApplyHullPresentation(BoatHullKind.Rowboat);
        }

        public void ApplyHullPresentation(BoatHullKind kind)
        {
            var valid = BoatHullSeatRules.TryGetDimensions(kind, out _);
            for (var i = 0; i < models.Length; i++)
                if (models[i] != null) models[i].SetActive(valid && i == (int)kind);
            PresentedHull = kind;
        }

        private static void Part(GameObject root, Material material, string name, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Collider>().enabled = false;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
