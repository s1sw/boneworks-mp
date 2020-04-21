using MelonLoader;
using RootMotion;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MultiplayerMod
{
    public class PlayerRep
    {
        public GameObject ford;
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
        public GameObject pelvis;
        public GameObject nametag;
        public GameObject footL;
        public GameObject footR;
        public IKSolverVR.Arm lArm;
        public IKSolverVR.Arm rArm;
        public IKSolverVR.Spine spine;
        public VRIK ik;
        public GameObject namePlate;

        private static AssetBundle fordBundle;

        public static void LoadFord()
        {
            fordBundle = AssetBundle.LoadFromFile("ford.ford");
            if (fordBundle == null)
                MelonModLogger.LogError("Failed to load Ford asset bundle");

            GameObject fordPrefab = fordBundle.LoadAsset("Assets/brett_body.prefab").Cast<GameObject>();
            if (fordPrefab == null)
                MelonModLogger.LogError("Failed to load Ford from the asset bundle???");
        }

        public PlayerRep(string name)
        {
            GameObject ford = GameObject.Instantiate(fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>());

            // attempt to fix shaders
            foreach (SkinnedMeshRenderer smr in ford.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                MelonModLogger.Log(smr.gameObject.name);
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

            GameObject root = ford.transform.Find("Ford/Brett@neutral").gameObject;
            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt");
            var ik = root.AddComponent<VRIK>();

            VRIK.References bipedReferences = new VRIK.References
            {
                root = root.transform.Find("SHJntGrp"),

                spine = realRoot.Find("Spine_01SHJnt"),
                pelvis = realRoot,

                leftThigh = realRoot.Find("l_Leg_HipSHJnt"),
                leftCalf = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt"),
                leftFoot = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt"),
                leftToes = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt/l_Leg_BallSHJnt"),

                rightThigh = realRoot.Find("r_Leg_HipSHJnt"),
                rightCalf = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt"),
                rightFoot = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt"),
                rightToes = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt/r_Leg_BallSHJnt"),

                leftUpperArm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt"),
                leftForearm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt"),
                leftHand = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt"),

                rightUpperArm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt"),
                rightForearm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt"),
                rightHand = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt"),

                head = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt")
            };

            ik.enabled = true;
            ik.references = bipedReferences;
            ik.solver.plantFeet = false;
            ik.fixTransforms = false;
            
            ik.solver.leftLeg.positionWeight = 1.0f;
            ik.solver.leftLeg.swivelOffset = 0.0f;
            ik.solver.leftLeg.bendToTargetWeight = 0.0f;
            ik.solver.leftLeg.legLengthMlp = 1.0f;

            ik.solver.rightLeg.positionWeight = 1.0f;
            ik.solver.rightLeg.swivelOffset = 0.0f;
            ik.solver.rightLeg.bendToTargetWeight = 0.0f;
            ik.solver.rightLeg.legLengthMlp = 1.0f;

            ik.solver.hasChest = false;
            ik.solver.spine.chestGoalWeight = 0.0f;
            ik.solver.spine.pelvisPositionWeight = 0.0f;
            ik.solver.spine.pelvisRotationWeight = 0.0f;

            ik.solver.leftArm.positionWeight = 1.0f;
            ik.solver.leftArm.rotationWeight = 1.0f;
            ik.solver.leftArm.shoulderRotationMode = IKSolverVR.Arm.ShoulderRotationMode.YawPitch;
            ik.solver.leftArm.shoulderRotationWeight = 0.5f;
            ik.solver.leftArm.shoulderTwistWeight = 1.0f;
            ik.solver.leftArm.bendGoalWeight = 0.0f;
            ik.solver.leftArm.swivelOffset = 20.0f;
            ik.solver.leftArm.armLengthMlp = 1.0f;

            ik.solver.rightArm.positionWeight = 1.0f;
            ik.solver.rightArm.rotationWeight = 1.0f;
            ik.solver.rightArm.shoulderRotationMode = IKSolverVR.Arm.ShoulderRotationMode.YawPitch;
            ik.solver.rightArm.shoulderRotationWeight = 0.5f;
            ik.solver.rightArm.shoulderTwistWeight = 1.0f;
            ik.solver.rightArm.bendGoalWeight = 0.0f;
            ik.solver.rightArm.swivelOffset = 20.0f;
            ik.solver.rightArm.armLengthMlp = 1.0f;

            IKSolverVR.Locomotion l = ik.solver.locomotion;
            l.weight = 0.0f;
            l.blockingEnabled = false;
            l.blockingLayers = LayerMask.NameToLayer("Default");
            l.footDistance = 0.3f;
            l.stepThreshold = 0.35f;
            l.angleThreshold = 60.0f;
            l.comAngleMlp = 0.5f;
            l.maxVelocity = 0.3f;
            l.velocityFactor = 0.3f;
            l.maxLegStretch = 0.98f;
            l.rootSpeed = 20.0f;
            l.stepSpeed = 2.8f;
            l.relaxLegTwistMinAngle = 20.0f;
            l.relaxLegTwistSpeed = 400.0f;
            l.stepInterpolation = InterpolationMode.InOutSine;
            l.offset = Vector3.zero;

            GameObject lHandTarget = new GameObject("LHand");
            GameObject rHandTarget = new GameObject("RHand");
            GameObject pelvisTarget = new GameObject("Pelvis");
            GameObject headTarget = new GameObject("HeadTarget");
            GameObject lFootTarget = new GameObject("LFoot");
            GameObject rFootTarget = new GameObject("RFoot");

            ik.solver.leftArm.target = lHandTarget.transform;
            ik.solver.rightArm.target = rHandTarget.transform;
            ik.solver.spine.pelvisTarget = pelvisTarget.transform;
            ik.solver.spine.headTarget = headTarget.transform;
            ik.solver.leftLeg.target = lFootTarget.transform;
            ik.solver.rightLeg.target = rFootTarget.transform;

            namePlate = new GameObject("Nameplate");
            TextMeshPro tm = namePlate.AddComponent<TextMeshPro>();
            tm.text = name;
            tm.color = Color.green;
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontSize = 1.0f;

            footL = ik.solver.leftLeg.target.gameObject;
            footR = ik.solver.rightLeg.target.gameObject;
            head = headTarget;
            handL = lHandTarget;
            handR = rHandTarget;
            pelvis = pelvisTarget;
            this.ford = ford;
            lArm = ik.solver.leftArm;
            rArm = ik.solver.rightArm;
            spine = ik.solver.spine;
            this.ik = ik;
        }

        public void UpdateNameplateFacing(Transform cameraTransform)
        {
            namePlate.transform.position = head.transform.position + (Vector3.up * 0.3f);
            namePlate.transform.rotation = cameraTransform.rotation;
        }

        public void Destroy()
        {
            GameObject.Destroy(ford);
            GameObject.Destroy(head);
            GameObject.Destroy(handL);
            GameObject.Destroy(handR);
            GameObject.Destroy(pelvis);
            GameObject.Destroy(namePlate);
        }
    }
}
