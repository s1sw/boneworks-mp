using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;

namespace MultiplayerMod
{
    public enum EnemyType
    {
        NullBody,
        FordEarlyExit,
        CorruptedNullBody,
        Crablet
    }

    public enum GunType
    {
        EDER22,
        M1911,
        P350,
        MP5K,
        MP5KFlashlight,
        MP5KSabrelake,
        MP5,
        MK18Holo,
        MK18Sabrelake,
        MK18LaserForegrip,
        M16Naked,
        M16Ironsights,
        M16LaserForegrip,
        M16ACOG,
        Uzi
    }

    static class BWUtil
    {
        private static readonly Dictionary<GunType, GameObject> gunPrefabs = new Dictionary<GunType, GameObject>()
        {};

        public static void InitialiseGunPrefabs()
        {
            foreach (UnityEngine.Object obj in FindObjectsOfType<UnityEngine.Object>())
            {
                MelonModLogger.Log("found obj " + obj.name);
                if (obj.TryCast<GameObject>() != null)
                {
                    GameObject go = obj.Cast<GameObject>();
                    if (go.scene.name == null || go.scene.rootCount == 0)
                    {
                        MelonModLogger.Log("Found prefab: " + go.name);
                    }
                }
            }
        }

        public static GameObject SpawnGun(GunType type)
        {
            return Instantiate(gunPrefabs[type]).Cast<GameObject>();
        }

        public static GunType? GetGunType(GameObject gunObj)
        {
            string name = gunObj.name.ToLowerInvariant();
            if (name.Contains("eder22"))
            {
                return GunType.EDER22;
            }
            else
            {
                return null;
            }
        }

        public static BoneworksRigTransforms GetLocalRigTransforms()
        {
            GameObject root = GameObject.Find("[RigManager (Default Brett)]/[SkeletonRig (GameWorld Brett)]/Brett@neutral");

            return GetHumanoidRigTransforms(root);
        }

        public static BoneworksRigTransforms GetHumanoidRigTransforms(GameObject root)
        {
            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt");

            BoneworksRigTransforms brt = new BoneworksRigTransforms()
            {
                main = root.transform.Find("SHJntGrp/MAINSHJnt"),
                root = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt"),
                lHip = realRoot.Find("l_Leg_HipSHJnt"),
                rHip = realRoot.Find("r_Leg_HipSHJnt"),
                spine1 = realRoot.Find("Spine_01SHJnt"),
                spine2 = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt"),
                spineTop = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt"),
                lClavicle = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt"),
                rClavicle = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt"),
                lShoulder = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt"),
                rShoulder = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt"),
                lElbow = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt"),
                rElbow = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt"),
                lWrist = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt"),
                rWrist = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt"),
                neck = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt"),
                lAnkle = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt"),
                rAnkle = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt"),
                lKnee = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt"),
                rKnee = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt"),
            };

            return brt;
        }

        public static NpcRigTransforms GetNpcRigTransforms(GameObject physicsObj)
        {
            Transform physTransform = physicsObj.transform;

            NpcRigTransforms nrt = new NpcRigTransforms()
            {
                root = physTransform.Find("Root_M"),
                lHip = physTransform.Find("Root_M/Hip_L"),
                rHip = physTransform.Find("Root_M/Hip_R"),
                lKnee = physTransform.Find("Root_M/Hip_L/Knee_L"),
                rKnee = physTransform.Find("Root_M/Hip_R/Knee_R"),
                lAnkle = physTransform.Find("Root_M/Hip_L/Knee_L/Ankle_L"),
                rAnkle = physTransform.Find("Root_M/Hip_R/Knee_R/Ankle_R"),
                spine = physTransform.Find("Root_M/Spine_M"),
                chest = physTransform.Find("Root_M/Spine_M/Chest_M"),
                lShoulder = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_L"),
                rShoulder = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_R"),
                lElbow = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_L/Elbow_L"),
                rElbow = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_R/Elbow_R"),
                lWrist = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_L/Elbow_L/Wrist_L"),
                rWrist = physTransform.Find("Root_M/Spine_M/Chest_M/Shoulder_R/Elbow_R/Wrist_R")
            };

            return nrt;
        }

