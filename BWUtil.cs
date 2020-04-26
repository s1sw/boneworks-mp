using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;

namespace MultiplayerMod
{
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
    }
}
