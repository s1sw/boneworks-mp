using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Threading;
using NET_SDK;
using NET_SDK.Reflection;
using System.Linq;
using System.Net;
using System.IO;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using StressLevelZero.Interaction;
using UnityEngine.UI;
using RootMotion;
using RootMotion.FinalIK;
using Il2CppSystem.Reflection;
using UnityEngine.Animations;
using System.Collections.Concurrent;

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.7.0";
        public const string DownloadLink = "";
    }

    public enum MessageType
    {
        Join,
        PlayerPosition,
        OtherPlayerPosition,
        Disconnect,
        ServerShutdown,
        JoinRejected
    }

    public struct PlayerRep
    {
        public GameObject ford;
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
        public GameObject pelvis;
        public GameObject nametag;
        public IKSolverVR.Arm lArm;
        public IKSolverVR.Arm rArm;
        public IKSolverVR.Spine spine;
        public VRIK ik;
    }

    struct ServerMessageSendInfo
    {
        byte[] bytes;
        bool sendToAll;
        SteamId exceptOrTarget;
    }

    public class MultiplayerMod : MelonMod
    {
        private const int MAX_PLAYERS = 32;
        private const byte PROTOCOL_VERSION = 14;

        private bool isServer = false;
        private bool isClient = false;
        private SteamId serverId;

        private GameObject localHandL;
        private GameObject localHandR;
        private GameObject localHead;
        private GameObject localPelvis;
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MAX_PLAYERS);
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MAX_PLAYERS); // Server only
        private readonly List<SteamId> players = new List<SteamId>();
        private byte smallIdCounter = 1;
        private MultiplayerUI ui;

        private AssetBundle fordBundle;
        private GameObject fordPrefab;

        private ConcurrentQueue<ServerMessageSendInfo> sendQueue = new ConcurrentQueue<ServerMessageSendInfo>();

        private void DestroyPlayerRep(PlayerRep pr)
        {
            UnityEngine.Object.Destroy(pr.handL);
            UnityEngine.Object.Destroy(pr.handR);
            UnityEngine.Object.Destroy(pr.head);
            UnityEngine.Object.Destroy(pr.pelvis);
            UnityEngine.Object.Destroy(pr.ford);
        }

        private PlayerRep CreatePlayerRep()
        {
            GameObject ford = GameObject.Instantiate(fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>());

            // attempt to fix shaders
            foreach (SkinnedMeshRenderer smr in ford.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                MelonModLogger.Log(smr.gameObject.name);
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

            GameObject root = ford.transform.Find("Ford/Brett@neutral").gameObject;
            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt");
            var ik = root.AddComponent<VRIK>();

            VRIK.References bipedReferences = new VRIK.References();
            bipedReferences.root = root.transform.Find("SHJntGrp");

            bipedReferences.spine = realRoot.Find("Spine_01SHJnt");
            bipedReferences.pelvis = realRoot;

            bipedReferences.leftThigh = realRoot.Find("l_Leg_HipSHJnt");
            bipedReferences.leftCalf = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt");
            bipedReferences.leftFoot = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
            bipedReferences.leftToes = realRoot.Find("l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt/l_Leg_BallSHJnt");

            bipedReferences.rightThigh = realRoot.Find("r_Leg_HipSHJnt");
            bipedReferences.rightCalf = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt");
            bipedReferences.rightFoot = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
            bipedReferences.rightToes = realRoot.Find("r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt/r_Leg_BallSHJnt");

            bipedReferences.leftUpperArm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt");
            bipedReferences.leftForearm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt");
            bipedReferences.leftHand = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");

            bipedReferences.rightUpperArm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt");
            bipedReferences.rightForearm = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt");
            bipedReferences.rightHand = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");

            bipedReferences.head = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");

            ik.enabled = true;
            ik.references = bipedReferences;
            ik.solver.plantFeet = false;
            ik.fixTransforms = false;
            ik.solver.leftLeg.positionWeight = 1.0f;
            ik.solver.rightLeg.positionWeight = 1.0f;
            ik.solver.hasChest = false;
            ik.solver.spine.chestGoalWeight = 0.0f;
            ik.solver.spine.pelvisPositionWeight = 1.0f;
            ik.solver.spine.pelvisRotationWeight = 1.0f;
            IKSolverVR.Locomotion l = ik.solver.locomotion;
            l.weight = 0.0f;
            l.blockingEnabled = true;
            l.blockingLayers = LayerMask.NameToLayer("Default");
            l.footDistance = 0.3f;
            l.stepThreshold = 0.35f;
            l.angleThreshold = 60.0f;
            l.comAngleMlp = 0.5f;
            l.maxVelocity = 0.3f;
            l.velocityFactor = 0.3f;
            l.maxLegStretch = 0.98f;
            l.rootSpeed = 20.0f;
            l.stepSpeed = 2.8f;
            l.relaxLegTwistMinAngle = 20.0f;
            l.relaxLegTwistSpeed = 400.0f;
            l.stepInterpolation = InterpolationMode.InOutSine;
            l.offset = Vector3.zero;

            GameObject lHandTarget = new GameObject("LHand");
            GameObject rHandTarget = new GameObject("RHand");
            GameObject pelvisTarget = new GameObject("Pelvis");
            GameObject headTarget = new GameObject("HeadTarget");

            ik.solver.leftArm.target = lHandTarget.transform;
            ik.solver.rightArm.target = rHandTarget.transform;
            ik.solver.spine.pelvisTarget = pelvisTarget.transform;
            ik.solver.spine.headTarget = headTarget.transform;
            ik.solver.leftLeg.target = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/FootLTarget");
            ik.solver.rightLeg.target = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/FootRTarget");

            return new PlayerRep()
            {
                head = headTarget,
                handL = lHandTarget,
                handR = rHandTarget,
                pelvis = pelvisTarget,
                ford = ford,
                lArm = ik.solver.leftArm,
                rArm = ik.solver.rightArm,
                spine = ik.solver.spine,
                ik = ik
            };
        }

        private PlayerRep GetPlayerRep(byte id)
        {
            if (!playerObjects.ContainsKey(id))
                playerObjects.Add(id, CreatePlayerRep());

            return playerObjects[id];
        }

        private void DestroyPlayerRep(byte id)
        {
            PlayerRep pr = playerObjects[id];
            UnityEngine.Object.Destroy(pr.head);
            UnityEngine.Object.Destroy(pr.handL);
            UnityEngine.Object.Destroy(pr.handR);
        }

        public override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterPrefString("ConnectionInfo", "HostSteamID", "0");
            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;

            fordBundle = AssetBundle.LoadFromFile("ford.ford");
            if (fordBundle == null)
                MelonModLogger.LogError("Failed to load Ford asset bundle");

            fordPrefab = fordBundle.LoadAsset("Assets/brett_body.prefab").Cast<GameObject>();

            if (fordPrefab == null)
                MelonModLogger.LogError("Failed to load Ford prefab");
        }

        private void Ui_StartServer()
        {
            MelonModLogger.Log("Starting server...");
            isServer = true;

            localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
        }

        private void Connect(string obj)
        {
            MelonModLogger.Log("Starting client and connecting");

            serverId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);
            isClient = true;

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(PROTOCOL_VERSION);

            SteamNetworking.SendP2PPacket(serverId, msg.GetBytes());
            localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
        }

        public override void OnLevelWasLoaded(int level)
        {
            MelonModLogger.Log("Loaded scene " + level.ToString());
            if (level == 1)
            {
                //ui = new MultiplayerUI();
                //ui.SetState(MultiplayerUIState.PreConnect);
                //ui.Connect += Ui_Connect;
                //ui.StartServer += Ui_StartServer;
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            MelonModLogger.Log("Loaded scene " + level.ToString());

        }

        public void OnP2PSessionRequest(SteamId id)
        {
            if (isServer || id == serverId)
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
                MelonModLogger.Log("Accepted session for " + id.ToString());
            }
            else
            {
                MelonModLogger.Log("SteamID " + id + " tried to start a P2P session");
            }
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            if (error == P2PSessionError.NoRightsToApp)
            {
                MelonModLogger.LogError("You don't own the game on Steam.");
            }
            else if (error == P2PSessionError.Timeout)
            {
                MelonModLogger.LogError("Failed to connect.");
            }
            else
            {
                MelonModLogger.LogError("Unhandled P2P error: " + error.ToString());
            }
        }

        private bool useTestModel = false;
        private PlayerRep testRep;

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + serverId);
                SteamFriends.SetRichPresence("steam_player_group", serverId.ToString());
                Connect(ModPrefs.GetString("ConnectionInfo", "HostSteamID"));
                //ui.SetState(MultiplayerUIState.Client);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SteamFriends.SetRichPresence("steam_display", "Hosting multiplayer on " + SceneManager.GetActiveScene().name);
                SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + SteamClient.SteamId);
                SteamFriends.SetRichPresence("steam_player_group", SteamClient.SteamId.ToString());
                Ui_StartServer();
                //ui.SetState(MultiplayerUIState.Server);
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                useTestModel = true;
                testRep = CreatePlayerRep();
            }

            if (useTestModel)
            {
                Vector3 offsetVec = Camera.current.transform.forward;
                testRep.ford.transform.position = localPelvis.transform.position + offsetVec;
                testRep.handL.transform.position = localHandL.transform.position + offsetVec;
                testRep.handR.transform.position = localHandR.transform.position + offsetVec;
                testRep.pelvis.transform.position = localPelvis.transform.position + offsetVec;
                testRep.head.transform.position = localHead.transform.position + offsetVec;

                //testRep.ford.transform.rotation = localPelvis.transform.rotation;
                testRep.handL.transform.rotation = localHandL.transform.rotation;
                testRep.handR.transform.rotation = localHandR.transform.rotation;
                testRep.pelvis.transform.rotation = localPelvis.transform.rotation;
                testRep.head.transform.rotation = localHead.transform.rotation;
            }
        }

        private void ServerUpdate()
        {
            //ui.SetPlayerCount(players.Count);
            uint size;
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                if (packet.HasValue)
                {
                    P2PMessage msg = new P2PMessage(packet.Value.Data);

                    MessageType type = (MessageType)msg.ReadByte();

                    switch (type)
                    {
                        case MessageType.Join:
                            {
                                if (msg.ReadByte() != PROTOCOL_VERSION)
                                {
                                    // Somebody tried to join with an incompatible verison
                                    P2PMessage m2 = new P2PMessage();
                                    m2.WriteByte((byte)MessageType.JoinRejected);
                                    SteamNetworking.SendP2PPacket(packet.Value.SteamId, m2.GetBytes(), -1, 0, P2PSend.Reliable);
                                    SteamNetworking.CloseP2PSessionWithUser(packet.Value.SteamId);
                                }
                                else
                                {
                                    MelonModLogger.Log("Player joined with SteamID: " + packet.Value.SteamId);
                                    players.Add(packet.Value.SteamId);
                                    MelonModLogger.Log("Player count: " + players.Count);
                                    smallPlayerIds.Add(packet.Value.SteamId, smallIdCounter);
                                    smallIdCounter++;
                                }
                                break;
                            }
                        case MessageType.Disconnect:
                            {
                                MelonModLogger.Log("Player left with SteamID: " + packet.Value.SteamId);
                                byte smallId = smallPlayerIds[packet.Value.SteamId];

                                P2PMessage disconnectMsg = new P2PMessage();
                                disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                                disconnectMsg.WriteByte(smallId);
                                foreach (SteamId p in players)
                                {
                                    SteamNetworking.SendP2PPacket(p, disconnectMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                                }
                                DestroyPlayerRep(smallId);
                                break;
                            }
                        case MessageType.PlayerPosition:
                            {
                                if (smallPlayerIds.ContainsKey(packet.Value.SteamId))
                                {
                                    PlayerRep pr = GetPlayerRep(smallPlayerIds[packet.Value.SteamId]);

                                    PlayerPositionMessage ppm = new PlayerPositionMessage(msg);
                                    pr.head.transform.position = ppm.headPos;
                                    pr.handL.transform.position = ppm.lHandPos;
                                    pr.handR.transform.position = ppm.rHandPos;
                                    pr.pelvis.transform.position = ppm.pelvisPos;
                                    pr.head.transform.rotation = ppm.headRot;
                                    pr.handL.transform.rotation = ppm.lHandRot;
                                    pr.handR.transform.rotation = ppm.rHandRot;
                                    pr.pelvis.transform.rotation = ppm.pelvisRot;

                                    // Send to all other players

                                    OtherPlayerPositionMessage relayOPPM = new OtherPlayerPositionMessage();
                                    relayOPPM.headPos = ppm.headPos;
                                    relayOPPM.lHandPos = ppm.lHandPos;
                                    relayOPPM.rHandPos = ppm.rHandPos;
                                    relayOPPM.pelvisPos = ppm.pelvisPos;
                                    relayOPPM.headRot = ppm.headRot;
                                    relayOPPM.lHandRot = ppm.lHandRot;
                                    relayOPPM.rHandRot = ppm.rHandRot;
                                    relayOPPM.pelvisRot = ppm.pelvisRot;
                                    relayOPPM.playerId = smallPlayerIds[packet.Value.SteamId];

                                    ServerSendToAllExcept(relayOPPM, P2PSend.Unreliable, packet.Value.SteamId);
                                }
                                break;
                            }
                        default:
                            MelonModLogger.Log("Unknown message type");
                            break;
                    }

                }
            }

            OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage();
            oppm.playerId = 0;
            oppm.headPos = localHead.transform.position;
            oppm.lHandPos = localHandL.transform.position;
            oppm.rHandPos = localHandR.transform.position;
            oppm.pelvisPos = localPelvis.transform.position;

            oppm.headRot = localHead.transform.rotation;
            oppm.lHandRot = localHandL.transform.rotation;
            oppm.rHandRot = localHandR.transform.rotation;
            oppm.pelvisRot = localPelvis.transform.rotation;

            ServerSendToAll(oppm, P2PSend.Unreliable);
        }

        private void ClientUpdate()
        {
            uint size;
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                if (packet.HasValue)
                {
                    P2PMessage msg = new P2PMessage(packet.Value.Data);

                    MessageType type = (MessageType)msg.ReadByte();

                    switch (type)
                    {
                        case MessageType.OtherPlayerPosition:
                            {
                                OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage(msg);
                                PlayerRep pr = GetPlayerRep(oppm.playerId);

                                pr.head.transform.position = oppm.headPos;
                                pr.handL.transform.position = oppm.lHandPos;
                                pr.handR.transform.position = oppm.rHandPos;
                                pr.pelvis.transform.position = oppm.pelvisPos;

                                pr.head.transform.rotation = oppm.headRot;
                                pr.handL.transform.rotation = oppm.lHandRot;
                                pr.handR.transform.rotation = oppm.rHandRot;
                                pr.pelvis.transform.rotation = oppm.pelvisRot;

                                break;
                            }
                        case MessageType.ServerShutdown:
                            {
                                foreach (PlayerRep pr in playerObjects.Values)
                                {
                                    DestroyPlayerRep(pr);
                                }
                                break;
                            }
                        case MessageType.Disconnect:
                            {
                                byte pid = msg.ReadByte();
                                DestroyPlayerRep(playerObjects[pid]);
                                playerObjects.Remove(pid);
                                break;
                            }
                        case MessageType.JoinRejected:
                            {
                                MelonModLogger.Log("Join rejected - likely an incompatible version of the game.");
                                break;
                            }
                    }

                }
            }

            PlayerPositionMessage ppm = new PlayerPositionMessage();
            ppm.headPos = localHead.transform.position;
            ppm.lHandPos = localHandL.transform.position;
            ppm.rHandPos = localHandR.transform.position;
            ppm.pelvisPos = localPelvis.transform.position;

            ppm.headRot = localHead.transform.rotation;
            ppm.lHandRot = localHandL.transform.rotation;
            ppm.rHandRot = localHandR.transform.rotation;
            ppm.pelvisRot = localPelvis.transform.rotation;

            SendToServer(ppm, P2PSend.Unreliable);
        }

        override public void OnFixedUpdate()
        {
            if (isClient)
                ClientUpdate();

            if (isServer)
                ServerUpdate();
        }

        private void SendToServer(P2PMessage msg, P2PSend send)
        {
            SteamNetworking.SendP2PPacket(serverId, msg.GetBytes(), -1, 0, send);
        }

        private void SendToServer(INetworkMessage msg, P2PSend send)
        {
            SendToServer(msg.MakeMsg(), send);
        }

        private void ServerSendToAll(INetworkMessage msg, P2PSend send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                SteamNetworking.SendP2PPacket(p, pMsg.GetBytes(), -1, 0, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, P2PSend send, SteamId except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                if (p != except)
                    SteamNetworking.SendP2PPacket(p, pMsg.GetBytes(), -1, 0, send);
            }
        }

        public override void OnApplicationQuit()
        {
            if (isClient)
            {
                P2PMessage quitMsg = new P2PMessage();
                quitMsg.WriteByte((byte)MessageType.Disconnect);
                SendToServer(quitMsg, P2PSend.Reliable);
                SteamNetworking.CloseP2PSessionWithUser(serverId);
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
