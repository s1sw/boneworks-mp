using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod
{
    public partial class MultiplayerMod
    {
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MAX_PLAYERS); // Server only

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
                                    byte newPlayerId = smallIdCounter;
                                    smallPlayerIds.Add(packet.Value.SteamId, newPlayerId);
                                    smallIdCounter++;

                                    string name = msg.ReadUnicodeString();
                                    MelonModLogger.Log("Name: " + name);

                                    foreach (var smallId in playerNames.Keys)
                                    {
                                        OtherPlayerNameMessage opnm = new OtherPlayerNameMessage();
                                        opnm.playerId = smallId;
                                        opnm.name = playerNames[smallId];
                                        SteamNetworking.SendP2PPacket(packet.Value.SteamId, opnm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
                                    }

                                    OtherPlayerNameMessage opnm2 = new OtherPlayerNameMessage();
                                    opnm2.playerId = 0;
                                    opnm2.name = SteamClient.Name;
                                    SteamNetworking.SendP2PPacket(packet.Value.SteamId, opnm2.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

                                    playerNames.Add(newPlayerId, name);
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

                                playerObjects[smallId].Destroy();
                                playerObjects.Remove(smallId);
                                players.Remove(packet.Value.SteamId);
                                smallPlayerIds.Remove(packet.Value.SteamId);
                                break;
                            }
                        case MessageType.PlayerPosition:
                            {
                                if (smallPlayerIds.ContainsKey(packet.Value.SteamId))
                                {
                                    byte playerId = smallPlayerIds[packet.Value.SteamId];
                                    PlayerRep pr = GetPlayerRep(playerId, GetPlayerName(playerId));

                                    PlayerPositionMessage ppm = new PlayerPositionMessage(msg);
                                    pr.head.transform.position = ppm.headPos;
                                    pr.handL.transform.position = ppm.lHandPos;
                                    pr.handR.transform.position = ppm.rHandPos;
                                    pr.pelvis.transform.position = ppm.pelvisPos;
                                    pr.footL.transform.position = ppm.lFootPos;
                                    pr.footR.transform.position = ppm.rFootPos;

                                    pr.head.transform.rotation = ppm.headRot;
                                    pr.handL.transform.rotation = ppm.lHandRot;
                                    pr.handR.transform.rotation = ppm.rHandRot;
                                    pr.pelvis.transform.rotation = ppm.pelvisRot;
                                    pr.footL.transform.rotation = ppm.lFootRot;
                                    pr.footR.transform.rotation = ppm.rFootRot;

                                    // Send to all other players

                                    OtherPlayerPositionMessage relayOPPM = new OtherPlayerPositionMessage();
                                    relayOPPM.headPos = ppm.headPos;
                                    relayOPPM.lHandPos = ppm.lHandPos;
                                    relayOPPM.rHandPos = ppm.rHandPos;
                                    relayOPPM.pelvisPos = ppm.pelvisPos;
                                    relayOPPM.lFootPos = ppm.lFootPos;
                                    relayOPPM.rFootPos = ppm.rFootPos;

                                    relayOPPM.headRot = ppm.headRot;
                                    relayOPPM.lHandRot = ppm.lHandRot;
                                    relayOPPM.rHandRot = ppm.rHandRot;
                                    relayOPPM.pelvisRot = ppm.pelvisRot;
                                    relayOPPM.lFootRot = ppm.lFootRot;
                                    relayOPPM.rFootRot = ppm.rFootRot;
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

            if (localHead != null)
            {
                OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage();
                oppm.playerId = 0;
                oppm.headPos = localHead.transform.position;
                oppm.lHandPos = localHandL.transform.position;
                oppm.rHandPos = localHandR.transform.position;
                oppm.pelvisPos = localPelvis.transform.position;
                oppm.lFootPos = localFootL.transform.position;
                oppm.rFootPos = localFootR.transform.position;

                oppm.headRot = localHead.transform.rotation;
                oppm.lHandRot = localHandL.transform.rotation;
                oppm.rHandRot = localHandR.transform.rotation;
                oppm.pelvisRot = localPelvis.transform.rotation;
                oppm.lFootRot = localFootL.transform.rotation;
                oppm.rFootRot = localFootR.transform.rotation;

                ServerSendToAll(oppm, P2PSend.Unreliable);
            }

            foreach (PlayerRep pr in playerObjects.Values)
            {
                pr.UpdateNameplateFacing(Camera.current.transform);
            }
        }

        private void StartServer()
        {
            MelonModLogger.Log("Starting server...");
            isServer = true;
            localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
            localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
            localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
        }
    }
}
