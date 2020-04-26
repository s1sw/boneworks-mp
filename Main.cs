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

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.9.0";
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
        OtherHandGunChange
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
        public const byte PROTOCOL_VERSION = 28;

        private bool isServer = false;
        private bool enableFullRig = true;
        private SteamId serverId;

        private GameObject localHandL;
        private GameObject localHandR;
        private GameObject localHead;
        private GameObject localPelvis;
        private GameObject localFootL;
        private GameObject localFootR;

        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MAX_PLAYERS);
        private readonly List<SteamId> players = new List<SteamId>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(MAX_PLAYERS);

        private byte smallIdCounter = 1;
        private MultiplayerUI ui;
        private Discord.Discord discord;
        private readonly Client client = new Client();
        //private readonly Server server = new Server();
         
        private BoneworksRigTransforms localRigTransforms;

        private PlayerRep GetPlayerRep(byte id)
        {
            if (!playerObjects.ContainsKey(id))
                return null;
                //playerObjects.Add(id, new PlayerRep(name ?? id.ToString(), sId));

            return playerObjects[id];
        }

        private string GetPlayerName(byte id)
        {
            if (!playerNames.ContainsKey(id))
                return id.ToString();

            return playerNames[id];
        }

        public unsafe override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");
            ModPrefs.RegisterPrefBool("MPMod", "FullRig", false);
            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;
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

        private void PlayerHooks_OnPlayerLetGoObject(GameObject obj)
        {
            if (isServer)
            {
                HandGunChangeMessage hgcm = new HandGunChangeMessage()
                {
                    isForOtherPlayer = true,
                    destroy = true,
                    playerId = 0
                };

                ServerSendToAll(hgcm, P2PSend.Reliable);
            }
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject obj)
        {
            // See if it's a gun
            GunType? gt = BWUtil.GetGunType(obj.transform.root.gameObject);
            if (gt != null)
            {
                if (isServer)
                {
                    HandGunChangeMessage hgcm = new HandGunChangeMessage()
                    {
                        isForOtherPlayer = true,
                        type = gt.Value,
                        destroy = false,
                        playerId = 0
                    };

                    ServerSendToAll(hgcm, P2PSend.Reliable);
                }

                switch (gt)
                {
                    case GunType.EDER22:
                        MelonModLogger.Log("Holding Eder22");
                        break;
                }
            }
        }

        private void LogNull(Transform t, string name)
        {
            if (t == null)
            {
                MelonModLogger.Log(name + " was null");
            }
        }

        private void SetLocalRigTransforms()
        {
            GameObject root = GameObject.Find("[RigManager (Default Brett)]/[SkeletonRig (GameWorld Brett)]/Brett@neutral");
            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt");

            localRigTransforms = new BoneworksRigTransforms()
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

            LogNull(localRigTransforms.root, "root");
            LogNull(localRigTransforms.lHip, "lHip");
            LogNull(localRigTransforms.rHip, "rHip");
            LogNull(localRigTransforms.spine1, "spine1");
            LogNull(localRigTransforms.spine2, "spine2");
            LogNull(localRigTransforms.spineTop, "spineTop");
            LogNull(localRigTransforms.lClavicle, "lClavicle");
            LogNull(localRigTransforms.rClavicle, "rClavicle");
            LogNull(localRigTransforms.lShoulder, "lShoulder");
            LogNull(localRigTransforms.rShoulder, "rShoulder");
            LogNull(localRigTransforms.lElbow, "lElbow");
            LogNull(localRigTransforms.rElbow, "rElbow");
            LogNull(localRigTransforms.lWrist, "lWrist");
            LogNull(localRigTransforms.rWrist, "rWrist");
            LogNull(localRigTransforms.neck, "neck");
        }

        public override void OnLevelWasLoaded(int level)
        {
            localHandL = null;
            localHandR = null;
            localPelvis = null;
            localHead = null;
            localFootR = null;
            localFootL = null;

            MelonModLogger.Log("Loaded scene " + level.ToString());

            // Since the scene load destroys the player objects,
            // recreate them here!
            List<byte> ids = new List<byte>();
            List<SteamId> steamIds = new List<SteamId>();

            foreach (byte id in playerObjects.Keys)
            {
                ids.Add(id);
                steamIds.Add(playerObjects[id].steamId);
            }

            int i = 0;
            foreach (byte id in ids)
            {
                playerObjects[id] = new PlayerRep(playerNames[id], steamIds[i]);
            }

            if (isServer)
            {
                SceneTransitionMessage stm = new SceneTransitionMessage
                {
                    sceneByte = (byte)level
                };
                ServerSendToAll(stm, P2PSend.Reliable);
                StopServer();
                StartServer();
            }

            if (client.isConnected)
            {
                string id = serverId.ToString();
                client.Disconnect();
                client.Connect(id);
            }
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

            //localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            //localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            //localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            //localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
            //localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
            //localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
            //SetLocalRigTransforms();
        }

        public void OnP2PSessionRequest(SteamId id)
        {
            if (isServer || id == serverId)
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
                MelonModLogger.Log("Accepted session for " + id.ToString());
            }
            else if(client.isConnected)
            {
                MelonModLogger.Log("SteamID " + id + " tried to start a P2P session, but this is a client and they're not the server?!??");
            }
            else
            {
                MelonModLogger.Log("SteamID " + id + 
                    " tried to start a P2P session, but this isn't a client or a server. " +
                    "This is probably somebody trying to connect to you, but you haven't started the server. " +
                    "Make sure you click on the game window before pressing S.");
            }
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            if (error == P2PSessionError.NoRightsToApp)
            {
                MelonModLogger.LogError("You don't own the game on Steam.");
            }
            else if (error == P2PSessionError.NotRunningApp || error == P2PSessionError.Timeout)
            {
                // Probably a leaver
                if (isServer && smallPlayerIds.ContainsKey(id))
                {
                    MelonModLogger.Log("Player left with SteamID: " + id);
                    byte smallId = smallPlayerIds[id];

                    P2PMessage disconnectMsg = new P2PMessage();
                    disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                    disconnectMsg.WriteByte(smallId);

                    foreach (SteamId p in players)
                    {
                        SteamNetworking.SendP2PPacket(p, disconnectMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                    }

                    playerObjects[smallId].Destroy();
                    playerObjects.Remove(smallId);
                    players.Remove(id);
                    smallPlayerIds.Remove(id);
                }
                else if (client.isConnected && id == serverId)
                {
                    foreach (PlayerRep pr in playerObjects.Values)
                    {
                        pr.Destroy();
                    }
                }
                else
                {
                    MelonModLogger.LogError("P2PError: NotRunningApp");
                }
            }
            else
            {
                MelonModLogger.LogError("Unhandled P2P error: " + error.ToString());
            }
        }

        private bool useTestModel = false;
        private PlayerRep testRep;

        void ApplyTransformMessage<T>(PlayerRep pr, T tfMsg) where T : RigTFMsgBase
        {
            pr.rigTransforms.main.position = tfMsg.posMain;
            pr.rigTransforms.main.rotation = tfMsg.rotMain;

            pr.rigTransforms.root.position = tfMsg.posRoot;
            pr.rigTransforms.root.rotation = tfMsg.rotRoot;

            pr.rigTransforms.lHip.position = tfMsg.posLHip;
            pr.rigTransforms.lHip.rotation = tfMsg.rotLHip;

            pr.rigTransforms.rHip.position = tfMsg.posRHip;
            pr.rigTransforms.rHip.rotation = tfMsg.rotRHip;

            pr.rigTransforms.lAnkle.position = tfMsg.posLAnkle;
            pr.rigTransforms.lAnkle.rotation = tfMsg.rotLAnkle;

            pr.rigTransforms.rAnkle.position = tfMsg.posRAnkle;
            pr.rigTransforms.rAnkle.rotation = tfMsg.rotRAnkle;

            pr.rigTransforms.lKnee.position = tfMsg.posLKnee;
            pr.rigTransforms.lKnee.rotation = tfMsg.rotLKnee;

            pr.rigTransforms.rKnee.position = tfMsg.posRKnee;
            pr.rigTransforms.rKnee.rotation = tfMsg.rotRKnee;

            pr.rigTransforms.spine1.position = tfMsg.posSpine1;
            pr.rigTransforms.spine1.rotation = tfMsg.rotSpine1;

            pr.rigTransforms.spine2.position = tfMsg.posSpine2;
            pr.rigTransforms.spine2.rotation = tfMsg.rotSpine2;

            pr.rigTransforms.spineTop.position = tfMsg.posSpineTop;
            pr.rigTransforms.spineTop.rotation = tfMsg.rotSpineTop;

            pr.rigTransforms.lClavicle.position = tfMsg.posLClavicle;
            pr.rigTransforms.lClavicle.rotation = tfMsg.rotLClavicle;

            pr.rigTransforms.rClavicle.position = tfMsg.posRClavicle;
            pr.rigTransforms.rClavicle.rotation = tfMsg.rotRClavicle;

            pr.rigTransforms.neck.position = tfMsg.posNeck;
            pr.rigTransforms.neck.rotation = tfMsg.rotNeck;

            pr.rigTransforms.lShoulder.position = tfMsg.posLShoulder;
            pr.rigTransforms.lShoulder.rotation = tfMsg.rotLShoulder;

            pr.rigTransforms.rShoulder.position = tfMsg.posRShoulder;
            pr.rigTransforms.rShoulder.rotation = tfMsg.rotRShoulder;

            pr.rigTransforms.lElbow.position = tfMsg.posLElbow;
            pr.rigTransforms.lElbow.rotation = tfMsg.rotLElbow;

            pr.rigTransforms.rElbow.position = tfMsg.posRElbow;
            pr.rigTransforms.rElbow.rotation = tfMsg.rotRElbow;

            pr.rigTransforms.lWrist.position = tfMsg.posLWrist;
            pr.rigTransforms.lWrist.rotation = tfMsg.rotLWrist;

            pr.rigTransforms.rWrist.position = tfMsg.posRWrist;
            pr.rigTransforms.rWrist.rotation = tfMsg.rotRWrist;
        }

        public override void OnUpdate()
        {
            //rpcClient.Invoke();
            //discord.RunCallbacks();
            RichPresence.Update();
            if (!client.isConnected && !isServer)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + serverId);
                    SteamFriends.SetRichPresence("steam_player_group", serverId.ToString());
                    client.Connect(ModPrefs.GetString("ConnectionInfo", "HostSteamID"));
                    //SetLocalRigTransforms();

                    //ui.SetState(MultiplayerUIState.Client);
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    SteamFriends.SetRichPresence("steam_display", "Hosting multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + SteamClient.SteamId);
                    SteamFriends.SetRichPresence("steam_player_group", SteamClient.SteamId.ToString());
                    StartServer();
                    RichPresence.SetActivity(
                        new Activity() { 
                            Details = "Hosting a server",
                            Secrets = new ActivitySecrets()
                            {
                                Join = SteamClient.SteamId.ToString()
                            },
                            Party = new ActivityParty()
                            {
                                Id = SteamClient.SteamId.ToString() + "P",
                                Size = new PartySize()
                                {
                                    CurrentSize = 1,
                                    MaxSize = MAX_PLAYERS
                                }
                            }
                        });
                    //SetLocalRigTransforms();

                    //ui.SetState(MultiplayerUIState.Server);
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
                    StopServer();
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                useTestModel = true;
                testRep = new PlayerRep(SteamClient.Name, SteamClient.SteamId);
                smallPlayerIds.Add(SteamClient.SteamId, byte.MaxValue);
                playerObjects.Add(byte.MaxValue, testRep);
                localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
                localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
                localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
                localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
                localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
                localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
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

                    rotMain = localRigTransforms.main.rotation,
                    rotRoot = localRigTransforms.root.rotation,
                    rotLHip = localRigTransforms.lHip.rotation,
                    rotRHip = localRigTransforms.rHip.rotation,
                    rotLKnee = localRigTransforms.lKnee.rotation,
                    rotRKnee = localRigTransforms.rKnee.rotation,
                    rotLAnkle = localRigTransforms.lAnkle.rotation,
                    rotRAnkle = localRigTransforms.rAnkle.rotation,
                    rotSpine1 = localRigTransforms.spine1.rotation,
                    rotSpine2 = localRigTransforms.spine2.rotation,
                    rotSpineTop = localRigTransforms.spineTop.rotation,
                    rotLClavicle = localRigTransforms.lClavicle.rotation,
                    rotRClavicle = localRigTransforms.rClavicle.rotation,
                    rotNeck = localRigTransforms.neck.rotation,
                    rotLShoulder = localRigTransforms.lShoulder.rotation,
                    rotRShoulder = localRigTransforms.rShoulder.rotation,
                    rotLElbow = localRigTransforms.lElbow.rotation,
                    rotRElbow = localRigTransforms.rElbow.rotation,
                    rotLWrist = localRigTransforms.lWrist.rotation,
                    rotRWrist = localRigTransforms.rWrist.rotation
                };

                serverId = SteamClient.SteamId;
                SteamNetworking.SendP2PPacket(SteamClient.SteamId, frtm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

                testRep.UpdateNameplateFacing(Camera.current.transform);
            }
        }

        public override void OnFixedUpdate()
        {
            if (client.isConnected)
                client.Update();

            if (isServer)
                ServerUpdate();
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
            {
                client.Disconnect();
            }

            if (isServer)
            {
                P2PMessage shutdownMsg = new P2PMessage();
                shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

                foreach (SteamId p in players)
                {
                    SteamNetworking.SendP2PPacket(p, shutdownMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                    SteamNetworking.CloseP2PSessionWithUser(p);
                }
            }
        }
    }
}