        public static void RigTransformsToMessage(NpcRigTransforms rigTransforms, out EnemyRigTransformMessage ertf)
        {
            ertf = new EnemyRigTransformMessage();
            
            ertf.rootPos = rigTransforms.root.position;
            ertf.lHipPos = rigTransforms.lHip.position;
            ertf.rHipPos = rigTransforms.rHip.position;
            ertf.lKneePos = rigTransforms.lKnee.position;
            ertf.rKneePos = rigTransforms.rKnee.position;
            ertf.lAnklePos = rigTransforms.lAnkle.position;
            ertf.rAnklePos = rigTransforms.rAnkle.position;
            ertf.spinePos = rigTransforms.spine.position;
            ertf.chestPos = rigTransforms.chest.position;
            ertf.lShoulderPos = rigTransforms.lShoulder.position;
            ertf.rShoulderPos = rigTransforms.rShoulder.position;
            ertf.lElbowPos = rigTransforms.lElbow.position;
            ertf.rElbowPos = rigTransforms.rElbow.position;
            ertf.lWristPos = rigTransforms.lWrist.position;
            ertf.rWristPos = rigTransforms.rWrist.position;

            ertf.rootRot = rigTransforms.root.rotation;
            ertf.lHipRot = rigTransforms.lHip.rotation;
            ertf.rHipRot = rigTransforms.rHip.rotation;
            ertf.lKneeRot = rigTransforms.lKnee.rotation;
            ertf.rKneeRot = rigTransforms.rKnee.rotation;
            ertf.lAnkleRot = rigTransforms.lAnkle.rotation;
            ertf.rAnkleRot = rigTransforms.rAnkle.rotation;
            ertf.spineRot =  rigTransforms.spine.rotation;
            ertf.chestRot = rigTransforms.chest.rotation;
            ertf.lShoulderRot = rigTransforms.lShoulder.rotation;
            ertf.rShoulderRot = rigTransforms.rShoulder.rotation;
            ertf.lElbowRot = rigTransforms.lElbow.rotation;
            ertf.rElbowRot = rigTransforms.rElbow.rotation;
            ertf.lWristRot = rigTransforms.lWrist.rotation;
            ertf.rWristRot = rigTransforms.rWrist.rotation;
        }

        public static EnemyRigTransformMessage LerpTransformMessage(EnemyRigTransformMessage a, EnemyRigTransformMessage b, float t)
        {
            EnemyRigTransformMessage ertf = new EnemyRigTransformMessage
            {
                rootPos = Vector3.Lerp(a.rootPos, b.rootPos, t),
                lHipPos = Vector3.Lerp(a.lHipPos, b.lHipPos, t),
                rHipPos = Vector3.Lerp(a.rHipPos, b.rHipPos, t),
                lKneePos = Vector3.Lerp(a.lKneePos, b.lKneePos, t),
                rKneePos = Vector3.Lerp(a.rKneePos, b.rKneePos, t),
                lAnklePos = Vector3.Lerp(a.lAnklePos, b.lAnklePos, t),
                rAnklePos = Vector3.Lerp(a.rAnklePos, b.rAnklePos, t),
                spinePos = Vector3.Lerp(a.spinePos, b.spinePos, t),
                chestPos = Vector3.Lerp(a.chestPos, b.chestPos, t),
                lShoulderPos = Vector3.Lerp(a.lShoulderPos, b.lShoulderPos, t),
                rShoulderPos = Vector3.Lerp(a.rShoulderPos, b.rShoulderPos, t),
                lElbowPos = Vector3.Lerp(a.lElbowPos, b.lElbowPos, t),
                rElbowPos = Vector3.Lerp(a.rElbowPos, b.rElbowPos, t),
                lWristPos = Vector3.Lerp(a.lWristPos, b.lWristPos, t),
                rWristPos = Vector3.Lerp(a.rWristPos, b.rWristPos, t),

                rootRot = Quaternion.Slerp(a.rootRot, b.rootRot, t),
                lHipRot = Quaternion.Slerp(a.lHipRot, b.lHipRot, t),
                rHipRot = Quaternion.Slerp(a.rHipRot, b.rHipRot, t),
                lKneeRot = Quaternion.Slerp(a.lKneeRot, b.rKneeRot, t),
                rKneeRot = Quaternion.Slerp(a.rKneeRot, b.rKneeRot, t),
                lAnkleRot = Quaternion.Slerp(a.lAnkleRot, b.lAnkleRot, t),
                rAnkleRot = Quaternion.Slerp(a.rAnkleRot, b.rAnkleRot, t),
                spineRot = Quaternion.Slerp(a.spineRot, b.spineRot, t),
                chestRot = Quaternion.Slerp(a.chestRot, b.chestRot, t),
                lShoulderRot = Quaternion.Slerp(a.lShoulderRot, b.lShoulderRot, t),
                rShoulderRot = Quaternion.Slerp(a.rShoulderRot, b.rShoulderRot, t),
                lElbowRot = Quaternion.Slerp(a.lElbowRot, b.lElbowRot, t),
                rElbowRot = Quaternion.Slerp(a.rElbowRot, b.rElbowRot, t),
                lWristRot = Quaternion.Slerp(a.lWristRot, b.lWristRot, t),
                rWristRot = Quaternion.Slerp(a.rWristRot, b.rWristRot, t)
            };

            return ertf;
        }

