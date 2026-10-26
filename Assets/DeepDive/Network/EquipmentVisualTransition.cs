using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // Cosmetic state only. Targets must come from the accepted player snapshot.
    public sealed class EquipmentVisualTransition
    {
        public const float HalfDuration=.14f;
        public HeldEquipmentMode Displayed {get;private set;}
        public CameraTier Tier {get;private set;}
        public float Lowering=>Mathf.SmoothStep(0,1,lower);
        public bool IsSwitching=>lowering || lower>0;
        private HeldEquipmentMode target;
        private CameraTier targetTier;
        private bool initialized,lowering;
        private float lower;

        public void Tick(HeldEquipmentMode accepted,CameraTier tier,float seconds,bool immediate=false)
        {
            if(accepted!=HeldEquipmentMode.Camera)tier=CameraTier.Basic;
            if(!initialized || immediate || Repair(accepted) || Repair(Displayed))
            {
                initialized=true; Displayed=target=accepted; Tier=targetTier=tier;
                lower=0;lowering=false;return;
            }
            target=accepted;targetTier=tier;
            var same=Displayed==target && Tier==targetTier;
            lowering=!same;
            if(lowering && Displayed==HeldEquipmentMode.None)
            {Displayed=target;Tier=targetTier;lower=1;lowering=false;}

            var budget=float.IsNaN(seconds)||float.IsInfinity(seconds)?0:Mathf.Max(0,seconds)/HalfDuration;
            if(lowering)
            {
                var amount=Mathf.Min(budget,1-lower);lower+=amount;budget-=amount;
                if(lower>=1)
                {
                    Displayed=target;Tier=targetTier;lowering=false;
                    if(Displayed==HeldEquipmentMode.None)lower=0;
                }
            }
            if(!lowering)lower=Mathf.MoveTowards(lower,0,budget);
        }
        private static bool Repair(HeldEquipmentMode mode)=>
            mode==HeldEquipmentMode.RepairPart || mode==HeldEquipmentMode.RepairHammer;
    }
}
