using System;
using System.Collections.Generic;
using Discord;
using Facepunch.Steamworks;
using MelonLoader;
using ModThatIsNotMod;
using MultiplayerMod.Boneworks;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using MultiplayerMod.Structs;
using StressLevelZero.Combat;
using StressLevelZero.Interaction;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Utilities;
using UnityEngine;
using StressLevelZero.Data;
using System.Collections;

namespace MultiplayerMod.Core
{
    public class Server : Peer
    {
        /// <summary>
        /// List of all synced physics objects.
        /// </summary>
        public List<SyncedObject> SyncedObjects { get; private set; } = new List<SyncedObject>();
        /// <summary>
        /// Rich presence party ID.
        /// </summary>
        public string PartyID { get; private set; } = string.Empty;
        public bool IsRunning { get; private set; }
        public override PeerType Type => PeerType.Server;

        public event Action<MPPlayer> OnPlayerJoin;
        public event Action<MPPlayer> OnPlayerLeave;

        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private BoneworksRigTransforms localRigTransforms;

        public Server(ITransportLayer transportLayer) : base(transportLayer)
        {
            players.OnPlayerAdd += Players_OnPlayerAdd;
            players.OnPlayerRemove += Players_OnPlayerRemove;
        }

        private void Players_OnPlayerRemove(MPPlayer player)
        {
            OnPlayerLeave?.Invoke(player);
        }

        private void Players_OnPlayerAdd(MPPlayer player)
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
                connection.SendMessage(iam.MakeMsg(), SendReliability.Reliable);
            }

            // Sync existing players to the newly joining player
            foreach (MPPlayer p in players)
            {
                if (p == player) continue;

                ClientJoinMessage cjm = new ClientJoinMessage
                {
                    playerId = p.SmallID,
                    name = p.Name,
                    steamId = p.FullID
                };
                connection.SendMessage(cjm.MakeMsg(), SendReliability.Reliable);
            }

            // Sync the host player
            {
                ClientJoinMessage cjm2 = new ClientJoinMessage
                {
                    playerId = 0,
                    name = SteamClient.Name,
                    steamId = SteamClient.SteamId
                };
                connection.SendMessage(cjm2.MakeMsg(), SendReliability.Reliable);
            }

            // Sync current scene, Discord party ID and player's small ID
            SceneTransitionMessage stm = new SceneTransitionMessage()
            {
                sceneName = BoneworksSceneManager.GetCurrentSceneName()
            };
            connection.SendMessage(stm.MakeMsg(), SendReliability.Reliable);

            SetPartyIdMessage spid = new SetPartyIdMessage()
            {
                partyId = PartyID
            };
            connection.SendMessage(spid.MakeMsg(), SendReliability.Reliable);

            OnPlayerJoin?.Invoke(player);

