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
using Harmony;
using StressLevelZero.Props.Weapons;

namespace MultiplayerMod
{
    public enum EnemyType
    {
        NullBody,
        FordEarlyExit,
        CorruptedNullBody
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
        public static event Action<Gun> OnFire;

        public static Player_Health LocalPlayerHealth
        {
            get
            {
                if (!localPlayerHealth)
                {
                    localPlayerHealth = FindRigManager().GetComponent<Player_Health>();
                }

                return localPlayerHealth;
            }
        }

        public static GameObject RigManager
        {
            get
            {
                if (!rigManager)
                    rigManager = FindRigManager();

                return rigManager;
            }
        }

        private static Player_Health localPlayerHealth;
        private static GameObject rigManager;

        private static readonly Dictionary<GunType, GameObject> gunPrefabs = new Dictionary<GunType, GameObject>()
        { };

        public static void Hook()
        {
            var harmonyInst = HarmonyInstance.Create("BWMP");
            harmonyInst.Patch(typeof(Gun).GetMethod("Fire"), new HarmonyMethod(typeof(BWUtil), "OnFireHook"));
        }

        private static void OnFireHook(Gun __instance)
        {
            OnFire?.Invoke(__instance);
        }

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
            GameObject root = GameObject.Find("[RigManager (Default Brett)]/[SkeletonRig (GameWorld Brett)]");

            return GetHumanoidRigTransforms(root);
        }

        public static BoneworksRigTransforms GetHumanoidRigTransforms(GameObject root)
        {
            BoneworksRigTransforms brt = new BoneworksRigTransforms()
            {
                root = root.transform,
                head = root.transform.Find("Head"),
                lfHand = root.transform.Find("Hand (left)"),
                rtHand = root.transform.Find("Hand (right)"),
                pelvis = root.transform.Find("Pelvis")
            };

            return brt;
        }

        public static void ApplyRigTransform(BoneworksRigTransforms rigTransforms, RigTFMsgBase tfMsg)
        {
            rigTransforms.root.position = tfMsg.pos_root;
            rigTransforms.root.rotation = tfMsg.rot_root;

            rigTransforms.head.position = tfMsg.pos_head;
            rigTransforms.head.rotation = tfMsg.rot_head;

            rigTransforms.lfHand.position = tfMsg.pos_lfHand;
            rigTransforms.lfHand.rotation = tfMsg.rot_lfHand;

            rigTransforms.rtHand.position = tfMsg.pos_rtHand;
            rigTransforms.rtHand.rotation = tfMsg.rot_rtHand;

            rigTransforms.pelvis.position = tfMsg.pos_pelvis;
            rigTransforms.pelvis.rotation = tfMsg.rot_pelvis;

        }

        public static string GetFullNamePath(GameObject obj)
        {
            if (obj.transform.parent == null)
                return obj.name;

            return GetFullNamePath(obj.transform.parent.gameObject) + "/" + obj.name + "|" + obj.transform.GetSiblingIndex();
        }

        public static GameObject GetObjectFromFullPath(string path)
        {
            string[] pathComponents = path.Split('/');

            // First object won't have a sibling index -
            // better hope that the game doesn't have identically named roots!
            // TODO: Could potentially work around this by
            // manually assigning IDs to each root upon scene load
            // but bleh

            GameObject rootObj;
            rootObj = GameObject.Find(pathComponents[0]);
            if (rootObj == null)
                return null;

            if (rootObj.transform.parent != null)
            {
                throw new Exception("Tried to find a root object but didn't get a root object. Try again, dumbass.");
            }

            GameObject currentObj = rootObj;

            for (int i = 1; i < pathComponents.Length; i++)
            {
                string[] splitComponent = pathComponents[i].Split('|');

                int siblingIdx = int.Parse(splitComponent[1]);
                string name = splitComponent[0];

                GameObject newObj = rootObj.transform.GetChild(siblingIdx).gameObject;

                if (newObj.name != name)
                {
                    throw new Exception("Name didn't match expected name at sibling index. Try again, dumbass.");
                }

                currentObj = newObj;
            }

            return currentObj;
        }

        private static GameObject FindRigManager()
        {
            return GameObject.Find("[RigManager (Default Brett)]");
        }
    }
}
