using UnityEngine;
using Facepunch.Steamworks;
using MultiplayerMod.Representations;
using MelonLoader;

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
                crownObj.SetActive(true);

                text.color = DevRed;
            }

            // zCubed
            if (userID == 76561198078927044)
                text.color = DevRed;

            //Maranara
            if (userID == 76561198088708478)
            {
                root.parent.parent.parent.Find("geoGrp/brett_body").GetComponent<SkinnedMeshRenderer>().materials[1].color = new Color(0.5141f, 1, 0.6199f);
                GameObject weaponWings = PlayerRep.fordBundle.LoadAsset("Assets/WeaponWings.prefab").Cast<GameObject>();
                if (weaponWings == null)
                    MelonLogger.LogError("Failed to load WeaponWings from bundle.");
                else
                {
                    GameObject wingInstance = GameObject.Instantiate(weaponWings);
                    wingInstance.transform.parent = root.Find("Spine_01SHJnt");
                    wingInstance.transform.localPosition = Vector3.zero;
                    wingInstance.transform.localEulerAngles = new Vector3(-0.042f, 0.057f, 30.129f);
                }
                text.color = DevRed;
            }

            // Camobiwon
            if (userID == 76561198060337335)
                text.color = LGPurple;
        }
    }
}
