using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerMod
{
    public partial class MultiplayerMod
    {
        private void Connect(string obj)
        {
            MelonModLogger.Log("Starting client and connecting");

            serverId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);
            isClient = true;

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(PROTOCOL_VERSION);
            msg.WriteUnicodeString(SteamClient.Name);

            SteamNetworking.SendP2PPacket(serverId, msg.GetBytes());

            PlayerNameMessage pnm = new PlayerNameMessage
            {
                name = SteamClient.Name
            };
            SendToServer(pnm, P2PSend.Reliable);

            localHandL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            localHandR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            localPelvis = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt");
            localHead = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/Neck_01SHJnt");
            localFootR = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/r_Leg_HipSHJnt/r_Leg_KneeSHJnt/r_Leg_AnkleSHJnt");
            localFootL = GameObject.Find("[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt/ROOTSHJnt/l_Leg_HipSHJnt/l_Leg_KneeSHJnt/l_Leg_AnkleSHJnt");
        }

        private void Disconnect()
        {
            MelonModLogger.Log("Disconnecting...");
            isClient = false;
            serverId = 0;
            playerObjects.Clear();
            playerNames.Clear();

            SteamNetworking.CloseP2PSessionWithUser(serverId);
        }

        private void ClientUpdate()
        {
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
                                pr.ford.transform.position = oppm.pelvisPos - new Vector3(0.0f, 0.3f, 0.0f);
                                pr.footL.transform.position = oppm.lFootPos;
                                pr.footR.transform.position = oppm.rFootPos;

                                pr.head.transform.rotation = oppm.headRot;
                                pr.handL.transform.rotation = oppm.lHandRot;
                                pr.handR.transform.rotation = oppm.rHandRot;
                                pr.pelvis.transform.rotation = oppm.pelvisRot;
                                pr.footL.transform.rotation = oppm.lFootRot;
                                pr.footR.transform.rotation = oppm.rFootRot;

                                break;
                            }
                        case MessageType.ServerShutdown:
                            {
                                foreach (PlayerRep pr in playerObjects.Values)
                                {
                                    pr.Destroy();
                                }
                                break;
                            }
                        case MessageType.Disconnect:
                            {
                                byte pid = msg.ReadByte();
                                playerObjects[pid].Destroy();
                                playerObjects.Remove(pid);
                                break;
                            }
                        case MessageType.JoinRejected:
                            {
                                MelonModLogger.Log("Join rejected - likely an incompatible version of the game.");
                                break;
                            }
                        case MessageType.OtherPlayerName:
                            {
                                OtherPlayerNameMessage opnm = new OtherPlayerNameMessage(msg);
                                PlayerRep pr = GetPlayerRep(opnm.playerId, opnm.name);
                                pr.namePlate.GetComponent<TMPro.TextMeshPro>().text = opnm.name;
                                playerNames.Add(opnm.playerId, opnm.name);
                                break;
                            }
                        case MessageType.SceneTransition:
                            {
                                SceneTransitionMessage stm = new SceneTransitionMessage(msg);
                                SceneManager.LoadScene(stm.sceneByte);
                                break;
                            }
                    }
                }
            }

            if (localHead != null)
            {
                PlayerPositionMessage ppm = new PlayerPositionMessage
                {
                    headPos = localHead.transform.position,
                    lHandPos = localHandL.transform.position,
                    rHandPos = localHandR.transform.position,
                    pelvisPos = localPelvis.transform.position,
                    lFootPos = localFootL.transform.position,
                    rFootPos = localFootR.transform.position,

                    headRot = localHead.transform.rotation,
                    lHandRot = localHandL.transform.rotation,
                    rHandRot = localHandR.transform.rotation,
                    pelvisRot = localPelvis.transform.rotation,
                    lFootRot = localFootL.transform.rotation,
                    rFootRot = localFootR.transform.rotation
                };

                SendToServer(ppm, P2PSend.Unreliable);

                foreach (PlayerRep pr in playerObjects.Values)
                {
                    pr.UpdateNameplateFacing(Camera.current.transform);
                }
            }   
        }
    }
}
