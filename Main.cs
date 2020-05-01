using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using NET_SDK;
using Facepunch.Steamworks;
using Discord;
using StressLevelZero.Props;
using StressLevelZero.Props.Weapons;
using BoneHook;

using static UnityEngine.Object;
using Utilties;
using StressLevelZero.Interaction;
using StressLevelZero.Combat;
using StressLevelZero.Utilities;
using StressLevelZero.Pool;
using StressLevelZero.AI;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.11.0";
        public const string DownloadLink = "https://discord.gg/2Wn3N2P";
    }

    public struct BoneworksRigTransforms
    {
        public Transform main;
        public Transform root;
        public Transform lHip;
        public Transform rHip;
        public Transform spine1;
        public Transform spine2;
        public Transform spineTop;
        public Transform lClavicle;
        public Transform rClavicle;
        public Transform neck;
        public Transform lShoulder;
        public Transform rShoulder;
        public Transform lElbow;
        public Transform rElbow;
        public Transform lKnee;
        public Transform rKnee;
        public Transform lAnkle;
        public Transform rAnkle;
        public Transform lWrist;
        public Transform rWrist;
    }


    public enum MessageType
    {
        Join,
        PlayerName,
        OtherPlayerName,
        PlayerPosition,
        OtherPlayerPosition,
        Disconnect,
        ServerShutdown,
        JoinRejected,
        SceneTransition,
        FullRig,
        OtherFullRig,
        HandGunChange,
        OtherHandGunChange,
        SetPartyId,
        EnemyRigTransform,
        Attack
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

    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 30;

        private bool isServer = false;

        private MultiplayerUI ui;
        private readonly Client client = new Client();
        private readonly Server server = new Server();

        internal static event Action<int> OnLevelWasLoadedEvent;
        internal static event Action<int> OnLevelWasInitializedEvent;

        internal delegate float TakeDamageDelegate(IntPtr instance, int m, IntPtr attack);

        static TakeDamageDelegate origTakeDamage;

        private static void Hook(IntPtr orig, IntPtr reflect)
        {
            typeof(Imports).GetMethod("Hook", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { orig, reflect });
        }

        private unsafe static void Hook<TDelegate>(string klass, string methodName, Type type, out TDelegate oldPtr)
        {
            IntPtr replacementPtr = type.GetMethod("Hook" + methodName, BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();

            Hook(SDK.GetClass(klass).GetMethod(methodName).Ptr, replacementPtr);

            oldPtr = Marshal.GetDelegateForFunctionPointer<TDelegate>(*(IntPtr*)SDK.GetClass(klass).GetMethod(methodName).Ptr);
        }

        private static float HookTakeDamage(IntPtr instance, int m, IntPtr attack)
        {
            float ret = origTakeDamage(instance, m, attack);

            MelonModLogger.Log("M: " + m.ToString());
            MelonModLogger.Log("Attack: ");
            Attack atk = new Attack(attack);
            MelonModLogger.Log("Type:" + atk.attackType.ToString());
            MelonModLogger.Log("Origin:" + atk.origin.ToString());
            MelonModLogger.Log("Normal:" + atk.normal.ToString());
            MelonModLogger.Log("Damage:" + atk.damage.ToString());
            MelonModLogger.Log("Back facing:" + atk.backFacing.ToString());

            return ret;
        }

        public unsafe override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");
            ModPrefs.RegisterPrefBool("MPMod", "MrCleanFord", false, "90% effective hair removal solution");

            SteamNetworking.AllowP2PPacketRelay(true);
            ui = new MultiplayerUI();
            PlayerRep.LoadFord();

            PlayerRep.hideBody = false;
            PlayerRep.showHair = ModPrefs.GetBool("MPMod", "MrCleanFord");
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

            Hook("PuppetMasta.SubBehaviourHealth", "TakeDamage", typeof(MultiplayerMod), out origTakeDamage);

            //PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject += PlayerHooks_OnPlayerLetGoObject;
            //BWUtil.InitialiseGunPrefabs();
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (level == -1) return;
            MelonModLogger.Log("Loaded scene " + level.ToString() + "(" + BoneworksSceneManager.GetSceneNameFromScenePath(level) + ") (from " + SceneManager.GetActiveScene().name + ")");

            OnLevelWasLoadedEvent?.Invoke(level);
        }

        private void FixObjectShaders(GameObject obj)
        {
            foreach (SkinnedMeshRenderer smr in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

            foreach (MeshRenderer smr in obj.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    string sName = m.shader.name;
                    sName = sName.Replace(" (to_replace)", "");
                    m.shader = Shader.Find(sName);
                }
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            ui.Recreate();
            MelonModLogger.Log("Initialized scene " + level.ToString());
        }

#if DEBUG
        private bool useTestModel = false;
        private PlayerRep testRep;
#endif

        public void PrintProps<T>(T t)
        {
            MelonModLogger.Log("====== Type " + t.ToString() + "======");

            System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

            foreach (var pi in props)
            {
                //if (pi.PropertyType.IsPrimitive)
                try
                {
                    var val = pi.GetValue(t);
                    if (val != null)
                        MelonModLogger.Log(pi.Name + ": " + val.ToString());
                    else
                        MelonModLogger.Log(pi.Name + ": null");
                }
                catch
                {
                    MelonModLogger.LogError("Error tring to get property " + pi.Name);
                }
            }
        }

        public void PrintComponentProps<T>(GameObject go)
        {
            try
            {
                if (go == null)
                    MelonModLogger.LogError("go was null???");

                T t = go.GetComponent<T>();

                if (t == null)
                    MelonModLogger.LogError("Couldn't find component " + t.GetType().Name);

                MelonModLogger.Log("====== Component type " + t.ToString() + "======");

                System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

                foreach (var pi in props)
                {
                    //if (pi.PropertyType.IsPrimitive)
                    try
                    {
                        var val = pi.GetValue(t);
                        if (val != null)
                            MelonModLogger.Log(pi.Name + ": " + val.ToString());
                        else
                            MelonModLogger.Log(pi.Name + ": null");
                    }
                    catch
                    {
                        MelonModLogger.LogError("Error tring to get property " + pi.Name);
                    }
                }
            }
            catch
            {
                MelonModLogger.LogError("i don't know anymore");
            }
        }

        private void PrintChildHierarchy(GameObject parent, int currentDepth = 0)
        {
            string offset = "";

            for (int j = 0; j < currentDepth; j++)
            {
                offset += "\t";
            }

            MelonModLogger.Log(offset + " Has components:");

            foreach (Component c in parent.GetComponents<Component>())
            {
                MelonModLogger.Log(offset + c.ToString());
            }

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;

                

                MelonModLogger.Log(offset + "-" + child.name);

                

                PrintChildHierarchy(child, currentDepth + 1);

                
            }
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !isServer)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Connect(ModPrefs.GetString("MPMod", "HostSteamID"));
                    SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + client.ServerId);
                    SteamFriends.SetRichPresence("steam_player_group", client.ServerId.ToString());
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    SteamFriends.SetRichPresence("steam_display", "Hosting multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + SteamClient.SteamId);
                    SteamFriends.SetRichPresence("steam_player_group", SteamClient.SteamId.ToString());
                    server.StartServer();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Disconnect();
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    MelonModLogger.Log("Stopping server...");
                    server.StopServer();
                }
            }

