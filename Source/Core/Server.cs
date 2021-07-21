using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
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

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Extras;
using StressLevelZero.Combat;
using BoneworksModdingToolkit.BoneHook;

namespace MultiplayerMod.Core
{
    public class Server
    {
        private readonly Players players = new Players();
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private readonly List<SyncedObject> syncObjs = new List<SyncedObject>();
        private string partyId = "";
        private byte smallIdCounter = 1;
        private BoneworksRigTransforms localRigTransforms;
        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;

        public bool IsRunning { get; private set; }

        public Server(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
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

            foreach (var obj in syncObjs)
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
            syncObjs.Clear();
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
            partyId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() + "P" + SteamClient.SteamId;

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
            MessageType type = (MessageType)msg.ReadByte();

            switch (type)
            {
                case MessageType.GunFire:
                    {
                        GunFireMessage gfm = new GunFireMessage(msg);

                        if (players.Contains(connection.ConnectedTo))
                        {
                            MPPlayer player = players[connection.ConnectedTo];
                            PlayerRep pr = player.PlayerRep;

                            AmmoVariables ammoVariables = new AmmoVariables()
                            {
                                AttackDamage = gfm.ammoDamage,
                                AttackType = AttackType.Piercing,
                                cartridgeType = (Cart)gfm.cartridgeType,
                                ExitVelocity = gfm.exitVelocity,
                                ProjectileMass = gfm.projectileMass,
                                Tracer = false
                            };

                            if ((StressLevelZero.Handedness)gfm.handedness == StressLevelZero.Handedness.RIGHT)
                            {
                                pr.rightGunScript.firePointTransform.position = gfm.firepointPos;
                                pr.rightGunScript.firePointTransform.rotation = gfm.firepointRotation;
                                pr.rightGunScript.muzzleVelocity = gfm.muzzleVelocity;
                                pr.rightBulletObject.ammoVariables = ammoVariables;
                                pr.leftGunScript.PullCartridge();
                                pr.rightGunScript.Fire();
                            }

                            if ((StressLevelZero.Handedness)gfm.handedness == StressLevelZero.Handedness.LEFT)
                            {
                                pr.leftGunScript.firePointTransform.position = gfm.firepointPos;
                                pr.leftGunScript.firePointTransform.rotation = gfm.firepointRotation;
                                pr.leftGunScript.muzzleVelocity = gfm.muzzleVelocity;
                                pr.leftBulletObject.ammoVariables = ammoVariables;
                                pr.leftGunScript.PullCartridge();
                                pr.leftGunScript.Fire();
                            }

                            GunFireMessageOther gfmo = new GunFireMessageOther()
                            {
                                playerId = player.SmallID,
                                handedness = gfm.handedness,
                                firepointPos = gfm.firepointPos,
                                firepointRotation = gfm.firepointRotation,
                                ammoDamage = gfm.ammoDamage,
                                projectileMass = gfm.projectileMass,
                                exitVelocity = gfm.exitVelocity,
                                muzzleVelocity = gfm.muzzleVelocity
                            };

                            pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Angry;
                            pr.faceAnimator.faceTime = 5;
                            ServerSendToAllExcept(gfmo, MessageSendType.Reliable, connection.ConnectedTo);
                        }
                        break;
                    }
                case MessageType.Join:
                    {
                        if (msg.ReadByte() != MultiplayerMod.PROTOCOL_VERSION)
                        {
                            // Somebody tried to join with an incompatible verison. Kick 'em!
                            P2PMessage m2 = new P2PMessage();
                            m2.WriteByte((byte)MessageType.JoinRejected);
                            connection.SendMessage(m2, MessageSendType.Reliable);
                            connection.Disconnect();
                        }
                        else
                        {
                            MelonLogger.Log("Player joined with ID: " + connection.ConnectedTo);

                            if (players.Contains(connection.ConnectedTo))
                                players.Remove(connection.ConnectedTo);

                            string name = msg.ReadUnicodeString();
                            byte newPlayerId = smallIdCounter;
                            smallIdCounter++;

                            var player = new MPPlayer(name, connection.ConnectedTo, newPlayerId, connection);

                            MelonLogger.Log("Player count: " + players.Count);
                            MelonLogger.Log("Name: " + name);

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

                                ClientJoinMessage cjm3 = new ClientJoinMessage
                                {
                                    playerId = newPlayerId,
                                    name = name,
                                    steamId = connection.ConnectedTo
                                };
                                ServerSendToAllExcept(cjm3, MessageSendType.Reliable, connection.ConnectedTo);
                            }

                            // Sync current scene, Discord party ID and player's small ID
                            SceneTransitionMessage stm = new SceneTransitionMessage()
                            {
                                sceneName = BoneworksSceneManager.GetCurrentSceneName()
                            };
                            connection.SendMessage(stm.MakeMsg(), MessageSendType.Reliable);

                            SetPartyIdMessage spid = new SetPartyIdMessage()
                            {
                                partyId = partyId
                            };
                            connection.SendMessage(spid.MakeMsg(), MessageSendType.Reliable);

                            SetLocalSmallIdMessage slsi = new SetLocalSmallIdMessage()
                            {
                                smallId = newPlayerId
                            };
                            connection.SendMessage(slsi.MakeMsg(), MessageSendType.Reliable);

                            // Sync any allocated physics sync objects
                            foreach (var so in syncObjs)
                            {
                                var iam = new IDAllocationMessage
                                {
                                    allocatedId = so.ID,
                                    namePath = BWUtil.GetFullNamePath(so.gameObject),
                                    initialOwner = so.owner
                                };
                                connection.SendMessage(iam.MakeMsg(), MessageSendType.Reliable);
                            }

                            players.Add(player);

                            ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);

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
                                            CurrentSize = players.Count,
                                            MaxSize = MultiplayerMod.MAX_PLAYERS
                                        }
                                    }
                                });

                            foreach (MPPlayer p in players)
                            {
                                p.PlayerRep.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Happy;
                                p.PlayerRep.faceAnimator.faceTime = 15;
                            }
                        }
                        break;
                    }
                case MessageType.Disconnect:
                    {
                        MelonLogger.Log("Player left with ID: " + connection.ConnectedTo);

                        var smallId = players[connection.ConnectedTo].SmallID;
                        players.Remove(connection.ConnectedTo);

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(smallId);

                        foreach (MPPlayer p in players)
                        {
                            p.Connection.SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }

                        foreach (MPPlayer p in players)
                        {
                            p.PlayerRep.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Sad;
                            p.PlayerRep.faceAnimator.faceTime = 6;
                        }
                        break;
                    }
                case MessageType.FullRig:
                    {
                        FullRigTransformMessage frtm = new FullRigTransformMessage(msg);

                        if (players.Contains(connection.ConnectedTo))
                        {
                            MPPlayer player = players[connection.ConnectedTo];
                            PlayerRep pr = player.PlayerRep;

                            if (pr.rigTransforms.main != null)
                            {
                                //ApplyTransformMessage(pr, frtm);
                                pr.ApplyTransformMessage(frtm);

                                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                                {
                                    playerId = player.SmallID,

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

                                ServerSendToAllExcept(ofrtm, MessageSendType.Unreliable, connection.ConnectedTo);
                            }
                        }
                        break;
                    }
                case MessageType.IdRequest:
                    {
                        var idrqm = new IDRequestMessage(msg);
                        MelonLogger.Log("ID request: " + idrqm.namePath);
                        var obj = BWUtil.GetObjectFromFullPath(idrqm.namePath);

                        SetupSyncFor(obj, idrqm.initialOwner);
                        break;
                    }
                case MessageType.ChangeObjectOwnership:
                    {
                        var coom = new ChangeObjectOwnershipMessage(msg);
                        var player = players[connection.ConnectedTo];

                        if (coom.ownerId != player.SmallID && coom.ownerId != 0)
                        {
                            MelonLogger.LogError("Invalid object ownership change??");
                        }

                        if (!ObjectIDManager.objects.ContainsKey(coom.objectId))
                        {
                            MelonLogger.LogError($"Got ownership change for invalid object ID {coom.objectId}");
                        }

                        MelonLogger.Log($"Object {coom.objectId} is now owned by {coom.ownerId}");

                        var obj = ObjectIDManager.GetObject(coom.objectId);
                        var so = obj.GetComponent<SyncedObject>();
                        so.owner = coom.ownerId;

                        if (so.owner != 0)
                        {
                            coom.linVelocity = so.rb.velocity;
                            coom.angVelocity = so.rb.angularVelocity;
                            so.rb.isKinematic = true;
                        }
                        else if (so.owner == 0)
                        {
                            so.rb.isKinematic = false;
                            so.rb.velocity = coom.linVelocity;
                            so.rb.angularVelocity = coom.angVelocity;
                        }

                        ServerSendToAll(coom, MessageSendType.Reliable);
                        break;
                    }
                case MessageType.ObjectSync:
                    {
                        ObjectSyncMessage osm = new ObjectSyncMessage(msg);
                        GameObject obj = ObjectIDManager.GetObject(osm.id);
                        var player = players[connection.ConnectedTo];

                        var so = obj.GetComponent<SyncedObject>();

                        if (!obj)
                        {
                            MelonLogger.LogError($"Couldn't find object with ID {osm.id}");
                        }
                        else
                        {
                            if (so.owner != player.SmallID)
                            {
                                MelonLogger.LogError("Got object sync from client that doesn't own the object");
                                var coom = new ChangeObjectOwnershipMessage(msg)
                                {
                                    ownerId = so.owner,
                                    objectId = so.ID,
                                    linVelocity = so.rb.velocity,
                                    angVelocity = so.rb.angularVelocity
                                };
                                player.Connection.SendMessage(coom.MakeMsg(), MessageSendType.Reliable);
                            }
                            else
                            {
                                obj.transform.position = osm.position;
                                obj.transform.rotation = osm.rotation;

                                ServerSendToAllExcept(osm, MessageSendType.Reliable, connection.ConnectedTo);
                            }
                        }

                        break;
                    }
                default:
                    MelonLogger.Log("Unknown message type: " + type.ToString());
                    break;
            }
        }

        public void StopServer()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
            IsRunning = false;

            smallIdCounter = 1;

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

        private void ServerSendToAll(INetworkMessage msg, MessageSendType send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in players)
            {
                p.Connection.SendMessage(pMsg, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, MessageSendType send, ulong except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in players)
            {
                if (p.FullID != except)
                    p.Connection.SendMessage(pMsg, send);
            }
        }

        private void SendToId(INetworkMessage msg, MessageSendType send, ulong id)
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
            syncObjs.Add(so);

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
