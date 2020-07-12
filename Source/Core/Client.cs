using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using Oculus.Platform.Samples.VrHoops;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;
using Valve.VR;
using StressLevelZero.Interaction;
using StressLevelZero.Utilities;
using StressLevelZero.Pool;
using StressLevelZero.AI;

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Combat;

namespace MultiplayerMod.Core
{
    public class Client
    {
        public static GameObject brett;
        public static Player_Health brett_Health;

        private BoneworksRigTransforms localRigTransforms;
        public SteamId ServerId
        {
            get; private set;
        }
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MultiplayerMod.MAX_PLAYERS);
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private readonly MultiplayerUI ui;
        public bool isConnected = false;

        public Client(MultiplayerUI ui)
        {
            this.ui = ui;
        }

        public void SetupRP()
        {
            RichPresence.OnJoin += RichPresence_OnJoin;
        }

        public void SetLocalRigTransforms(BoneworksRigTransforms rigTransforms)
        {
            localRigTransforms = rigTransforms;
        }

        public void RecreatePlayers()
        {
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
        }

        public void Connect(string obj)
        {
            brett = GameObject.Find("[RigManager (Default Brett)]");
            brett_Health = brett.GetComponent<Player_Health>();
            MelonModLogger.Log("Starting client and connecting");

            ServerId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(MultiplayerMod.PROTOCOL_VERSION);
            msg.WriteUnicodeString(SteamClient.Name);
            SteamNetworking.SendP2PPacket(ServerId, msg.GetBytes());

            isConnected = true;
            //PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject += PlayerHooks_OnPlayerLetGoObject;
            localRigTransforms = BWUtil.GetLocalRigTransforms();

            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;
            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire += GunHooks_OnGunFire;
            ui.SetState(MultiplayerUIState.Client);
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError err)
        {
            if (id == ServerId)
            {
                ui.SetState(MultiplayerUIState.PreConnect);
                MelonModLogger.LogError("Got P2P connection error " + err.ToString());
                foreach (PlayerRep pr in playerObjects.Values)
                {
                    pr.Destroy();
                }
            }
        }

