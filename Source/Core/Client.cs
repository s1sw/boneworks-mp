using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;
using StressLevelZero.Utilities;
using StressLevelZero.Pool;
using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using MultiplayerMod.MonoBehaviours;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Combat;
using MultiplayerMod.Extras;
using BoneworksModdingToolkit.BoneHook;

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
            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire += BWUtil_OnFire;
        }

        private void BWUtil_OnFire(Gun obj)
        {
            BulletObject bobj = obj.chamberedBulletGameObject.GetComponent<BulletObject>();
            try
            {
                AmmoVariables bObj = new AmmoVariables();
                bObj.AttackDamage = 1;
                bObj.ProjectileMass = 1;
                bObj.ExitVelocity = 1;
                if (obj.chamberedCartridge != null)
                {
                    bObj = obj.chamberedCartridge.ammoVariables;
                }
                else if (obj.overrideMagazine != null)
                {
                    bObj = obj.overrideMagazine.AmmoSlots[0].ammoVariables;
                }
                GunFireMessage gfm = new GunFireMessage()
                {
                    handedness = (byte)obj.host.GetHand(0).handedness,
                    firepointPos = obj.firePointTransform.position,
                    firepointRotation = obj.firePointTransform.rotation,
                    ammoDamage = bObj.AttackDamage,
                    projectileMass = bObj.ProjectileMass,
                    exitVelocity = bObj.ExitVelocity,
                    muzzleVelocity = obj.muzzleVelocity
                };

                SendToServer(gfm.MakeMsg(), MessageSendType.Reliable);
            }
            catch
            {

            }
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection arg1, P2PMessage msg)
        {
            MessageType type = (MessageType)msg.ReadByte();

            switch (type)
            {
                case MessageType.GunFire:
                    {
                        GunFireMessageOther gfmo = new GunFireMessageOther(msg);
                        PlayerRep pr = GetPlayerRep(gfmo.playerId);
                        AmmoVariables ammoVariables = new AmmoVariables()
                        {
                            AttackDamage = gfmo.ammoDamage,
                            AttackType = AttackType.Piercing,
                            cartridgeType = Cart.Cal_9mm,
                            ExitVelocity = gfmo.exitVelocity,
                            ProjectileMass = gfmo.projectileMass,
                            Tracer = false
                        };
                        if ((StressLevelZero.Handedness)gfmo.handedness == StressLevelZero.Handedness.RIGHT)
                        {
                            pr.rightGunScript.firePointTransform.position = gfmo.firepointPos;
                            pr.rightGunScript.firePointTransform.rotation = gfmo.firepointRotation;
                            pr.rightGunScript.muzzleVelocity = gfmo.muzzleVelocity;
                            pr.rightBulletObject.ammoVariables = ammoVariables;
                            pr.rightGunScript.PullCartridge();
                            pr.rightGunScript.Fire();
                        }
                        if ((StressLevelZero.Handedness)gfmo.handedness == StressLevelZero.Handedness.LEFT)
                        {
                            pr.leftGunScript.firePointTransform.position = gfmo.firepointPos;
                            pr.leftGunScript.firePointTransform.rotation = gfmo.firepointRotation;
                            pr.leftGunScript.muzzleVelocity = gfmo.muzzleVelocity;
                            pr.leftBulletObject.ammoVariables = ammoVariables;
                            pr.leftGunScript.PullCartridge();
                            pr.leftGunScript.Fire();
                        }
                        //pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Angry;
                        //pr.faceAnimator.faceTime = 5;
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

                        foreach (PlayerRep pr in playerObjects.Values)
                        {
                            //pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Sad;
                            //pr.faceAnimator.faceTime = 10;
                        }
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

                        foreach (PlayerRep pr in playerObjects.Values)
                        {
                            //pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Happy;
                            //pr.faceAnimator.faceTime = 15;
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
            Disconnect();
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

            BoneworksModdingToolkit.BoneHook.GunHooks.OnGunFire -= BWUtil_OnFire;
        }

        public void Update()
        {
            transportLayer.Update();
            if (SceneLoader.loading) return;

            if (localRigTransforms.root == null)
                SetupPlayerReferences();

            if (localRigTransforms.root != null)
            {
                FullRigTransformMessage frtm = new FullRigTransformMessage
                {
                    pos_root = localRigTransforms.root.position,
                    pos_head = localRigTransforms.head.position,
                    pos_lfHand = localRigTransforms.lfHand.position,
                    pos_rtHand = localRigTransforms.rtHand.position,
                    pos_pelvis = localRigTransforms.pelvis.position,
                    pos_lfFoot = localRigTransforms.lfFoot.position,
                    pos_rtFoot = localRigTransforms.rtFoot.position,
                    rot_root = localRigTransforms.root.rotation,
                    rot_head = localRigTransforms.head.rotation,
                    rot_lfHand = localRigTransforms.lfHand.rotation,
                    rot_rtHand = localRigTransforms.rtHand.rotation,
                    rot_lfFoot = localRigTransforms.lfFoot.rotation,
                    rot_rtFoot = localRigTransforms.rtFoot.rotation,
                    rot_pelvis = localRigTransforms.pelvis.rotation
                    
                };

                SendToServer(frtm, MessageSendType.Unreliable);

                foreach (PlayerRep pr in playerObjects.Values)
                {
                    pr.UpdateNameplateFacing(Camera.current.transform);
                    pr.ikAnimator.Update();
                }
            }
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
