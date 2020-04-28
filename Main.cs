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
        SetPartyId
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
        private bool enableFullRig = true;

        private MultiplayerUI ui;
        private readonly Client client = new Client();
        private readonly Server server = new Server();

        internal static event Action<int> OnLevelWasLoadedEvent;
        internal static event Action<int> OnLevelWasInitializedEvent;

        public unsafe override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");
            SteamNetworking.AllowP2PPacketRelay(true);
            ui = new MultiplayerUI();
            PlayerRep.LoadFord();

            PlayerRep.hideBody = false;
            enableFullRig = true;

            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

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

#if DEBUG
            if (Input.GetKeyDown(KeyCode.N))
            {
                useTestModel = true;
                testRep = new PlayerRep(SteamClient.Name, SteamClient.SteamId);
                smallPlayerIds.Add(SteamClient.SteamId, byte.MaxValue);
                playerObjects.Add(byte.MaxValue, testRep);
                HandGunChangeMessage hgcm = new HandGunChangeMessage
                {
                    isForOtherPlayer = false,
                    playerId = byte.MaxValue,
                    type = GunType.EDER22,
                    destroy = false
                };
                serverId = SteamClient.SteamId;

                //SendToServer(hgcm, P2PSend.Reliable);
                SteamNetworking.SendP2PPacket(SteamClient.SteamId, hgcm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                foreach (var cs in FindObjectsOfType<ClaimedSpawner>())
                {
                    MelonModLogger.Log(cs.spawnObject.title + ": " + cs.spawnObject.prefab.GetInstanceID());
                }
            }

            // Horrid janky testing for bodies
            // Requires the server to be started first!

            if (Input.GetKeyDown(KeyCode.M))
            {
                GameObject go = Instantiate(FindObjectFromInstanceID(22072)).Cast<GameObject>();
                go.transform.position = Camera.current.transform.position + Camera.current.transform.forward;
                MelonModLogger.Log(go.name);
                Gun gun = go.GetComponent<Gun>();
                for (int i = 0; i < 15; i++)
                {
                    GameObject magazineObj = Instantiate(gun.spawnableMagazine.prefab);
                    magazineObj.transform.position = Camera.current.transform.position + Camera.current.transform.forward + Vector3.up;
                }
            }

            if (Input.GetKeyDown(KeyCode.P))
            {

            }

            if (useTestModel)
            {
                Vector3 offsetVec = new Vector3(0.0f, 0.0f, 1.0f);

                FullRigTransformMessage frtm = new FullRigTransformMessage
                {
                    posMain = localRigTransforms.main.position + offsetVec,
                    posRoot = localRigTransforms.root.position + offsetVec,
                    posLHip = localRigTransforms.lHip.position + offsetVec,
                    posRHip = localRigTransforms.rHip.position + offsetVec,
                    posLKnee = localRigTransforms.lKnee.position + offsetVec,
                    posRKnee = localRigTransforms.rKnee.position + offsetVec,
                    posLAnkle = localRigTransforms.lAnkle.position + offsetVec,
                    posRAnkle = localRigTransforms.rAnkle.position + offsetVec,

                    posSpine1 = localRigTransforms.spine1.position + offsetVec,
                    posSpine2 = localRigTransforms.spine2.position + offsetVec,
                    posSpineTop = localRigTransforms.spineTop.position + offsetVec,
                    posLClavicle = localRigTransforms.lClavicle.position + offsetVec,
                    posRClavicle = localRigTransforms.rClavicle.position + offsetVec,
                    posNeck = localRigTransforms.neck.position + offsetVec,
                    posLShoulder = localRigTransforms.lShoulder.position + offsetVec,
                    posRShoulder = localRigTransforms.rShoulder.position + offsetVec,
                    posLElbow = localRigTransforms.lElbow.position + offsetVec,
                    posRElbow = localRigTransforms.rElbow.position + offsetVec,
                    posLWrist = localRigTransforms.lWrist.position + offsetVec,
                    posRWrist = localRigTransforms.rWrist.position + offsetVec,

                    rotMain = localRigTransforms.main.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRoot = localRigTransforms.root.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLHip = localRigTransforms.lHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRHip = localRigTransforms.rHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLKnee = localRigTransforms.lKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRKnee = localRigTransforms.rKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLAnkle = localRigTransforms.lAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRAnkle = localRigTransforms.rAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotSpine1 = localRigTransforms.spine1.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotSpine2 = localRigTransforms.spine2.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotSpineTop = localRigTransforms.spineTop.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLClavicle = localRigTransforms.lClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRClavicle = localRigTransforms.rClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotNeck = localRigTransforms.neck.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLShoulder = localRigTransforms.lShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRShoulder = localRigTransforms.rShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLElbow = localRigTransforms.lElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRElbow = localRigTransforms.rElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotLWrist = localRigTransforms.lWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
                    rotRWrist = localRigTransforms.rWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back)
                };

                serverId = SteamClient.SteamId;
                SteamNetworking.SendP2PPacket(SteamClient.SteamId, frtm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

                testRep.UpdateNameplateFacing(Camera.current.transform);
            }

#endif
        }

        public override void OnFixedUpdate()
        {
            if (client.isConnected)
                client.Update();

            if (isServer)
                server.Update();
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