            player.PlayerRep.OnDamage += PlayerRep_OnDamage;
        }

        private void PlayerRep_OnDamage(float damage, PlayerRep rep)
        {
            PlayerDamageMessage pdm = new PlayerDamageMessage()
            {
                playerId = rep.player.SmallID,
                damage = damage
            };

            Players.SendMessageToAll(pdm, SendReliability.Reliable);
        }

        private void GunHooks_OnGunFire(Gun obj)
        {
            MelonLogger.Msg($"Gun {obj.gameObject.name} fired");
            
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

                GunFireInfo fireInfo = new GunFireInfo()
                {
                    handedness = (byte)obj.host.GetHand(0).handedness,
                    firepointPos = obj.firePointTransform.position,
                    firepointRotation = obj.firePointTransform.rotation,
                    ammoDamage = bObj.AttackDamage,
                    projectileMass = bObj.ProjectileMass,
                    exitVelocity = bObj.ExitVelocity,
                    muzzleVelocity = obj.muzzleVelocity,
                    cartridgeType = (byte)bObj.cartridgeType
                };

                GunFireMessageOther gfmo = new GunFireMessageOther()
                {
                    playerId = 0,
                    fireInfo = fireInfo
                };
                players.SendMessageToAll(gfmo, SendReliability.Reliable);
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
                RigTransforms rigTransforms = new RigTransforms
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

                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                {
                    playerId = 0,
                    transforms = rigTransforms
                };

                players.SendMessageToAll(ofrtm, SendReliability.Unreliable);
            }

            foreach (MPPlayer player in players)
            {
                PlayerRep rep = player.PlayerRep;
                rep.UpdateNameplateFacing(Camera.current.transform);
                //rep.faceAnimator.Update();
            }

            SyncOwnedObjects();
        }

        private void SyncOwnedObjects()
        {
            foreach (var obj in SyncedObjects)
            {
                if (obj.owner == 0 && obj.NeedsSync())
                {
                    players.SendMessageToAll(obj.CreateSyncMessage(), SendReliability.Unreliable);
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
            players.SendMessageToAll(stm, SendReliability.Reliable);
            enemyPoolManager.FindAllPools();
        }

        public void StartServer()
        {
            MelonLogger.Msg("Starting server...");
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
            transportLayer.OnMessageReceived += messageRouter.HandleMessage;
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;

            Hooking.OnPostFireGun += GunHooks_OnGunFire;
            Hooking.OnGripAttached += OnGripAttached;
            Hooking.OnGripDetached += OnGripReleased;

            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;
            BWUtil.OnSpawnGunFire += BWUtil_OnSpawnGunFire;

            transportLayer.StartListening();

            Action<GameObject, OwnershipPriorityLevel> onCollideCallback = 
                (GameObject obj, OwnershipPriorityLevel priorityLevel) => { SetupSyncFor(obj, 0, OwnershipPriorityLevel.Touched); };

            var syncOnCollideL = Player.leftHand.gameObject.AddComponent<SyncOnCollide>();
            syncOnCollideL.SyncObjectCallback = onCollideCallback;
            var syncOnCollideR = Player.rightHand.gameObject.AddComponent<SyncOnCollide>();
            syncOnCollideR.SyncObjectCallback = onCollideCallback;

            IsRunning = true;
        }

        private void BWUtil_OnSpawnGunFire(SpawnGun gun, SpawnableObject spawnable, GameObject spawned)
        {
            var spawnMessage = new PoolSpawnMessage()
            {
                poolId = spawnable.title,
                position = spawned.transform.position,
                rotation = spawned.transform.rotation
            };

            Players.SendMessageToAll(spawnMessage, SendReliability.Reliable);
            SetupSyncFor(spawned.gameObject, 0, OwnershipPriorityLevel.Touched);
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

                        MelonLogger.Msg($"Player {player.Name} left");

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(player.SmallID);

                        foreach (MPPlayer p in players)
                        {
                            p.Connection.SendMessage(disconnectMsg, SendReliability.Reliable);
                        }
                    }
                    break;
                case ConnectionClosedReason.Other:
                    break;
            }
        }

        public void StopServer()
        {
            IsRunning = false;

            messageRouter = null;

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (MPPlayer p in players)
            {
                p.Connection.SendMessage(shutdownMsg, SendReliability.Reliable);
                p.Connection.Disconnect();
            }

            players.Clear();

            transportLayer.StopListening();


            transportLayer.OnMessageReceived -= messageRouter.HandleMessage;
            transportLayer.OnConnectionClosed -= TransportLayer_OnConnectionClosed;
            Hooking.OnPostFireGun -= GunHooks_OnGunFire;
            Hooking.OnGripAttached -= OnGripAttached;
            Hooking.OnGripDetached -= OnGripReleased;

            MultiplayerMod.OnLevelWasLoadedEvent -= MultiplayerMod_OnLevelWasLoadedEvent;
            BWUtil.OnSpawnGunFire -= BWUtil_OnSpawnGunFire;

            UnityEngine.Object.Destroy(Player.leftHand.GetComponent<SyncOnCollide>());
            UnityEngine.Object.Destroy(Player.rightHand.GetComponent<SyncOnCollide>());
        }

        private void OnGripReleased(Grip grip, Hand hand)
        {
            GameObject obj = grip.gameObject;
            MelonLogger.Msg($"Released {obj.name}");
        }

        private void OnGripAttached(Grip grip, Hand hand)
        {
            GameObject obj = grip.gameObject;
            var rb = obj.GetComponentInParent<Rigidbody>();

            if (rb == null)
            {
                MelonLogger.Warning("Grabbed non-RB!!!");
                return;
            }

            MelonLogger.Msg($"Grabbed {rb.gameObject.name}");

            if (rb.gameObject.GetComponent<SyncedObject>() == null)
                SetupSyncFor(rb.gameObject, 0, OwnershipPriorityLevel.Grabbed);
        }

        /// <summary>
        /// Sets up physics sync for the specified object.
        /// </summary>
        /// <param name="obj">The object to sync.</param>
        /// <param name="initialOwner">Small ID of the initial owner of the object.</param>
        public void SetupSyncFor(GameObject obj, byte initialOwner = 0, OwnershipPriorityLevel priorityLevel = OwnershipPriorityLevel.Touched)
        {
            if (obj.GetComponent<SyncedObject>() != null)
            {
                return;
            }
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
                initialOwner = so.owner,
                initialPriority = priorityLevel
            };

            players.SendMessageToAll(iam, SendReliability.Reliable);
        }
    }
}
