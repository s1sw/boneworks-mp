using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using Oculus.Platform.Samples.VrHoops;
using StressLevelZero.Props.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Object;

namespace MultiplayerMod
{
    public partial class MultiplayerMod
    {
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MAX_PLAYERS);

        private void ServerUpdate()
        {
            //ui.SetPlayerCount(players.Count);
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
                                    largePlayerIds.Add(newPlayerId, packet.Value.SteamId);
                                    smallIdCounter++;

                                    string name = msg.ReadUnicodeString();
                                    MelonModLogger.Log("Name: " + name);

                                    foreach (var smallId in playerNames.Keys)
                                    {
                                        ClientJoinMessage cjm = new ClientJoinMessage
                                        {
                                            playerId = smallId,
                                            name = playerNames[smallId],
                                            steamId = largePlayerIds[smallId]
                                        };
                                        SteamNetworking.SendP2PPacket(packet.Value.SteamId, cjm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
                                    }

                                    ClientJoinMessage cjm2 = new ClientJoinMessage
                                    {
                                        playerId = 0,
                                        name = SteamClient.Name,
                                        steamId = SteamClient.SteamId
                                    };
                                    SteamNetworking.SendP2PPacket(packet.Value.SteamId, cjm2.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

                                    playerNames.Add(newPlayerId, name);

                                    ClientJoinMessage cjm3 = new ClientJoinMessage
                                    {
                                        playerId = newPlayerId,
                                        name = name,
                                        steamId = packet.Value.SteamId
                                    };
                                    ServerSendToAllExcept(cjm3, P2PSend.Reliable, packet.Value.SteamId);

                                    playerObjects.Add(newPlayerId, new PlayerRep(name, packet.Value.SteamId));

                                    RichPresence.SetActivity(
                                        new Activity()
                                        {
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
                                                    CurrentSize = players.Count + 1,
                                                    MaxSize = MAX_PLAYERS
                                                }
                                            }
                                        });
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
                        //case MessageType.PlayerPosition:
                        //    {
                        //        if (smallPlayerIds.ContainsKey(packet.Value.SteamId))
                        //        {
                        //            byte playerId = smallPlayerIds[packet.Value.SteamId];
                        //            PlayerRep pr = GetPlayerRep(playerId);

                        //            PlayerPositionMessage ppm = new PlayerPositionMessage(msg);
                        //            pr.head.transform.position = ppm.headPos;
                        //            pr.handL.transform.position = ppm.lHandPos;
                        //            pr.handR.transform.position = ppm.rHandPos;
                        //            pr.ford.transform.position = ppm.pelvisPos - new Vector3(0.0f, 0.3f, 0.0f);
                        //            pr.pelvis.transform.position = ppm.pelvisPos;
                        //            pr.footL.transform.position = ppm.lFootPos;
                        //            pr.footR.transform.position = ppm.rFootPos;

                        //            //pr.ford.transform.rotation = ppm.pelvisRot;
                        //            pr.head.transform.rotation = ppm.headRot;
                        //            pr.handL.transform.rotation = ppm.lHandRot;
                        //            pr.handR.transform.rotation = ppm.rHandRot;
                        //            pr.pelvis.transform.rotation = ppm.pelvisRot;
                        //            pr.footL.transform.rotation = ppm.lFootRot;
                        //            pr.footR.transform.rotation = ppm.rFootRot;

                        //            // Send to all other players

                        //            OtherPlayerPositionMessage relayOPPM = new OtherPlayerPositionMessage
                        //            {
                        //                headPos = ppm.headPos,
                        //                lHandPos = ppm.lHandPos,
                        //                rHandPos = ppm.rHandPos,
                        //                pelvisPos = ppm.pelvisPos,
                        //                lFootPos = ppm.lFootPos,
                        //                rFootPos = ppm.rFootPos,

                        //                headRot = ppm.headRot,
                        //                lHandRot = ppm.lHandRot,
                        //                rHandRot = ppm.rHandRot,
                        //                pelvisRot = ppm.pelvisRot,
                        //                lFootRot = ppm.lFootRot,
                        //                rFootRot = ppm.rFootRot,
                        //                playerId = smallPlayerIds[packet.Value.SteamId]
                        //            };

                        //            ServerSendToAllExcept(relayOPPM, P2PSend.Unreliable, packet.Value.SteamId);
                        //        }
                        //        break;
                        //    }
                        case MessageType.FullRig:
                            {
                                FullRigTransformMessage frtm = new FullRigTransformMessage(msg);

                                byte playerId = smallPlayerIds[packet.Value.SteamId];
                                if (playerObjects.ContainsKey(playerId) && enableFullRig)
                                {
                                    PlayerRep pr = GetPlayerRep(playerId);

                                    //ApplyTransformMessage(pr, frtm);
                                    pr.ApplyTransformMessage(frtm);

                                    OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                                    {
                                        playerId = playerId,

                                        posMain = frtm.posMain,
                                        posRoot = frtm.posRoot,
                                        posLHip = frtm.posLHip,
                                        posRHip = frtm.posRHip,
                                        posLKnee = frtm.posLKnee,
                                        posRKnee = frtm.posRKnee,
                                        posLAnkle = frtm.posLAnkle,
                                        posRAnkle = frtm.posRAnkle,

                                        posSpine1 = frtm.posSpine1,
                                        posSpine2 = frtm.posSpine2,
                                        posSpineTop = frtm.posSpineTop,
                                        posLClavicle = frtm.posLClavicle,
                                        posRClavicle = frtm.posRClavicle,
                                        posNeck = frtm.posNeck,
                                        posLShoulder = frtm.posLShoulder,
                                        posRShoulder = frtm.posRShoulder,
                                        posLElbow = frtm.posLElbow,
                                        posRElbow = frtm.posRElbow,
                                        posLWrist = frtm.posLWrist,
                                        posRWrist = frtm.posRWrist,

                                        rotMain = frtm.rotMain,
                                        rotRoot = frtm.rotRoot,
                                        rotLHip = frtm.rotLHip,
                                        rotRHip = frtm.rotRHip,
                                        rotLKnee = frtm.rotLKnee,
                                        rotRKnee = frtm.rotRKnee,
                                        rotLAnkle = frtm.rotLAnkle,
                                        rotRAnkle = frtm.rotRAnkle,
                                        rotSpine1 = frtm.rotSpine1,
                                        rotSpine2 = frtm.rotSpine2,
                                        rotSpineTop = frtm.rotSpineTop,
                                        rotLClavicle = frtm.rotLClavicle,
                                        rotRClavicle = frtm.rotRClavicle,
                                        rotNeck = frtm.rotNeck,
                                        rotLShoulder = frtm.rotLShoulder,
                                        rotRShoulder = frtm.rotRShoulder,
                                        rotLElbow = frtm.rotLElbow,
                                        rotRElbow = frtm.rotRElbow,
                                        rotLWrist = frtm.rotLWrist,
                                        rotRWrist = frtm.rotRWrist
                                    };

                                    ServerSendToAllExcept(ofrtm, P2PSend.Unreliable, packet.Value.SteamId);
                                }
                                break;
                            }
                        case MessageType.HandGunChange:
                            {
                                HandGunChangeMessage hgcm = new HandGunChangeMessage(msg, false)
                                {
                                    playerId = smallPlayerIds[packet.Value.SteamId],
                                    isForOtherPlayer = false
                                };

                                MelonModLogger.Log("Got HGC: " + hgcm.type.ToString() + ", destroy: " + hgcm.destroy.ToString());

                                if (hgcm.destroy)
                                {
                                    Destroy(playerObjects[smallPlayerIds[packet.Value.SteamId]].currentGun);
                                }
                                else
                                {
                                    MelonModLogger.Log("Spawning " + hgcm.type.ToString());
                                    PlayerRep pr = playerObjects[smallPlayerIds[packet.Value.SteamId]];
                                    pr.currentGun = BWUtil.SpawnGun(hgcm.type);
                                    if (pr.currentGun == null)
                                        MelonModLogger.LogError("Failed to spawn gun");
                                    pr.currentGun.transform.parent = pr.gunParent.transform;
                                    pr.currentGun.transform.localPosition = Vector3.zero;
                                    pr.currentGun.transform.localRotation = Quaternion.identity;//Quaternion.AngleAxis(90.0f, new Vector3(0.0f, 1.0f, 0.0f)) * Quaternion.AngleAxis(90.0f, new Vector3(1.0f, 0.0f, 0.0f));
                                    if (pr.currentGun.GetComponentInChildren<Rigidbody>() == null)
                                        MelonModLogger.LogError("wefijhewkfhwekjfhew");
                                    pr.currentGun.GetComponentInChildren<Rigidbody>().isKinematic = true;
                                }

                                hgcm.isForOtherPlayer = true;
                                ServerSendToAllExcept(hgcm, P2PSend.Reliable, packet.Value.SteamId);
                                break;
                            }
                        default:
                            MelonModLogger.Log("Unknown message type: " + type.ToString());
                            break;
                    }

                }
            }

            //if (localHead != null && !enableFullRig)
            //{
            //    OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage
            //    {
            //        playerId = 0,
            //        headPos = localHead.transform.position,
            //        lHandPos = localHandL.transform.position,
            //        rHandPos = localHandR.transform.position,
            //        pelvisPos = localPelvis.transform.position,
            //        lFootPos = localFootL.transform.position,
            //        rFootPos = localFootR.transform.position,

            //        headRot = localHead.transform.rotation,
            //        lHandRot = localHandL.transform.rotation,
            //        rHandRot = localHandR.transform.rotation,
            //        pelvisRot = localPelvis.transform.rotation,
            //        lFootRot = localFootL.transform.rotation,
            //        rFootRot = localFootR.transform.rotation
            //    };

            //    ServerSendToAll(oppm, P2PSend.Unreliable);
            //} 
            //else if(enableFullRig && localHead != null)
            {
                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                {
                    playerId = 0,
                    posMain = localRigTransforms.main.position,
                    posRoot = localRigTransforms.root.position,
                    posLHip = localRigTransforms.lHip.position,
                    posRHip = localRigTransforms.rHip.position,
                    posLKnee = localRigTransforms.lKnee.position,
                    posRKnee = localRigTransforms.rKnee.position,
                    posLAnkle = localRigTransforms.lAnkle.position,
                    posRAnkle = localRigTransforms.rAnkle.position,

                    posSpine1 = localRigTransforms.spine1.position,
                    posSpine2 = localRigTransforms.spine2.position,
                    posSpineTop = localRigTransforms.spineTop.position,
                    posLClavicle = localRigTransforms.lClavicle.position,
                    posRClavicle = localRigTransforms.rClavicle.position,
                    posNeck = localRigTransforms.neck.position,
                    posLShoulder = localRigTransforms.lShoulder.position,
                    posRShoulder = localRigTransforms.rShoulder.position,
                    posLElbow = localRigTransforms.lElbow.position,
                    posRElbow = localRigTransforms.rElbow.position,
                    posLWrist = localRigTransforms.lWrist.position,
                    posRWrist = localRigTransforms.rWrist.position,

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

                ServerSendToAll(ofrtm, P2PSend.Unreliable);
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
            SetLocalRigTransforms();

        }

        private void StopServer()
        {
            try
            {
                foreach (PlayerRep r in playerObjects.Values)
                {
                    r.Destroy();
                }
            }
            catch (Exception)
            {
                MelonModLogger.LogError("Caught exception destroying player objects");
            }

            playerObjects.Clear();
            playerNames.Clear();
            smallPlayerIds.Clear();
            largePlayerIds.Clear();
            smallIdCounter = 1;
            isServer = false;

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
