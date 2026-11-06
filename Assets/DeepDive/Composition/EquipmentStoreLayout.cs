using System;
using UnityEngine;

namespace DeepDive.Composition
{
    // #249: the named points of the enterable equipment store prefab (Resources/CityKit/EquipmentStore). Data only, no behaviour and no
    // network object: it tells the owners of the other pieces where things are. Utku places and collides the building, Mehmet binds the
    // shelf selection, basket and till to these anchors; nothing here sells, charges or selects anything.
    public sealed class EquipmentStoreLayout : MonoBehaviour
    {
        [Serializable]
        public struct Display
        {
            public string EquipmentId;    // the catalog id the items on this display stand for (camera-basic, tube-1, ...)
            public Transform Anchor;      // the display's own transform; its children are the visible items
            public int Items;             // how many copies stand there
        }

        [Tooltip("Ground centre of the door opening, on the street side of the wall. Walk in along +Z.")]
        public Transform Entrance;
        [Tooltip("Floor line of the doorway. The floor is flush with the street: no step.")]
        public Transform Threshold;
        [Tooltip("Where a customer stands to pay (in front of the counter, facing the shopkeeper).")]
        public Transform CustomerPoint;
        [Tooltip("The till on the counter (separate payment point).")]
        public Transform Register;
        [Tooltip("Where the shopkeeper stands behind the counter.")]
        public Transform ShopkeeperPoint;
        [Tooltip("Outside the door, where a leaving player ends up.")]
        public Transform ExitPoint;
        public Display[] Displays = new Display[0];
        public float OpeningWidth;        // clear width of the door opening in metres
        public float OpeningHeight;       // clear height of the door opening in metres
    }
}
