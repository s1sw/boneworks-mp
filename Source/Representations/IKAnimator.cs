using StressLevelZero.VRMK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigChungus;
using UnityEngine;
using MultiplayerMod.Representations;
using StressLevelZero.Rig;

namespace MultiplayerMod.Source.Representations
{
    public class IKAnimator
    {
        public Transform pelvis;
        public SLZ_Body slzBody;
        public GameWorldSkeletonRig gameWorldSkeletonRig;
        public SLZ_BodyBlender slzBody_Blend;

        Vector3 lastPelvisPosition;
        Vector3 lastPelvisVelocity;

        public void Update()
        {
            Vector3 pelvisVelocity = Vector3.zero;
            Vector3 pelvisAccel = Vector3.zero;
            slzBody.FullBodyUpdate(pelvisVelocity, pelvisAccel);
            slzBody_Blend.UpdateBlender();
            lastPelvisPosition = pelvis.position;
            lastPelvisVelocity = pelvisVelocity;
        }
    }
}