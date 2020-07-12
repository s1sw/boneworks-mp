using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using Oculus.Platform.Models;
using Oculus.Platform.Samples.VrHoops;
using StressLevelZero.Pool;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;
using BoneworksModdingToolkit;
using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Combat;

namespace MultiplayerMod.Core
{
    public class Server
    {
        public static GameObject brett;
        public static Player_Health brett_Health;

        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MultiplayerMod.MAX_PLAYERS);
        private readonly List<ulong> players = new List<ulong>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(MultiplayerMod.MAX_PLAYERS);
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private string partyId = "";
        private byte smallIdCounter = 0;
        private BoneworksRigTransforms localRigTransforms;
        private readonly MultiplayerUI ui;

        public bool IsRunning { get; private set; }

        public Server(MultiplayerUI ui)
        {
            this.ui = ui;
        }

        private void GunHooks_OnGunFire(Gun obj)
        {
                BulletObject bobj = obj.chamberedBulletGameObject.GetComponent<BulletObject>();
                GunFireMessage gfm = new GunFireMessage()
                {
                    fireDirection = obj.firePointTransform.rotation,
                    fireOrigin = obj.firePointTransform.position,
                    bulletDamage = 2
                    
                };
                GameObject instance = GameObject.Instantiate(lineHolder);
                LineRenderer lineRenderer = instance.GetComponent<LineRenderer>();
                lineRenderer.widthMultiplier = 0.2f;
                lineRenderer.SetPosition(0, gfm.fireOrigin);
                lineRenderer.SetPosition(1, gfm.fireOrigin + (gfm.fireDirection.eulerAngles * 999));
                GameObject.Destroy(instance, 10);
                ServerSendToAll(gfm, P2PSend.Unreliable);
        }

