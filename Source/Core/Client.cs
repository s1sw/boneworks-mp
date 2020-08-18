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
using MultiplayerMod.MonoBehaviours;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Combat;
using MultiplayerMod.Extras;
//using BoneworksModdingToolkit;

namespace MultiplayerMod.Core
{
    public class Client
    {
        private BoneworksRigTransforms localRigTransforms;
        private Player_Health localHealth;

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
        private readonly ITransportLayer transportLayer;
        private ITransportConnection connection;
        public bool isConnected = false;

        public Client(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
        }

        public void SetupRP()
        {
            RichPresence.OnJoin += RichPresence_OnJoin;
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
            MelonModLogger.Log("Starting client and connecting");

            ServerId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(MultiplayerMod.PROTOCOL_VERSION);
            msg.WriteUnicodeString(SteamClient.Name);

            connection = transportLayer.ConnectTo(ServerId, msg);
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;
            transportLayer.OnMessageReceived += TransportLayer_OnMessageReceived;

            isConnected = true;
            localRigTransforms = BWUtil.GetLocalRigTransforms();

            ui.SetState(MultiplayerUIState.Client);
            BWUtil.OnFire += BWUtil_OnFire;
        }

        private void BWUtil_OnFire(Gun obj)
        {
            BulletObject bobj = obj.chamberedBulletGameObject.GetComponent<BulletObject>();
            GunFireMessage gfm = new GunFireMessage()
            {
                fireDirection = obj.firePointTransform.forward,
                fireOrigin = obj.firePointTransform.position,
                bulletDamage = bobj.ammoVariables.AttackDamage
            };

            SendToServer(gfm.MakeMsg(), MessageSendType.Reliable);
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection arg1, P2PMessage msg)
        {
            MessageType type = (MessageType)msg.ReadByte();

            switch (type)
            {
                case MessageType.GunFireHit:
                    {
                        GunFireHit gfm = new GunFireHit(msg);
                        if (playerObjects.ContainsKey(gfm.playerId))
                        {
                            PlayerRep pr = playerObjects[gfm.playerId];

                            MelonModLogger.Log("Hit local player");
                            if (pr.rigTransforms.main != null)
                            {
                                GameObject instance = GameObject.Instantiate(GunResources.HurtSFX, pr.rigTransforms.main);
                                Destroy(instance, 3);
                            }
                        }
                        break;
                    }
                case MessageType.GunFire:
                    {
                        bool didHit;
                        GunFireMessage gfm = new GunFireMessage(msg);
                        Ray ray = new Ray(gfm.fireOrigin, gfm.fireDirection);
                        if (Physics.Raycast(ray, out RaycastHit hit, int.MaxValue, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform.root.gameObject == BWUtil.RigManager)
                            {
                                MelonModLogger.Log("Hit BRETT!");
                                int random = UnityEngine.Random.Range(0, 10);
                                BWUtil.LocalPlayerHealth.TAKEDAMAGE(gfm.bulletDamage, random == 0);
                                GunFireHitToServer gff = new GunFireHitToServer();
                                SendToServer(gff, MessageSendType.Reliable);
                            }
                            else
                            {
                                MelonModLogger.Log("Hit!");
                            }
                            didHit = true;
                        }
                        else
                        {
                            didHit = false;
                            MelonModLogger.Log("Did not hit!");

                        }

                        GameObject instance = Instantiate(GunResources.LinePrefab);
                        LineRenderer lineRenderer = instance.GetComponent<LineRenderer>();
                        lineRenderer.SetPosition(0, gfm.fireOrigin);
                        if (didHit)
                            lineRenderer.SetPosition(1, hit.transform.position);
                        else
                            lineRenderer.SetPosition(1, gfm.fireOrigin + (gfm.fireDirection * int.MaxValue));
                        Destroy(instance, 3);

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
                case MessageType.IdAllocation:
                    {
                        IDAllocationMessage iam = new IDAllocationMessage(msg);
                        GameObject obj = BWUtil.GetObjectFromFullPath(iam.namePath);
                        ObjectIDManager.AddObject(iam.allocatedId, obj);
                        obj.AddComponent<IDHolder>().ID = iam.allocatedId;
                        break;
                    }
                case MessageType.ObjectSync:
                    {
                        ObjectSyncMessage osm = new ObjectSyncMessage(msg);
                        GameObject obj = ObjectIDManager.GetObject(osm.id);

                        if (!obj)
                        {
                            MelonModLogger.LogError($"Couldn't find object with ID {osm.id}");
                        }
                        else
                        {
                            obj.transform.position = osm.position;
                            obj.transform.rotation = osm.rotation;
                        }
                        break;
                    }
            }
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            if (connection.ConnectedTo != ServerId)
            {
                MelonModLogger.LogError("Connection with non-server ID was closed - but we're a client???");
                return;
            }

            ui.SetState(MultiplayerUIState.PreConnect);
            MelonModLogger.LogError("Got P2P connection error " + reason.ToString());
            foreach (PlayerRep pr in playerObjects.Values)
            {
                pr.Destroy();
            }
        }

        private void RichPresence_OnJoin(string obj)
        {
            Connect(obj);
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

            if (connection.IsConnected)
                connection.Disconnect();

            BWUtil.OnFire -= BWUtil_OnFire;
        }

        public void Update()
        {
            if (SceneLoader.loading) return;

            if (localRigTransforms.main == null)
                SetupPlayerReferences();

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

                SendToServer(frtm, MessageSendType.Unreliable);

                foreach (PlayerRep pr in playerObjects.Values)
                {
                    pr.UpdateNameplateFacing(Camera.current.transform);
                }
            }

            transportLayer.Update();
        }

        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        private void SendToServer(P2PMessage msg, MessageSendType send)
        {
            byte[] msgBytes = msg.GetBytes();
            connection.SendMessage(msg, send);
        }

        private void SendToServer(INetworkMessage msg, MessageSendType send)
        {
            SendToServer(msg.MakeMsg(), send);
        }

        private void SetupPlayerReferences()
        {
            localRigTransforms = BWUtil.GetLocalRigTransforms();
            localHealth = BWUtil.RigManager.GetComponent<Player_Health>();
        }
    }
}