        private void OnP2PSessionRequest(SteamId id)
        {
            if (id != ServerId)
            {
                MelonModLogger.LogError("Got a P2P session request from something that is not the server.");
            }
            else
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
            }
        }

        private void RichPresence_OnJoin(string obj)
        {
            Connect(obj);
        }

        private void PlayerHooks_OnPlayerLetGoObject(GameObject obj)
        {
            HandGunChangeMessage hgcm = new HandGunChangeMessage()
            {
                isForOtherPlayer = false,
                destroy = true
            };

            SendToServer(hgcm, P2PSend.Reliable);
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject obj)
        {
            GunType? gt = BWUtil.GetGunType(obj.transform.root.gameObject);
            if (gt != null)
            {
                HandGunChangeMessage hgcm = new HandGunChangeMessage()
                {
                    isForOtherPlayer = false,
                    type = gt.Value,
                    destroy = false
                };

                SendToServer(hgcm, P2PSend.Reliable);
            }
        }

        public void Disconnect()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
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

            MelonModLogger.Log("Disconnecting...");
            isConnected = false;
            ServerId = 0;
            playerObjects.Clear();
            playerNames.Clear();
            largePlayerIds.Clear();
            smallPlayerIds.Clear();

            SteamNetworking.CloseP2PSessionWithUser(ServerId);
            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire -= GunHooks_OnGunFire;
            //PlayerHooks.OnPlayerGrabObject -= PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject -= PlayerHooks_OnPlayerLetGoObject;

            SteamNetworking.OnP2PConnectionFailed = null;
            SteamNetworking.OnP2PSessionRequest = null;
        }

        public void Update()
        {
            if (SceneLoader.loading) return;

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
                                MelonModLogger.Log("Pew complete!");
                                break;
                            }
                        case MessageType.OtherPlayerPosition:
                            {
                                OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage(msg);

                                if (playerObjects.ContainsKey(oppm.playerId))
                                {
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
                                }

                                break;
                            }
                        case MessageType.OtherFullRig:
                            {
                                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage(msg);
                                byte playerId = ofrtm.playerId;

                                if (playerObjects.ContainsKey(ofrtm.playerId))
                                {
                                    PlayerRep pr = GetPlayerRep(playerId);

                                    pr.ApplyTransformMessage(ofrtm);
                                }
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
                                largePlayerIds.Remove(pid);
                                playerNames.Remove(pid);
                                break;
                            }
                        case MessageType.JoinRejected:
                            {
                                MelonModLogger.LogError("Join rejected - you are using an incompatible version of the mod!");
                                Disconnect();
                                break;
                            }
                        case MessageType.SceneTransition:
                            {
                                SceneTransitionMessage stm = new SceneTransitionMessage(msg);
                                if (BoneworksSceneManager.GetCurrentSceneName() != stm.sceneName)
                                {
                                    BoneworksSceneManager.LoadScene(stm.sceneName);
                                }
                                break;
                            }
                        case MessageType.Join:
                            {
                                ClientJoinMessage cjm = new ClientJoinMessage(msg);
                                largePlayerIds.Add(cjm.playerId, cjm.steamId);
                                playerNames.Add(cjm.playerId, cjm.name);
                                playerObjects.Add(cjm.playerId, new PlayerRep(cjm.name, cjm.steamId));
                                break;
                            }
                        case MessageType.OtherHandGunChange:
                            {
                                HandGunChangeMessage hgcm = new HandGunChangeMessage(msg, true);

                                if (hgcm.destroy)
                                {
                                    Destroy(playerObjects[hgcm.playerId].currentGun);
                                }
                                else
                                {
                                    PlayerRep pr = playerObjects[hgcm.playerId];
                                    pr.currentGun = BWUtil.SpawnGun(hgcm.type);
                                    pr.currentGun.transform.parent = pr.gunParent.transform;
                                    pr.currentGun.transform.localPosition = Vector3.zero;
                                    pr.currentGun.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
                                    pr.currentGun.GetComponent<Rigidbody>().isKinematic = true;
                                }
                                break;
                            }
                        case MessageType.SetPartyId:
                            {
                                SetPartyIdMessage spid = new SetPartyIdMessage(msg);
                                RichPresence.SetActivity(
                                    new Activity()
                                    {
                                        Details = "Connected to a server",
                                        Secrets = new ActivitySecrets()
                                        {
                                            Join = ServerId.ToString()
                                        },
                                        Party = new ActivityParty()
                                        {
                                            Id = spid.partyId,
                                            Size = new PartySize()
                                            {
                                                CurrentSize = 1,
                                                MaxSize = MultiplayerMod.MAX_PLAYERS
                                            }
                                        }
                                    });
                                break;
                            }
                        case MessageType.EnemyRigTransform:
                            {
                                enemyPoolManager.FindMissingPools();
                                EnemyRigTransformMessage ertm = new EnemyRigTransformMessage(msg);
                                Pool pool = enemyPoolManager.GetPool(ertm.enemyType);
                                // HORRID PERFORMANCE
                                Transform enemyTf = pool.transform.GetChild(ertm.poolChildIdx);
                                GameObject rootObj = enemyTf.Find("enemyBrett@neutral").gameObject;
                                BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(rootObj);
                                BWUtil.ApplyRigTransform(brt, ertm);
                                break;
                            }
                    }
                }
            }
            //if (localHead != null)
            //{
            //    PlayerPositionMessage ppm = new PlayerPositionMessage
            //    {
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

            //    SendToServer(ppm, P2PSend.Unreliable);

            //    foreach (PlayerRep pr in playerObjects.Values)
            //    {
            //        pr.UpdateNameplateFacing(Camera.current.transform);
            //    }
            //}
            //else if (enableFullRig)
            {
                if (localRigTransforms.main == null)
                    SetLocalRigTransforms(BWUtil.GetLocalRigTransforms());

                if (localRigTransforms.main != null)
                {
                    FullRigTransformMessage frtm = new FullRigTransformMessage
                    {
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

                    SendToServer(frtm, P2PSend.Unreliable);

                    foreach (PlayerRep pr in playerObjects.Values)
                    {
                        pr.UpdateNameplateFacing(Camera.current.transform);
                    }
                }
            }
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
                SendToServer(gfm, P2PSend.Unreliable);
        }
        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        private void SendToServer(P2PMessage msg, P2PSend send)
        {
            byte[] msgBytes = msg.GetBytes();
            SteamNetworking.SendP2PPacket(ServerId, msgBytes, msgBytes.Length, 0, send);
        }

        private void SendToServer(INetworkMessage msg, P2PSend send)
        {
            SendToServer(msg.MakeMsg(), send);
        }
    }
}