//#if DEBUG
            //if (Input.GetKeyDown(KeyCode.N))
            //{
            //    useTestModel = true;
            //    testRep = new PlayerRep(SteamClient.Name, SteamClient.SteamId);
            //    //smallPlayerIds.Add(SteamClient.SteamId, byte.MaxValue);
            //    playerObjects.Add(byte.MaxValue, testRep);
            //    HandGunChangeMessage hgcm = new HandGunChangeMessage
            //    {
            //        isForOtherPlayer = false,
            //        playerId = byte.MaxValue,
            //        type = GunType.EDER22,
            //        destroy = false
            //    };
            //    serverId = SteamClient.SteamId;

            //    //SendToServer(hgcm, P2PSend.Reliable);
            //    SteamNetworking.SendP2PPacket(SteamClient.SteamId, hgcm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
            //}

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    foreach (var cs in FindObjectsOfType<ClaimedSpawner>())
            //    {
            //        MelonModLogger.Log(cs.spawnObject.title + ": " + cs.spawnObject.prefab.GetInstanceID());
            //    }
            //}

            //// Horrid janky testing for bodies
            //// Requires the server to be started first!

            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    GameObject go = Instantiate(FindObjectFromInstanceID(22072)).Cast<GameObject>();
            //    go.transform.position = Camera.current.transform.position + Camera.current.transform.forward;
            //    MelonModLogger.Log(go.name);
            //    Gun gun = go.GetComponent<Gun>();
            //    for (int i = 0; i < 15; i++)
            //    {
            //        GameObject magazineObj = Instantiate(gun.spawnableMagazine.prefab);
            //        magazineObj.transform.position = Camera.current.transform.position + Camera.current.transform.forward + Vector3.up;
            //    }
            //}

            if (Input.GetKeyDown(KeyCode.P))
            {
                Pool nullbodyPool = null;
                foreach (Pool p in FindObjectsOfType<Pool>())
                {
                    if (p.Prefab != null && p.Prefab.name.ToLowerInvariant().Contains("nullbody"))
                    {
                        //MelonModLogger.Log("Found nullbody pool");
                        nullbodyPool = p;
                        //MelonModLogger.Log("Set nullbody pool");
                    }
                }

                //if (nullbodyPool == null)
                //{
                //    MelonModLogger.LogError("Couldn't find the nullbody pool :(");
                //}
                //else
                //{

                for (int i = 0; i < nullbodyPool.transform.childCount; i++)
                {
                    GameObject childNullbody = nullbodyPool.transform.GetChild(i).gameObject;
                    //PrintChildHierarchy(childNullbody);
                    //BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(childNullbody.transform.Find("brettEnemy@neutral").gameObject);
                    //PrintProps(childNullbody.GetComponent<AIBrain>().behaviour.health);
                    var brain = childNullbody.GetComponent<AIBrain>();
                    //if (!brain.isDead)
                    MelonModLogger.Log("A: " + childNullbody.name);
                }
                //}

                foreach (AIBrain brain in FindObjectsOfType<AIBrain>())
                {
                    MelonModLogger.Log("B: " + brain.gameObject.name);
                    AIBrain brain2 = brain.gameObject.GetComponent<AIBrain>();
                    Attack attack = new Attack();
                    attack.damage = 0.3f;
                    MelonModLogger.Log("pain");
                    brain2.behaviour.health.TakeDamage(1, attack);

                    brain2.behaviour.sfx.Pain(50.0f);
                }

                //nullbodyPool.DespawnAll(true);
            }

            //if (useTestModel)
            //{
            //    Vector3 offsetVec = new Vector3(0.0f, 0.0f, 1.0f);

            //    FullRigTransformMessage frtm = new FullRigTransformMessage
            //    {
            //        posMain = localRigTransforms.main.position + offsetVec,
            //        posRoot = localRigTransforms.root.position + offsetVec,
            //        posLHip = localRigTransforms.lHip.position + offsetVec,
            //        posRHip = localRigTransforms.rHip.position + offsetVec,
            //        posLKnee = localRigTransforms.lKnee.position + offsetVec,
            //        posRKnee = localRigTransforms.rKnee.position + offsetVec,
            //        posLAnkle = localRigTransforms.lAnkle.position + offsetVec,
            //        posRAnkle = localRigTransforms.rAnkle.position + offsetVec,

            //        posSpine1 = localRigTransforms.spine1.position + offsetVec,
            //        posSpine2 = localRigTransforms.spine2.position + offsetVec,
            //        posSpineTop = localRigTransforms.spineTop.position + offsetVec,
            //        posLClavicle = localRigTransforms.lClavicle.position + offsetVec,
            //        posRClavicle = localRigTransforms.rClavicle.position + offsetVec,
            //        posNeck = localRigTransforms.neck.position + offsetVec,
            //        posLShoulder = localRigTransforms.lShoulder.position + offsetVec,
            //        posRShoulder = localRigTransforms.rShoulder.position + offsetVec,
            //        posLElbow = localRigTransforms.lElbow.position + offsetVec,
            //        posRElbow = localRigTransforms.rElbow.position + offsetVec,
            //        posLWrist = localRigTransforms.lWrist.position + offsetVec,
            //        posRWrist = localRigTransforms.rWrist.position + offsetVec,

            //        rotMain = localRigTransforms.main.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRoot = localRigTransforms.root.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLHip = localRigTransforms.lHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRHip = localRigTransforms.rHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLKnee = localRigTransforms.lKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRKnee = localRigTransforms.rKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLAnkle = localRigTransforms.lAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRAnkle = localRigTransforms.rAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpine1 = localRigTransforms.spine1.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpine2 = localRigTransforms.spine2.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpineTop = localRigTransforms.spineTop.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLClavicle = localRigTransforms.lClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRClavicle = localRigTransforms.rClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotNeck = localRigTransforms.neck.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLShoulder = localRigTransforms.lShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRShoulder = localRigTransforms.rShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLElbow = localRigTransforms.lElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRElbow = localRigTransforms.rElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLWrist = localRigTransforms.lWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRWrist = localRigTransforms.rWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back)
            //    };

            //    serverId = SteamClient.SteamId;
            //    SteamNetworking.SendP2PPacket(SteamClient.SteamId, frtm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

            //    testRep.UpdateNameplateFacing(Camera.current.transform);
            //}

//#endif
        }

        public override void OnFixedUpdate()
        {
            //if (client.isConnected)
            //    client.Update();

            //if (isServer)
            //    server.Update();
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
                client.Disconnect();

            if (isServer)
                server.StopServer();
        }
    }
}
