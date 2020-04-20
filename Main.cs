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
using Steamworks;
using StressLevelZero.Interaction;
using UnityEngine.UI;

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.3.0";
        public const string DownloadLink = "";
    }

    public enum MessageType
    {
        Join,
        PlayerPosition,
        OtherPlayerPosition
    }

    public struct PlayerRep
    {
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
    }

    public class P2PMessage
    {
        int length;
        byte[] bytes;
        int pos = 0;

        public P2PMessage(int length)
        {
            this.length = length;
            bytes = new byte[length];
        }

        public P2PMessage(byte[] bytes)
        {
            this.bytes = bytes;
            length = bytes.Length;
        }

        public byte[] GetBytes()
        {
            return bytes;
        }

        public void WriteByte(byte b)
        {
            bytes[pos] = b;
            pos += 1;
        }

        public void WriteFloat(float f)
        {
            BitConverter.GetBytes(f).CopyTo(bytes, pos);
            pos += sizeof(float);
        }

        public void WriteVector3(Vector3 v3)
        {
            WriteFloat(v3.x);
            WriteFloat(v3.y);
            WriteFloat(v3.z);
        }

        public void WriteUlong(ulong u)
        {
            BitConverter.GetBytes(u).CopyTo(bytes, pos);
            pos += sizeof(ulong);
        }

        public byte ReadByte()
        {
            byte v = bytes[pos];
            pos++;
            return v;
        }

        public float ReadFloat()
        {
            float v = BitConverter.ToSingle(bytes, pos);
            pos += sizeof(float);
            return v;
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        public ulong ReadUlong()
        {
            ulong id = BitConverter.ToUInt64(bytes, pos);
            pos += sizeof(ulong);
            return id;
        }
    }

    public class MultiplayerMod : MelonMod
    {
        private const int MAX_PLAYERS = 64;

        private bool isServer = false;
        private bool isClient = false;
        private SteamId serverId;

        private GameObject localHandL;
        private GameObject localHandR;
        private GameObject localHead;
        private Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MAX_PLAYERS);
        private Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MAX_PLAYERS); // Server only
        private List<SteamId> players = new List<SteamId>();
        private byte smallIdCounter = 1;
        private MultiplayerUI ui;

        private PlayerRep CreatePlayerRep()
        {
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            GameObject handL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handL.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            GameObject handR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handR.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            return new PlayerRep()
            {
                head = head,
                handL = handL,
                handR = handR
            };
        }

        private PlayerRep GetPlayerRep(byte id)
        {
            if (!playerObjects.ContainsKey(id))
                playerObjects.Add(id, CreatePlayerRep());

            return playerObjects[id];
        }

        public override void OnApplicationStart()
        {
            SteamClient.Init(823500);
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString());
            ModPrefs.RegisterPrefString("ConnectionInfo", "HostSteamID", "0");
            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            
        }

        private void Ui_StartServer()
        {
            MelonModLogger.Log("Starting server...");
            isServer = true;

            localHandL = GameObject.Find("[SkeletonRig (Realtime SkeleBones)]/Hand (left)");
            localHandR = GameObject.Find("[SkeletonRig (Realtime SkeleBones)]/Hand (right)");
            localHead = Camera.current.gameObject;
        }

        private void Ui_Connect(string obj)
        {
            MelonModLogger.Log("Starting client and connecting");

            serverId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);
            isClient = true;

            P2PMessage msg = new P2PMessage(sizeof(byte));
            msg.WriteByte((byte)MessageType.Join);

            SteamNetworking.SendP2PPacket(serverId, msg.GetBytes());
            localHandL = GameObject.Find("[SkeletonRig (Realtime SkeleBones)]/Hand (left)");
            localHandR = GameObject.Find("[SkeletonRig (Realtime SkeleBones)]/Hand (right)");
            localHead = Camera.current.gameObject;
        }

        public override void OnLevelWasLoaded(int level)
        {
            MelonModLogger.Log("Loaded scene " + level.ToString());
            if (level == 1)
            {
                //ui = new MultiplayerUI();
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
            SteamNetworking.AcceptP2PSessionWithUser(id);
            MelonModLogger.Log("Accepted session for " + id.ToString());
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Ui_Connect(ModPrefs.GetString("ConnectionInfo", "HostSteamID"));
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Ui_StartServer();
            }

            if (isServer)
                ServerUpdate();

            if (isClient)
                ClientUpdate();
        }

        private void ServerUpdate()
        {
            //ui.SetPlayerCount(players.Count);

            try
            {
                uint size;
                while (SteamNetworking.IsP2PPacketAvailable(0))
                {
                    Steamworks.Data.P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                    if (packet.HasValue)
                    {
                        P2PMessage msg = new P2PMessage(packet.Value.Data);

                        MessageType type = (MessageType)msg.ReadByte();

                        switch (type)
                        {
                            case MessageType.Join:
                                MelonModLogger.Log("Player joined with SteamID: " + packet.Value.SteamId);
                                players.Add(packet.Value.SteamId);
                                MelonModLogger.Log("Player count: " + players.Count);
                                smallPlayerIds.Add(packet.Value.SteamId, smallIdCounter);
                                smallIdCounter++;
                                break;
                            case MessageType.PlayerPosition:
                                {
                                    PlayerRep pr = GetPlayerRep(smallPlayerIds[packet.Value.SteamId]);
                                    Vector3 pos = msg.ReadVector3();
                                    Vector3 hPosL = msg.ReadVector3();
                                    Vector3 hPosR = msg.ReadVector3();
                                    pr.head.transform.position = pos;
                                    pr.handL.transform.position = hPosL;
                                    pr.handR.transform.position = hPosR;

                                    // Send to all other players

                                    P2PMessage sendMsg = new P2PMessage((sizeof(byte) * 2) + (sizeof(float) * 9));
                                    sendMsg.WriteByte((byte)MessageType.OtherPlayerPosition);
                                    sendMsg.WriteByte(smallPlayerIds[packet.Value.SteamId]);
                                    sendMsg.WriteVector3(pos);
                                    sendMsg.WriteVector3(hPosL);
                                    sendMsg.WriteVector3(hPosR);

                                    foreach (SteamId id in players)
                                    {
                                        if (id != packet.Value.SteamId)
                                            SteamNetworking.SendP2PPacket(id, sendMsg.GetBytes(), -1, 0, P2PSend.Unreliable);
                                    }

                                    break;
                                }
                            default:
                                MelonModLogger.Log("Unknown message type");
                                break;
                        }

                    }
                }

                P2PMessage headMsg = new P2PMessage((sizeof(byte) * 2) + (sizeof(float) * 9));
                headMsg.WriteByte((byte)MessageType.OtherPlayerPosition);
                headMsg.WriteByte(0);
                headMsg.WriteVector3(localHead.transform.position);
                headMsg.WriteVector3(localHandL.transform.position);
                headMsg.WriteVector3(localHandR.transform.position);

                foreach (SteamId id in players)
                {
                    SteamNetworking.SendP2PPacket(id, headMsg.GetBytes(), -1, 0, P2PSend.Unreliable);
                }
            }
            catch (Exception e)
            {
                MelonModLogger.LogError("error you fool: " + e.StackTrace.ToString());
            }
        }

        private void ClientUpdate()
        {
            uint size;
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                Steamworks.Data.P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                if (packet.HasValue)
                {
                    P2PMessage msg = new P2PMessage(packet.Value.Data);

                    MessageType type = (MessageType)msg.ReadByte();

                    switch (type)
                    {
                        case MessageType.OtherPlayerPosition:
                            {
                                byte pid = msg.ReadByte();
                                PlayerRep pr = GetPlayerRep(pid);
                                Vector3 pos = msg.ReadVector3();
                                Vector3 hPosL = msg.ReadVector3();
                                Vector3 hPosR = msg.ReadVector3();
                                pr.head.transform.position = pos;
                                pr.handL.transform.position = hPosL;
                                pr.handR.transform.position = hPosR;

                                break;
                            }
                    }

                }
            }

            P2PMessage headMsg = new P2PMessage((sizeof(byte) * 2) + (sizeof(float) * 9));
            headMsg.WriteByte((byte)MessageType.PlayerPosition);
            headMsg.WriteVector3(localHead.transform.position);
            headMsg.WriteVector3(localHandL.transform.position);
            headMsg.WriteVector3(localHandR.transform.position);
            SteamNetworking.SendP2PPacket(serverId, headMsg.GetBytes(), -1, 0, P2PSend.Unreliable);
        }

        public override void OnApplicationQuit()
        {

        }
    }
}
