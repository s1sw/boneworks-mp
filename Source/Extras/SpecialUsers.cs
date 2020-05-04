using UnityEngine;
using Facepunch.Steamworks;

namespace MultiplayerMod.Extras
{
    public static class SpecialUsers
    {
        public static void GiveUniqueAppearances(SteamId userID, Transform root, TMPro.TextMeshPro text)
        {
            Color32 DevRed = new Color32(230, 0, 10, 255);
            Color32 AquaBlue = new Color32(64, 224, 208, 255);
            Color32 LGPurple = new Color32(155, 89, 182, 255);

            // Someone Somewhere
            if (userID == 76561198078346603)
            {
                GameObject crownObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Crown").gameObject;
                //GameObject glassesObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Glasses").gameObject;
                crownObj.SetActive(true);
                //glassesObj.SetActive(true);

                text.color = DevRed;
            }

            // zCubed
            if (userID == 76561198078927044)
                text.color = DevRed;

            // Aqua
            if (userID == 76561198383037191)
            {
                //GameObject hlLogo = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/HL_Logo").gameObject;
                //GameObject hlId = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/HL_ID").gameObject;
                GameObject helmetObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Helmet").gameObject;
                helmetObj.SetActive(true);

                text.color = AquaBlue;
            }

            // Camobiwon
            if (userID == 76561198060337335)
                text.color = LGPurple;
        }
    }
}
