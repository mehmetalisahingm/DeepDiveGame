using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.Network
{
    // Presentation-only seam: the world owns the slot/nails, the player consumes
    // their positions. No network object, interaction collider or economic action.
    public sealed class RepairWorkContactTarget : MonoBehaviour
    {
        private static readonly Dictionary<int,RepairWorkContactTarget> targets=new Dictionary<int,RepairWorkContactTarget>();
        private Transform[] nails;
        private int part=-1;
        public void Bind(int index,Transform[] values)
        {part=index;nails=values;targets[index]=this;}
        public static RepairWorkContactTarget ForPart(int index)
        {return targets.TryGetValue(index,out var target)&&target!=null?target:null;}
        public Vector3 ContactAt(int index)
        {
            if(nails==null||nails.Length==0)return transform.position;
            var nail=nails[Mathf.Clamp(index,0,nails.Length-1)];
            if(nail==null)return transform.position;
            var contact=nail.Find("ContactPoint");return contact!=null?contact.position:nail.position;
        }
        private void OnDestroy()
        {if(targets.TryGetValue(part,out var current)&&current==this)targets.Remove(part);}
    }
}
