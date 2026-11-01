using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeepDive.EditorTools
{
    // Sample prefabs assembled from the MegaKit modules (2 m wall module, shared module-base origin). Not placed in any scene.
    public static class MegaKitSamples
    {
        private const string Models = P47MegaKitSetup.Root + "/Models/";

        // Door leaves are modelled around their hinge (leaf centre is -0.51 m from the origin); shifting by this centres the leaf in the wall opening.
        private const float DoorHinge = 0.51f;

        private static GameObject Part(Transform parent, string model, Vector3 position, float yaw = 0f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = model;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation;   // keep the FBX root axis fix-up
            return instance;
        }

        private static void Save(GameObject root, string folder, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        public static void Build(string folder)
        {
            Save(HomeFacade(), folder, "HomeFacade_Sample");
            Save(ShopFront(), folder, "ShopFront_Sample");
        }

        // Footprint 6 m wide x 4 m deep, origin at the ground centre of the front wall; the front faces -Z, the body extends to +Z.
        public static GameObject HomeFacade()
        {
            var root = new GameObject("HomeFacade_Sample").transform;
            Part(root, "Wall_Plaster_Window_Wide_Round", new Vector3(-2f, 0f, 0f));
            Part(root, "Wall_Plaster_Door_Round", new Vector3(0f, 0f, 0f));
            Part(root, "Wall_Plaster_Window_Wide_Round", new Vector3(2f, 0f, 0f));
            Part(root, "Door_1_Round", new Vector3(DoorHinge, 0f, 0f));
            Part(root, "Window_Wide_Round1", new Vector3(-2f, 0f, 0f));
            Part(root, "Window_Wide_Round1", new Vector3(2f, 0f, 0f));
            for (var i = 0; i < 2; i++)
            {
                Part(root, "Wall_Plaster_Straight", new Vector3(-3f, 0f, 1f + 2f * i), 90f);
                Part(root, "Wall_Plaster_Straight", new Vector3(3f, 0f, 1f + 2f * i), -90f);
            }
            for (var i = -1; i <= 1; i++) Part(root, "Wall_Plaster_Straight", new Vector3(2f * i, 0f, 4f), 180f);
            foreach (var x in new[] { -3f, 3f }) foreach (var z in new[] { -0.1f, 4.1f }) Part(root, "Corner_Exterior_Wood", new Vector3(x, 0f, z));
            Part(root, "Roof_RoundTiles_6x4", new Vector3(0f, 3.12f, -0.2f));
            Part(root, "Roof_Front_Brick6", new Vector3(0f, 3.12f, 0f));
            Part(root, "Roof_Front_Brick6", new Vector3(0f, 3.12f, 4f), 180f);
            Part(root, "Stairs_Exterior_Straight", new Vector3(0f, 0f, -1.1f), 180f);
            return root.gameObject;
        }

        private static GameObject Block(Transform parent, string name, string materialPath, Vector3 position, Vector3 scale, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(P47MegaKitSetup.Root + "/Materials/" + materialPath + ".mat");
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // Equipment shop front: 4 m wide, origin at ground centre of the wall; customer stands at ServicePoint (1.7 m in front, -Z).
        public static GameObject ShopFront()
        {
            var root = new GameObject("ShopFront_Sample").transform;
            Part(root, "Wall_UnevenBrick_Window_Wide_Flat", new Vector3(-1f, 0f, 0f));
            Part(root, "Wall_Plaster_Door_Flat", new Vector3(1f, 0f, 0f));
            Part(root, "Door_1_Flat", new Vector3(1f + DoorHinge, 0f, 0f));
            Part(root, "Window_Wide_Flat1", new Vector3(-1f, 0f, 0f));
            foreach (var x in new[] { -2f, 2f }) Part(root, "Corner_Exterior_Wood", new Vector3(x, 0f, -0.1f));
            Part(root, "Roof_Wooden_2x1", new Vector3(-1f, 3.12f, 0f), 180f);
            Part(root, "Roof_Wooden_2x1", new Vector3(1f, 3.12f, 0f), 180f);
            Block(root, "Counter", "MI_WoodTrim", new Vector3(-1f, 0.5f, -0.75f), new Vector3(1.8f, 1f, 0.6f), true);
            // Free-standing sign in front of the awning (the wall top is under the roof): two posts, a board and the blue accent strip.
            Block(root, "SignPostL", "MI_WoodTrim", new Vector3(-2.1f, 1.1f, -1.7f), new Vector3(0.12f, 2.2f, 0.12f), false);
            Block(root, "SignPostR", "MI_WoodTrim", new Vector3(0.1f, 1.1f, -1.7f), new Vector3(0.12f, 2.2f, 0.12f), false);
            Block(root, "SignBoard", "MI_WoodTrim", new Vector3(-1f, 2.0f, -1.7f), new Vector3(2.4f, 0.5f, 0.08f), false);
            Block(root, "SignAccent", "Shop_Accent_Equipment", new Vector3(-1f, 2.0f, -1.76f), new Vector3(2.1f, 0.32f, 0.04f), false);
            Part(root, "Prop_Crate", new Vector3(2.9f, 0f, -0.9f));
            Part(root, "Prop_Crate", new Vector3(2.9f, 1.06f, -0.9f), 20f);
            new GameObject("ServicePoint").transform.SetParent(root, false);
            root.Find("ServicePoint").localPosition = new Vector3(-1f, 0f, -1.7f);
            return root.gameObject;
        }
    }
}