        public void Update()
        {
            if (SceneLoader.loading) return;
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
                        case MessageType.GunFire:
                            {
                                GunFireMessage gfm = new GunFireMessage(msg);
                                Ray ray = new Ray(gfm.fireOrigin, gfm.fireDirection.eulerAngles);
                                
                                RaycastHit hit;
                                GameObject instance = GameObject.Instantiate(lineHolder);
                                LineRenderer lineRenderer = instance.GetComponent<LineRenderer>();
                                lineRenderer.widthMultiplier = 0.2f;
                                lineRenderer.SetPosition(0, gfm.fireOrigin);
                                lineRenderer.SetPosition(1, gfm.fireOrigin + (gfm.fireDirection.eulerAngles * 999));
                                GameObject.Destroy(instance, 10);
                                if (Physics.Raycast(ray, out hit, int.MaxValue, ~0, QueryTriggerInteraction.Ignore))
                                {
                                    if (hit.transform.root == brett)
                                    {
                                        MelonModLogger.Log("Hit BRETT!");
                                        int random = UnityEngine.Random.Range(0, 10);
                                        brett_Health.TAKEDAMAGE(gfm.bulletDamage, random == 0);
                                    }
                                    else
                                    {
                                        MelonModLogger.Log("Hit!");
                                    }
                                }
                                else
                                {
                                    MelonModLogger.Log("Did not hit!");
                                }
                                ServerSendToAllExcept(gfm, P2PSend.Unreliable, packet.Value.SteamId);
                                MelonModLogger.Log("Pew serber send");
                                break;
                            }
                        case MessageType.Join:
                            {
                                if (msg.ReadByte() != MultiplayerMod.PROTOCOL_VERSION)
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
                                                Id = partyId,
                                                Size = new PartySize()
                                                {
                                                    CurrentSize = players.Count + 1,
                                                    MaxSize = MultiplayerMod.MAX_PLAYERS
                                                }
                                            }
                                        });

                                    SceneTransitionMessage stm = new SceneTransitionMessage()
                                    {
                                        sceneName = BoneworksSceneManager.GetCurrentSceneName()
                                    };
                                    SendToId(stm, P2PSend.Reliable, packet.Value.SteamId);

                                    SetPartyIdMessage spid = new SetPartyIdMessage()
                                    {
                                        partyId = partyId
                                    };
                                    SendToId(spid, P2PSend.Reliable, packet.Value.SteamId);

                                    ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);
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
                                players.RemoveAll((ulong val) => val == packet.Value.SteamId);
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
                                if (playerObjects.ContainsKey(playerId))
                                {
                                    PlayerRep pr = playerObjects[playerId];

                                    if (pr.rigTransforms.main != null)
                                    {
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

            #region Unused Code
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
            #endregion

            if (localRigTransforms.main == null)
                localRigTransforms = BWUtil.GetLocalRigTransforms();

            if (localRigTransforms.main != null)
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
            // Disabled temporarily
#if false
            {
                enemyPoolManager.FindMissingPools();
                Pool pool = enemyPoolManager.GetPool(EnemyType.NullBody);
                for (int i = 0; i < pool.transform.childCount; i++)
                {
                    GameObject childEnemy = pool.transform.GetChild(i).gameObject;

                    BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(childEnemy.transform.Find("brettEnemy@neutral").gameObject);

                    EnemyRigTransformMessage ertf = new EnemyRigTransformMessage()
                    {
                        poolChildIdx = (byte)i,
                        enemyType = EnemyType.NullBody,
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

                    ServerSendToAll(ertf, P2PSend.UnreliableNoDelay);
                }
            }
#endif
        }

        private void OnP2PSessionRequest(SteamId id)
        {
            SteamNetworking.AcceptP2PSessionWithUser(id);
            MelonModLogger.Log("Accepted session for " + id.ToString());
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            if (error == P2PSessionError.NoRightsToApp)
            {
                MelonModLogger.LogError("You don't own the game on Steam.");
            }
            else if (error == P2PSessionError.NotRunningApp)
            {
                // Probably a leaver
                if (smallPlayerIds.ContainsKey(id))
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
                    players.RemoveAll((ulong val) => val == id);
                    smallPlayerIds.Remove(id);
                }
            }
            else if (error == P2PSessionError.Timeout)
            {
                MelonModLogger.LogError("Connection with " + id + "timed out.");

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
                players.RemoveAll((ulong val) => val == id);
                smallPlayerIds.Remove(id);
            }
            else
            {
                MelonModLogger.LogError("Unhandled P2P error: " + error.ToString());
            }
        }

        private void MultiplayerMod_OnLevelWasLoadedEvent(int level)
        {
            SceneTransitionMessage stm = new SceneTransitionMessage
            {
                sceneName = BoneworksSceneManager.GetSceneNameFromScenePath(level)
            };
            ServerSendToAll(stm, P2PSend.Reliable);
            enemyPoolManager.FindAllPools();
            brett = GameObject.Find("[RigManager (Default Brett)]");
            brett_Health = brett.GetComponent<Player_Health>();
        }

        GameObject lineHolder;
        public void StartServer()
        {
            lineHolder = new GameObject();
            lineHolder.AddComponent<LineRenderer>();

            brett = GameObject.Find("[RigManager (Default Brett)]");
            brett_Health = brett.GetComponent<Player_Health>();

            ui.SetState(MultiplayerUIState.Server);
            MelonModLogger.Log("Starting server...");
            localRigTransforms = BWUtil.GetLocalRigTransforms();
            partyId = SteamClient.SteamId + "P" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

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
                        Id = partyId,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = MultiplayerMod.MAX_PLAYERS
                        }
                    }
                });

            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;

            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;
            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire += GunHooks_OnGunFire;
            IsRunning = true;
        }

        public void StopServer()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
            IsRunning = false;

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

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (SteamId p in players)
            {
                SteamNetworking.SendP2PPacket(p, shutdownMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                SteamNetworking.CloseP2PSessionWithUser(p);
            }

            players.Clear();

            MultiplayerMod.OnLevelWasLoadedEvent -= MultiplayerMod_OnLevelWasLoadedEvent;
            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire -= GunHooks_OnGunFire;

            SteamNetworking.OnP2PSessionRequest = null;
            SteamNetworking.OnP2PConnectionFailed = null;
        }

        private void ServerSendToAll(INetworkMessage msg, P2PSend send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            foreach (SteamId p in players)
            {
                SteamNetworking.SendP2PPacket(p, bytes, bytes.Length, 0, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, P2PSend send, SteamId except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            foreach (SteamId p in players)
            {
                if (p != except)
                    SteamNetworking.SendP2PPacket(p, bytes, bytes.Length, 0, send);
            }
        }

        private void SendToId(INetworkMessage msg, P2PSend send, SteamId id)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            SteamNetworking.SendP2PPacket(id, bytes, bytes.Length, 0, send);
        }

        private void PlayerHooks_OnPlayerLetGoObject(GameObject obj)
        {
            HandGunChangeMessage hgcm = new HandGunChangeMessage()
            {
                isForOtherPlayer = true,
                destroy = true,
                playerId = 0
            };

            ServerSendToAll(hgcm, P2PSend.Reliable);
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject obj)
        {
            // See if it's a gun
            GunType? gt = BWUtil.GetGunType(obj.transform.root.gameObject);
            if (gt != null)
            {
                HandGunChangeMessage hgcm = new HandGunChangeMessage()
                {
                    isForOtherPlayer = true,
                    type = gt.Value,
                    destroy = false,
                    playerId = 0
                };

                ServerSendToAll(hgcm, P2PSend.Reliable);

                switch (gt)
                {
                    case GunType.EDER22:
                        MelonModLogger.Log("Holding Eder22");
                        break;
                }
            }
        }
    }
}
