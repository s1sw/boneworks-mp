using System;
using System.Collections.Generic;
using BoneworksModdingToolkit.BoneHook;
using Discord;
using Facepunch.Steamworks;
using MelonLoader;
using MultiplayerMod.Boneworks;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using MultiplayerMod.Structs;
using StressLevelZero.Combat;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Utilities;
using UnityEngine;

namespace MultiplayerMod.Core
{
    public class Server : Peer
    {
        public List<SyncedObject> SyncedObjects { get; private set; } = new List<SyncedObject>();
        public string PartyID { get; private set; } = string.Empty;
        public bool IsRunning { get; private set; }
        public override PeerType Type => PeerType.Server;

        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;
        private readonly Players players = new Players();
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private MessageRouter messageRouter;
        private BoneworksRigTransforms localRigTransforms;

        public Server(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
        }

        public void SyncNewPlayer(MPPlayer player)
        {
            var connection = player.Connection;

            // Sync any allocated physics sync objects
            foreach (var so in SyncedObjects)
            {
                var iam = new IDAllocationMessage
                {
                    allocatedId = so.ID,
                    namePath = BWUtil.GetFullNamePath(so.gameObject),
                    initialOwner = so.owner
                };
                connection.SendMessage(iam.MakeMsg(), MessageSendType.Reliable);
            }

            // Sync existing players to the newly joining player
            foreach (MPPlayer p in players)
            {
                ClientJoinMessage cjm = new ClientJoinMessage
                {
                    playerId = p.SmallID,
                    name = p.Name,
                    steamId = p.FullID
                };
                connection.SendMessage(cjm.MakeMsg(), MessageSendType.Reliable);
            }

            // Sync the host player
            {
                ClientJoinMessage cjm2 = new ClientJoinMessage
                {
                    playerId = 0,
                    name = SteamClient.Name,
                    steamId = SteamClient.SteamId
                };
                connection.SendMessage(cjm2.MakeMsg(), MessageSendType.Reliable);
            }

            // Sync current scene, Discord party ID and player's small ID
            SceneTransitionMessage stm = new SceneTransitionMessage()
            {
                sceneName = BoneworksSceneManager.GetCurrentSceneName()
            };
            connection.SendMessage(stm.MakeMsg(), MessageSendType.Reliable);

            SetPartyIdMessage spid = new SetPartyIdMessage()
            {
                partyId = PartyID
            };
            connection.SendMessage(spid.MakeMsg(), MessageSendType.Reliable);
        }

        private void GunHooks_OnGunFire(Gun obj)
        {
            try
            {
                AmmoVariables bObj = new AmmoVariables
                {
                    AttackDamage = 1,
                    ProjectileMass = 1,
                    ExitVelocity = 1
                };

                if (obj.chamberedCartridge != null)
                {
                    bObj = obj.chamberedCartridge.ammoVariables;
                }
                else if (obj.overrideMagazine != null)
                {
                    bObj = obj.overrideMagazine.AmmoSlots[0].ammoVariables;
                }

                GunFireMessageOther gfmo = new GunFireMessageOther()
                {
                    handedness = (byte)obj.host.GetHand(0).handedness,
                    playerId = 0,
                    firepointPos = obj.firePointTransform.position,
                    firepointRotation = obj.firePointTransform.rotation,
                    ammoDamage = bObj.AttackDamage,
                    projectileMass = bObj.ProjectileMass,
                    exitVelocity = bObj.ExitVelocity,
                    muzzleVelocity = obj.muzzleVelocity,
                    cartridgeType = (byte)bObj.cartridgeType
                };
                ServerSendToAll(gfmo, MessageSendType.Reliable);
            }
            catch
            {

            }

        }

        public void Update()
        {
            transportLayer.Update();
            if (SceneLoader.loading) return;

            ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);

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

                ServerSendToAll(ofrtm, MessageSendType.Unreliable);
            }

            foreach (MPPlayer player in players)
            {
                PlayerRep rep = player.PlayerRep;
                rep.UpdateNameplateFacing(Camera.current.transform);
                rep.faceAnimator.Update();
            }

