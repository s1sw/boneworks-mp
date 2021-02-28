using Facepunch.Steamworks;
using MelonLoader;
using BigChungus;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Object;
using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Features;
using Facepunch.Steamworks.Data;
using System.Collections;
using MultiplayerMod.Source.Representations;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Combat;
using BoneworksModdingToolkit;
using StressLevelZero.UI.Radial;
using StressLevelZero.Data;
using StressLevelZero.VRMK;

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
        public GameObject rightGun;
        public Gun rightGunScript;
        public BulletObject rightBulletObject;
        public GameObject gunRParent;

        public GameObject leftGun;
        public Gun leftGunScript;
        public BulletObject leftBulletObject;
        public GameObject gunLParent;
        public IKAnimator ikAnimator;

        public static AssetBundle fordBundle;

        // Async operations
        //Task<Facepunch.Steamworks.Data.Image?> task_asyncLoadPlayerIcon;
        //public bool isPlayerIconLoaded = false;

        public static void LoadFord()
        {
            fordBundle = AssetBundle.LoadFromFile("ford.ford");
            if (fordBundle == null)
                MelonLogger.LogError("Failed to load Ford asset bundle");

            GameObject fordPrefab = fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>();
            if (fordPrefab == null)
                MelonLogger.LogError("Failed to load Ford from the asset bundle???");
        }

        // Constructor
        public PlayerRep(string name, SteamId steamId)
        {
            this.steamId = steamId;

            // Create this player's "Ford" to represent them, known as their rep
            GameObject ford = Instantiate(fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>());

            // Makes sure that the rep isn't destroyed per level change.
            DontDestroyOnLoad(ford);

            ImpactPropertiesManager bloodManager = ford.AddComponent<ImpactPropertiesManager>();
            bloodManager.material = ImpactPropertiesVariables.Material.Blood;
            bloodManager.modelType = ImpactPropertiesVariables.ModelType.Skinned;
            bloodManager.MainColor = UnityEngine.Color.red;
            bloodManager.SecondaryColor = UnityEngine.Color.red;
            bloodManager.PenetrationResistance = 0.8f;
            bloodManager.megaPascalModifier = 1;
            bloodManager.FireResistance = 100;
            Collider[] colliders = ford.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                ImpactProperties blood = c.gameObject.AddComponent<ImpactProperties>();
                blood.material = ImpactPropertiesVariables.Material.Blood;
                blood.modelType = ImpactPropertiesVariables.ModelType.Skinned;
                blood.MainColor = UnityEngine.Color.red;
                blood.SecondaryColor = UnityEngine.Color.red;
                blood.PenetrationResistance = 0.8f;
                blood.megaPascalModifier = 1;
                blood.FireResistance = 100;
                blood.MyCollider = c;
                blood.hasManager = true;
                blood.Manager = bloodManager;
            }

            GameObject root = ford.transform.Find("Ford/Brett@neutral").gameObject; // Get the rep's head

            //faceAnimator = new FaceAnimator();
            //faceAnimator.animator = root.GetComponent<Animator>();
            //faceAnimator.faceTime = 10;

            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt"); // Then get the head's root joint

            // Assign targets for the IK system
            GameObject pelvisTarget = new GameObject("Pelvis");

            // Create an anchor object to hold the rep's gun
            gunRParent = new GameObject("gunRParent");
            gunRParent.transform.parent = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            gunRParent.transform.localPosition = new Vector3(0.0758f, -0.0459f, -0.0837f);
            gunRParent.transform.localEulerAngles = new Vector3(2.545f, -251.689f, 149.121f);

            GameObject rig = Player.FindRigManager();
            PopUpMenuView menu = rig.GetComponentInChildren<PopUpMenuView>();
            GameObject spawnGun = menu.utilityGunSpawnable.prefab;
            SpawnableMasterListData masterList = spawnGun.GetComponent<SpawnGun>().masterList;
            rightGun = Instantiate(masterList.objects[MultiplayerMod.gunOffset].prefab.transform.Find("Physics/Root/Gun").gameObject);
            rightGun.GetComponent<Rigidbody>().isKinematic = true;
            rightGun.transform.parent = gunRParent.transform;
            rightGun.transform.localPosition = Vector3.zero;
            rightGun.transform.localRotation = Quaternion.identity;
            rightGunScript = rightGun.GetComponent<Gun>();
            rightGunScript.proxyOverride = null;
            rightBulletObject = rightGunScript.overrideMagazine.AmmoSlots[0];
            rightGunScript.roundsPerMinute = 20000;
            rightGunScript.roundsPerSecond = 333;
            GameObject.Destroy(rightGun.GetComponent<ConfigurableJoint>());
            GameObject.Destroy(rightGun.GetComponent<ImpactProperties>());
            GameObject.Destroy(rightGun.transform.Find("attachment_Lazer_Omni").gameObject);

            // Create an anchor object to hold the rep's gun
            gunLParent = new GameObject("gunRParent");
            gunLParent.transform.parent = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            gunLParent.transform.localPosition = new Vector3(-0.0941f, 0.0452f, 0.0945f);
            gunLParent.transform.localEulerAngles = new Vector3(3.711f, -81.86301f, -157.739f);

            leftGun = Instantiate(masterList.objects[MultiplayerMod.gunOffset].prefab.transform.Find("Physics/Root/Gun").gameObject);
            leftGun.GetComponent<Rigidbody>().isKinematic = true;
            leftGun.transform.parent = gunLParent.transform;
            leftGun.transform.localPosition = Vector3.zero;
            leftGun.transform.localRotation = Quaternion.identity;
            leftGunScript = leftGun.GetComponent<Gun>();
            leftGunScript.proxyOverride = null;
            leftBulletObject = leftGunScript.overrideMagazine.AmmoSlots[0];
            GameObject.Destroy(leftGun.GetComponent<ConfigurableJoint>());
            GameObject.Destroy(leftGun.transform.Find("attachment_Lazer_Omni").gameObject);
            GameObject.Destroy(leftGun.GetComponent<ImpactProperties>());

            // If for whatever reason this is needed, show or hide the rep's body and hair
            root.transform.Find("geoGrp/brett_body").GetComponent<SkinnedMeshRenderer>().enabled = showBody;
            //root.transform.Find("geoGrp/brett_hairCards").gameObject.SetActive(showHair);

            // Assign the transforms for the rep
            rigTransforms = BWUtil.GetHumanoidRigTransforms(ford.transform.Find("Ford").gameObject);

            // Grab these body parts from the rigTransforms
            head = rigTransforms.head.gameObject;
            handL = rigTransforms.lfHand.gameObject;
            handR = rigTransforms.rtHand.gameObject;
            pelvis = rigTransforms.pelvis.gameObject;
            footL = rigTransforms.lfFoot.gameObject;
            footR = rigTransforms.rtFoot.gameObject;

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

            this.ford = ford;

            ikAnimator = new IKAnimator();
            ikAnimator.pelvis = pelvis.transform;
            ikAnimator.slzBody_Blend = root.GetComponent<SLZ_BodyBlender>();
            ikAnimator.slzBody = ford.transform.Find("Ford/Body").gameObject.GetComponent<SLZ_Body>();
            ikAnimator.slzBody.OnStart();
        }

        private IEnumerator AsyncAvatarRoutine(SteamId id)
        {
            Task<Image?> imageTask = SteamFriends.GetLargeAvatarAsync(id);
            while (!imageTask.IsCompleted)
            {
                // WaitForEndOfFrame is broken in MelonLoader, so use WaitForSeconds
                yield return null;
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
                    namePlate.transform.position = rigTransforms.head.transform.position + (Vector3.up * 0.3f);
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
