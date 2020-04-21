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
using Utilties;
using StressLevelZero.Utilities;
//using DiscordRPC;
//using DiscordRPC.Logging;

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.7.1";
        public const string DownloadLink = "https://discord.gg/2Wn3N2P";
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
        SceneTransition
    }

    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        private const int MAX_PLAYERS = 32;
        private const byte PROTOCOL_VERSION = 20;

        private bool isServer = false;
        private bool isClient = false;
        private SteamId serverId;

        private GameObject localHandL;
        private GameObject localHandR;
        private GameObject localHead;
        private GameObject localPelvis;
        private GameObject localFootL;
        private GameObject localFootR;

        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MAX_PLAYERS);
        private readonly List<SteamId> players = new List<SteamId>();
        private byte smallIdCounter = 1;
        private MultiplayerUI ui;
        //private DiscordRpcClient rpcClient;

        private PlayerRep GetPlayerRep(byte id, string name=null)
        {
            if (!playerObjects.ContainsKey(id))
                playerObjects.Add(id, new PlayerRep(name ?? id.ToString()));

            return playerObjects[id];
        }

        private string GetPlayerName(byte id)
        {
            if (!playerNames.ContainsKey(id))
                return id.ToString();

            return playerNames[id];
        }

        public override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterPrefString("ConnectionInfo", "HostSteamID", "0");
            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;
            ui = new MultiplayerUI();
            PlayerRep.LoadFord();

            //rpcClient = new DiscordRpcClient("701895326600265879", 0, new ConsoleLogger() { Level = LogLevel.Trace }, false);

            //rpcClient.OnReady += (sender, e) =>
            //{
            //    MelonModLogger.Log("Initialised RPC as " + e.User.Username);

            //    rpcClient.SetPresence(new RichPresence()
            //    {
            //        Details = "On " + SceneManager.GetActiveScene().name,
            //        State = "Idle"
            //    });
            //};

            //rpcClient.Initialize();

            //rpcClient.OnJoinRequested += RpcClient_OnJoinRequested;
            //rpcClient.OnJoin += RpcClient_OnJoin;
            //rpcClient.Subscribe(DiscordRPC.EventType.JoinRequest);
        }

        //private void RpcClient_OnJoin(object sender, DiscordRPC.Message.JoinMessage args)
        //{
        //    Connect(args.Secret);
        //}

        //private void RpcClient_OnJoinRequested(object sender, DiscordRPC.Message.JoinRequestMessage args)
        //{
        //    // TODO
        //    rpcClient.Respond(args, true);
        //}

        public override void OnLevelWasLoaded(int level)
        {
            localHandL = null;
            localHandR = null;
            localPelvis = null;
            localHead = null;
            localFootR = null;
            localFootL = null;

            MelonModLogger.Log("Loaded scene " + level.ToString());

            //rpcClient.SetPresence(new RichPresence()
            //{
            //    Details = "On " + SceneManager.GetActiveScene().name,
            //    State = "Idle"
            //});
            // Since the scene load destroys the player objects,
            // recreate them here!
            List<byte> ids = new List<byte>();

            foreach (byte id in playerObjects.Keys)
            {
                ids.Add(id);
            }

            foreach (byte id in ids)
            {
                playerObjects[id] = new PlayerRep(playerNames[id]);
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

            if (isClient)
            {
                string id = serverId.ToString();
                Disconnect();
                Connect(id);
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            ui.Recreate();
            MelonModLogger.Log("Initialized scene " + level.ToString());

            localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
            localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
            localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
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
                else if (isClient && id == serverId)
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

        public override void OnUpdate()
        {
            //rpcClient.Invoke();
            if (!isClient && !isServer)
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
                    StartServer();
                    //ui.SetState(MultiplayerUIState.Server);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    Disconnect();
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
                testRep = new PlayerRep(SteamClient.Name);
                localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
                localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
                localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
                localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
                localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
                localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
            }

            if (useTestModel)
            {
                Vector3 offsetVec = new Vector3(0.0f, 0.0f, 1.0f);
                testRep.ford.transform.position = localPelvis.transform.position + offsetVec - new Vector3(0.0f, 0.3f, 0.0f);
                testRep.handL.transform.position = localHandL.transform.position + offsetVec;
                testRep.handR.transform.position = localHandR.transform.position + offsetVec;
                testRep.pelvis.transform.position = localPelvis.transform.position + offsetVec;
                testRep.head.transform.position = localHead.transform.position + offsetVec;
                testRep.footL.transform.position = localFootL.transform.position + offsetVec;
                testRep.footR.transform.position = localFootR.transform.position + offsetVec;

                //testRep.ford.transform.rotation = localPelvis.transform.rotation;
                testRep.handL.transform.rotation = localHandL.transform.rotation;
                testRep.handR.transform.rotation = localHandR.transform.rotation;
                testRep.pelvis.transform.rotation = localPelvis.transform.rotation;
                testRep.head.transform.rotation = localHead.transform.rotation;
                testRep.footL.transform.rotation = localFootL.transform.rotation;
                testRep.footR.transform.rotation = localFootR.transform.rotation;

                testRep.UpdateNameplateFacing(Camera.current.transform);
            }
        }

        public override void OnFixedUpdate()
        {
            if (isClient)
                ClientUpdate();

            if (isServer)
                ServerUpdate();
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
