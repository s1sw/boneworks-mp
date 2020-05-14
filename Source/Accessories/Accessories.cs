using UnityEngine;

namespace MultiplayerMod.Accessories
{
    public struct Accessory
    {
        GameObject prefab;
        Accessories.AttachPoint attachPoint;
    }

    public static partial class Accessories
    {
        public enum AttachPoint
        {
            Belt,
            Stomach,
            Chest,
            Neck,
            Head,
            ShoulderL,
            ShoulderR,
            UpperArmL,
            UpperArmR,
            LowerArmL,
            LowerArmR,
            WristL,
            WristR,
            HandL,
            HandR,
            UpperLegL,
            UpperLegR,
            LowerLegL,
            LowerLegR,
            AnkleL,
            AnkleR,
            FootL,
            FootR,
        }

        public static readonly string[] mountPoints = new string[]
        {
        "ROOTSHJnt",
        "ROOTSHJnt/Spine_01SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_TopSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Upper_Curve1SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Upper_Curve1SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_Arm_Lower_Curve1SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_Arm_Lower_Curve1SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt/l_Hand_2SHJnt",
        "ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt/r_Hand_2SHJnt",
        "ROOTSHJnt/l_Leg_HipSHJnt",
        "ROOTSHJnt/r_Leg_HipSHJnt",
        "ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt",
        "ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt",
        "ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt",
        "ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt",
        "ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt/l_Leg_BallSHJnt",
        "ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt/r_Leg_BallSHJnt",
        };

        public static Accessory CreateAccessory(Transform root)
        {
            return new Accessory();
        }

        public static void CreateDummies(Transform root)
        {
            for (int t = 0; t < mountPoints.Length; t++)
            {
                GameObject rep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rep.transform.localScale = Vector3.one / 10f;

                rep.transform.parent = root.Find(mountPoints[t]);

                rep.transform.localPosition = Vector3.zero;
                rep.transform.localRotation = Quaternion.identity;

                rep.GetComponent<Collider>().enabled = false;
            }
        }
    }
}
