using UnityEngine;
using Facepunch.Steamworks;

namespace MultiplayerMod.Extras
{
    public static class SpecialUsers
    {
        public static void GiveUniqueAccessories(SteamId userID, Transform root)
        {
            if (userID == 76561198078346603)
            {
                GameObject crownObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Crown").gameObject;
                //GameObject glassesObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Glasses").gameObject;
                crownObj.SetActive(true);
                //glassesObj.SetActive(true);
            }

            if (userID == 76561198383037191)
            {
                //GameObject hlLogo = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/HL_Logo").gameObject;
                //GameObject hlId = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/HL_ID").gameObject;
                GameObject helmetObj = root.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt/Neck_02SHJnt/Neck_TopSHJnt/Head_Helmet").gameObject;
                helmetObj.SetActive(true);
            }
        }
    }
}