        public static void ApplyNpcRigTransform(NpcRigTransforms rigTransforms, EnemyRigTransformMessage tfMsg)
        {
            rigTransforms.root.position = tfMsg.rootPos;
            rigTransforms.lHip.position = tfMsg.lHipPos;
            rigTransforms.rHip.position = tfMsg.rHipPos;
            rigTransforms.lKnee.position = tfMsg.lKneePos;
            rigTransforms.rKnee.position = tfMsg.rKneePos;
            rigTransforms.lAnkle.position = tfMsg.lAnklePos;
            rigTransforms.rAnkle.position = tfMsg.rAnklePos;
            rigTransforms.spine.position = tfMsg.spinePos;
            rigTransforms.chest.position = tfMsg.chestPos;
            rigTransforms.lShoulder.position = tfMsg.lShoulderPos;
            rigTransforms.rShoulder.position = tfMsg.rShoulderPos;
            rigTransforms.lElbow.position = tfMsg.lElbowPos;
            rigTransforms.rElbow.position = tfMsg.rElbowPos;
            rigTransforms.lWrist.position = tfMsg.lWristPos;
            rigTransforms.rWrist.position = tfMsg.rWristPos;

            rigTransforms.root.rotation = tfMsg.rootRot;
            rigTransforms.lHip.rotation = tfMsg.lHipRot;
            rigTransforms.rHip.rotation = tfMsg.rHipRot;
            rigTransforms.lKnee.rotation = tfMsg.lKneeRot;
            rigTransforms.rKnee.rotation = tfMsg.rKneeRot;
            rigTransforms.lAnkle.rotation = tfMsg.lAnkleRot;
            rigTransforms.rAnkle.rotation = tfMsg.rAnkleRot;
            rigTransforms.spine.rotation = tfMsg.spineRot;
            rigTransforms.chest.rotation = tfMsg.chestRot;
            rigTransforms.lShoulder.rotation = tfMsg.lShoulderRot;
            rigTransforms.rShoulder.rotation = tfMsg.rShoulderRot;
            rigTransforms.lElbow.rotation = tfMsg.lElbowRot;
            rigTransforms.rElbow.rotation = tfMsg.rElbowRot;
            rigTransforms.lWrist.rotation = tfMsg.lWristRot;
            rigTransforms.rWrist.rotation = tfMsg.rWristRot;
        }

        public static void ApplyRigTransform(BoneworksRigTransforms rigTransforms, RigTFMsgBase tfMsg)
        {
            rigTransforms.main.position = tfMsg.posMain;
            rigTransforms.main.rotation = tfMsg.rotMain;

            rigTransforms.root.position = tfMsg.posRoot;
            rigTransforms.root.rotation = tfMsg.rotRoot;

            rigTransforms.lHip.position = tfMsg.posLHip;
            rigTransforms.lHip.rotation = tfMsg.rotLHip;

            rigTransforms.rHip.position = tfMsg.posRHip;
            rigTransforms.rHip.rotation = tfMsg.rotRHip;

            rigTransforms.lAnkle.position = tfMsg.posLAnkle;
            rigTransforms.lAnkle.rotation = tfMsg.rotLAnkle;

            rigTransforms.rAnkle.position = tfMsg.posRAnkle;
            rigTransforms.rAnkle.rotation = tfMsg.rotRAnkle;

            rigTransforms.lKnee.position = tfMsg.posLKnee;
            rigTransforms.lKnee.rotation = tfMsg.rotLKnee;

            rigTransforms.rKnee.position = tfMsg.posRKnee;
            rigTransforms.rKnee.rotation = tfMsg.rotRKnee;

            rigTransforms.spine1.position = tfMsg.posSpine1;
            rigTransforms.spine1.rotation = tfMsg.rotSpine1;

            rigTransforms.spine2.position = tfMsg.posSpine2;
            rigTransforms.spine2.rotation = tfMsg.rotSpine2;

            rigTransforms.spineTop.position = tfMsg.posSpineTop;
            rigTransforms.spineTop.rotation = tfMsg.rotSpineTop;

            rigTransforms.lClavicle.position = tfMsg.posLClavicle;
            rigTransforms.lClavicle.rotation = tfMsg.rotLClavicle;

            rigTransforms.rClavicle.position = tfMsg.posRClavicle;
            rigTransforms.rClavicle.rotation = tfMsg.rotRClavicle;

            rigTransforms.neck.position = tfMsg.posNeck;
            rigTransforms.neck.rotation = tfMsg.rotNeck;

            rigTransforms.lShoulder.position = tfMsg.posLShoulder;
            rigTransforms.lShoulder.rotation = tfMsg.rotLShoulder;

            rigTransforms.rShoulder.position = tfMsg.posRShoulder;
            rigTransforms.rShoulder.rotation = tfMsg.rotRShoulder;

            rigTransforms.lElbow.position = tfMsg.posLElbow;
            rigTransforms.lElbow.rotation = tfMsg.rotLElbow;

            rigTransforms.rElbow.position = tfMsg.posRElbow;
            rigTransforms.rElbow.rotation = tfMsg.rotRElbow;

            rigTransforms.lWrist.position = tfMsg.posLWrist;
            rigTransforms.lWrist.rotation = tfMsg.rotLWrist;

            if (rigTransforms.rWrist)
            {
                rigTransforms.rWrist.position = tfMsg.posRWrist;
                rigTransforms.rWrist.rotation = tfMsg.rotRWrist;
            }
        }
    }
}
