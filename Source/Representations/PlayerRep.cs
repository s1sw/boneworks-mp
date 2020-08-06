using Facepunch.Steamworks;
using MelonLoader;
using RootMotion;
using RootMotion.FinalIK;
using StressLevelZero.Rig;
using StressLevelZero.VFX;
//using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Object;

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Features;
using Facepunch.Steamworks.Data;
using System.Collections;

namespace MultiplayerMod.Representations
{
    public class PlayerRep
    {
        public static bool showBody = true;
        public static bool showHair = true;

        public GameObject ford;
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
        public GameObject pelvis;
        public GameObject nametag;
        public GameObject footL;
        public GameObject footR;
        public GameObject namePlate;
        public SteamId steamId;
        public BoneworksRigTransforms rigTransforms;
        public GameObject currentGun;
        public GameObject gunParent;

        //IK vars
        public IKSolverVR.Arm ik_lArm;
        public IKSolverVR.Arm ik_rArm;
        public IKSolverVR.Spine ik_spine;
        public VRIK ik;

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

        // Constructor
        public PlayerRep(string name, SteamId steamId)
        {
            this.steamId = steamId;

            // Create this player's "Ford" to represent them, known as their rep
            GameObject ford = Instantiate(fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>());

            // Makes sure that the rep isn't destroyed per level change.
            DontDestroyOnLoad(ford);

            
            GameObject root = ford.transform.Find("Ford/Brett@neutral").gameObject; // Get the rep's head
            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt"); // Then get the head's root joint

            // Assign targets for the IK system
            //GameObject lHandTarget = new GameObject("LHand");
            //GameObject rHandTarget = new GameObject("RHand");
            GameObject pelvisTarget = new GameObject("Pelvis");
            //GameObject headTarget = new GameObject("HeadTarget");
            //GameObject lFootTarget = new GameObject("LFoot");
            //GameObject rFootTarget = new GameObject("RFoot");

            // Create an anchor object to hold the rep's gun
            gunParent = new GameObject("gunParent");
            gunParent.transform.parent = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            gunParent.transform.localPosition = Vector3.zero;
            gunParent.transform.localRotation = Quaternion.identity;

            // If for whatever reason this is needed, show or hide the rep's body and hair
            root.transform.Find("geoGrp/brett_body").GetComponent<SkinnedMeshRenderer>().enabled = showBody;
            root.transform.Find("geoGrp/brett_hairCards").gameObject.SetActive(showHair);

            // Assign the transforms for the rep
            rigTransforms = BWUtil.GetHumanoidRigTransforms(root);

            // Grab these body parts from the rigTransforms
            head = rigTransforms.neck.gameObject;
            handL = rigTransforms.lWrist.gameObject;
            handR = rigTransforms.rWrist.gameObject;
            pelvis = rigTransforms.spine1.gameObject;

            // Create the nameplate and assign values to the TMP's vars
            namePlate = new GameObject("Nameplate");
            TextMeshPro tm = namePlate.AddComponent<TextMeshPro>();
            tm.text = name;
            tm.color = UnityEngine.Color.green;
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontSize = 1.0f;

            // Prevents the nameplate from being destroyed during a level change
            DontDestroyOnLoad(namePlate);

            MelonCoroutines.Start(AsyncAvatarRoutine(steamId));

            // Gives certain users special appearances
            Extras.SpecialUsers.GiveUniqueAppearances(steamId, realRoot, tm);

            // Change the shader to the one that's already used in the game
            // Without this, the player model will only show in one eye
            foreach (SkinnedMeshRenderer smr in ford.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }
            foreach (MeshRenderer smr in ford.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

#if DEBUG
            //zCubed.Accessories.Accessory.CreateDummies(realRoot.parent);
#endif

            this.ford = ford;
        }

        private IEnumerator AsyncAvatarRoutine(SteamId id)
        {
            Task<Image?> imageTask = SteamFriends.GetLargeAvatarAsync(id);
            while (!imageTask.IsCompleted)
            {
                // WaitForEndOfFrame is broken in MelonLoader, so use WaitForSeconds
                yield return new WaitForSeconds(0.011f);
            }

            if (imageTask.Result.HasValue)
            {
                GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(avatar.GetComponent<Collider>());
                var avatarMr = avatar.GetComponent<MeshRenderer>();
                var avatarMat = avatarMr.material;
                avatarMat.shader = Shader.Find("Unlit/Texture");

                var avatarIcon = imageTask.Result.Value;

                Texture2D returnTexture = new Texture2D((int)avatarIcon.Width, (int)avatarIcon.Height, TextureFormat.RGBA32, false, true);
                GCHandle pinnedArray = GCHandle.Alloc(avatarIcon.Data, GCHandleType.Pinned);
                IntPtr pointer = pinnedArray.AddrOfPinnedObject();
                returnTexture.LoadRawTextureData(pointer, avatarIcon.Data.Length);
                returnTexture.Apply();
                pinnedArray.Free();

                avatarMat.mainTexture = returnTexture;

                avatar.transform.SetParent(namePlate.transform);
                avatar.transform.localScale = new Vector3(0.25f, -0.25f, 0.25f);
                avatar.transform.localPosition = new Vector3(0.0f, 0.2f, 0.0f);
            }
        }

        // Updates the NamePlate's direction to face towards the player's camera
        public void UpdateNameplateFacing(Transform cameraTransform)
        {
            if (namePlate.activeInHierarchy != ClientSettings.hiddenNametags)
                namePlate.SetActive(ClientSettings.hiddenNametags);

            if (namePlate.activeInHierarchy)
            {
                if (showBody)
                {
                    namePlate.transform.position = head.transform.position + (Vector3.up * 0.3f);
                    namePlate.transform.rotation = cameraTransform.rotation;
                }
                else
                {
                    namePlate.transform.position = rigTransforms.neck.transform.position + (Vector3.up * 0.3f);
                    namePlate.transform.rotation = cameraTransform.rotation;
                }
            }
        }

        // Destroys the GameObjects stored inside this class, preparing this instance for deletion
        public void Destroy()
        {
            UnityEngine.Object.Destroy(ford);
            UnityEngine.Object.Destroy(head);
            UnityEngine.Object.Destroy(handL);
            UnityEngine.Object.Destroy(handR);
            UnityEngine.Object.Destroy(pelvis);
            UnityEngine.Object.Destroy(namePlate);
        }

        // Applies the information recieved from the Transform packet
        public void ApplyTransformMessage<T>(T tfMsg) where T : RigTFMsgBase
        {
            BWUtil.ApplyRigTransform(rigTransforms, tfMsg);
        }
    }
}