            foreach (var obj in SyncedObjects)
            {
                if (obj.owner == 0 && obj.NeedsSync())
                {
                    ServerSendToAll(obj.CreateSyncMessage(), MessageSendType.Unreliable);
                    obj.UpdateLastSync();
                }
            }
        }

        private void MultiplayerMod_OnLevelWasLoadedEvent(int level)
        {
            SyncedObjects.Clear();
            SceneTransitionMessage stm = new SceneTransitionMessage
            {
                sceneName = BoneworksSceneManager.GetSceneNameFromScenePath(level)
            };
            ServerSendToAll(stm, MessageSendType.Reliable);
            enemyPoolManager.FindAllPools();
        }

        public void StartServer()
        {
            ui.SetState(MultiplayerUIState.Server);
            MelonLogger.Log("Starting server...");
            localRigTransforms = BWUtil.GetLocalRigTransforms();
            PartyID = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() + "P" + SteamClient.SteamId;
            messageRouter = new MessageRouter(players, this);

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
                        Id = PartyID,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = MultiplayerMod.MAX_PLAYERS
                        }
                    }
                });
            transportLayer.OnMessageReceived += TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;
            GunHooks.OnGunFire += GunHooks_OnGunFire;
            PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            PlayerHooks.OnPlayerReleaseObject += PlayerHooks_OnPlayerReleaseObject;
            transportLayer.StartListening();

            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;

            IsRunning = true;
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            switch (reason)
            {
                case ConnectionClosedReason.Timeout:
                case ConnectionClosedReason.ClosedByRemote:
                    if (players.Contains(connection.ConnectedTo))
                    {
                        MPPlayer player = players[connection.ConnectedTo];
                        players.Remove(player);

                        MelonLogger.Log($"Player {player.Name} left");

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(player.SmallID);

                        foreach (MPPlayer p in players)
                        {
                            p.Connection.SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }
                    }
                    break;
                case ConnectionClosedReason.Other:
                    break;

            }
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection connection, P2PMessage msg)
        {
            messageRouter.HandleMessage(connection, msg);
        }

        public void StopServer()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
            IsRunning = false;

            messageRouter = null;

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (MPPlayer p in players)
            {
                p.Connection.SendMessage(shutdownMsg, MessageSendType.Reliable);
                p.Connection.Disconnect();
            }

            players.Clear();

            transportLayer.OnMessageReceived -= TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed -= TransportLayer_OnConnectionClosed;
            GunHooks.OnGunFire -= GunHooks_OnGunFire;
            PlayerHooks.OnPlayerGrabObject -= PlayerHooks_OnPlayerGrabObject;
            PlayerHooks.OnPlayerReleaseObject -= PlayerHooks_OnPlayerReleaseObject;
            transportLayer.StopListening();

            MultiplayerMod.OnLevelWasLoadedEvent -= MultiplayerMod_OnLevelWasLoadedEvent;
        }

        private void PlayerHooks_OnPlayerReleaseObject(GameObject obj)
        {
            MelonLogger.Log($"Released {obj.name}");
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject obj)
        {
            var rb = obj.GetComponentInParent<Rigidbody>();

            if (rb == null)
            {
                MelonLogger.LogWarning("Grabbed non-RB!!!");
                return;
            }

            MelonLogger.Log($"Grabbed {rb.gameObject.name}");

            if (rb.gameObject.GetComponent<SyncedObject>() == null)
                SetupSyncFor(rb.gameObject);
        }

        public void ServerSendToAll(INetworkMessage msg, MessageSendType send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in players)
            {
                p.Connection.SendMessage(pMsg, send);
            }
        }

        public void ServerSendToAllExcept(INetworkMessage msg, MessageSendType send, ulong except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in players)
            {
                if (p.FullID != except)
                    p.Connection.SendMessage(pMsg, send);
            }
        }

        public void SendToId(INetworkMessage msg, MessageSendType send, ulong id)
        {
            P2PMessage pMsg = msg.MakeMsg();
            players[id].Connection.SendMessage(pMsg, send);
        }

        public void SetupSyncFor(GameObject obj, byte initialOwner = 0)
        {
            ushort id = ObjectIDManager.AllocateID();
            ObjectIDManager.AddObject(id, obj);

            var so = obj.AddComponent<SyncedObject>();
            so.owner = initialOwner;
            so.ID = id;
            so.rb = obj.GetComponent<Rigidbody>();
            SyncedObjects.Add(so);

            var iam = new IDAllocationMessage
            {
                allocatedId = id,
                namePath = BWUtil.GetFullNamePath(obj),
                initialOwner = so.owner
            };
            ServerSendToAll(iam, MessageSendType.Reliable);
        }
    }
}
