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
using StressLevelZero.Interaction;

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
        private readonly List<SyncedObject> syncedObjects = new List<SyncedObject>();
        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;
        private ITransportConnection connection;
        private byte localSmallId;
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
            MelonLogger.Log("Starting client and connecting");

            ServerId = ulong.Parse(obj);
            MelonLogger.Log("Connecting to " + obj);

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
            GunHooks.OnGunFire += BWUtil_OnFire;
            PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            PlayerHooks.OnPlayerReleaseObject += PlayerHooks_OnPlayerReleaseObject;
            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;
        }

        private void MultiplayerMod_OnLevelWasLoadedEvent(int obj)
        {
            syncedObjects.Clear();
        }

        private void PlayerHooks_OnPlayerReleaseObject(GameObject grabObj)
        {
            MelonLogger.Log($"Released {grabObj.name}");
            var rb = grabObj.GetComponentInParent<Rigidbody>();
            if (rb == null) return;
            var obj = rb.gameObject;

            var so = obj.GetComponent<SyncedObject>();

            if (so && so.owner == localSmallId)
            {
                var coom = new ChangeObjectOwnershipMessage
                {
                    objectId = so.ID,
                    ownerId = 0,
                    linVelocity = rb.velocity,
                    angVelocity = rb.angularVelocity
                };

                SendToServer(coom, MessageSendType.Reliable);
            }
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject grabObj)
        {
            var rb = grabObj.GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                return;
            }
            MelonLogger.Log($"Grabbed {rb.gameObject.name}");

            var obj = rb.gameObject;
            var so = obj.GetComponent<SyncedObject>();

            if (!so)
            {
                MelonLogger.Log($"Requesting ID for {obj.name}");
                var req = new IDRequestMessage
                {
                    namePath = BWUtil.GetFullNamePath(obj),
                    initialOwner = localSmallId
                };

                SendToServer(req, MessageSendType.Reliable);
            }
            else
            {
                MelonLogger.Log($"Grapped object has ID of {so.ID}");
                var coom = new ChangeObjectOwnershipMessage
                {
                    objectId = so.ID,
                    ownerId = localSmallId
                };

                SendToServer(coom, MessageSendType.Reliable);
            }
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

            try
            {
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
                            pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Angry;
                            pr.faceAnimator.faceTime = 5;
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
                                pr.Delete();
                            }
                            break;
                        }
                    case MessageType.Disconnect:
                        {
                            byte pid = msg.ReadByte();
                            playerObjects[pid].Delete();
                            playerObjects.Remove(pid);
                            largePlayerIds.Remove(pid);
                            playerNames.Remove(pid);

                            foreach (PlayerRep pr in playerObjects.Values)
                            {
                                pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Sad;
                                pr.faceAnimator.faceTime = 10;
                            }
                            break;
                        }
                    case MessageType.JoinRejected:
                        {
                            MelonLogger.LogError("Join rejected - you are using an incompatible version of the mod!");
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
                                pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Happy;
                                pr.faceAnimator.faceTime = 15;
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
                    case MessageType.IdAllocation:
                        {
                            IDAllocationMessage iam = new IDAllocationMessage(msg);
                            GameObject obj = BWUtil.GetObjectFromFullPath(iam.namePath);
                            if (!obj)
                            {
                                MelonLogger.LogWarning("Got IdAllocation for nonexistent object???");
                            }
                            ObjectIDManager.AddObject(iam.allocatedId, obj);

                            var so = obj.AddComponent<SyncedObject>();
                            so.ID = iam.allocatedId;
                            so.owner = iam.initialOwner;
                            so.rb = obj.GetComponent<Rigidbody>();

                            syncedObjects.Add(so);

                            if (so.owner != localSmallId)
                                so.rb.isKinematic = true;

                            MelonLogger.Log($"ID Allocation: {iam.namePath}, {so.ID}");
                            break;
                        }
                    case MessageType.ObjectSync:
                        {
                            ObjectSyncMessage osm = new ObjectSyncMessage(msg);
                            GameObject obj = ObjectIDManager.GetObject(osm.id);

                            if (!obj)
                            {
                                MelonLogger.LogError($"Couldn't find object with ID {osm.id}");
                            }
                            else
                            {
                                obj.transform.position = osm.position;
                                obj.transform.rotation = osm.rotation;
                            }
                            break;
                        }
                    case MessageType.ChangeObjectOwnership:
                        {
                            var coom = new ChangeObjectOwnershipMessage(msg);
                            var obj = ObjectIDManager.GetObject(coom.objectId);
                            var so = obj.GetComponent<SyncedObject>();
                            so.owner = coom.ownerId;

                            if (so.owner == localSmallId)
                            {
                                so.rb.isKinematic = false;
                                so.rb.velocity = coom.linVelocity;
                                so.rb.angularVelocity = coom.angVelocity;
                            }
                            else
                                so.rb.isKinematic = true;

                            MelonLogger.Log($"Object {coom.objectId} is now owned by {coom.ownerId} (kinematic: {so.rb.isKinematic})");

                            break;
                        }
                    case MessageType.SetLocalSmallId:
                        {
                            var slsi = new SetLocalSmallIdMessage(msg);
                            localSmallId = slsi.smallId;
                            break;
                        }
                }
            } 
            catch (Exception e)
            {
                MelonLogger.LogError($"Caught exception in message handler for message {type}: {e}");
            }
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            if (connection.ConnectedTo != ServerId)
            {
                MelonLogger.LogError("Connection with non-server ID was closed - but we're a client???");
                return;
            }

            ui.SetState(MultiplayerUIState.PreConnect);
            MelonLogger.LogError("Got P2P connection error " + reason.ToString());
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
                    r.Delete();
                }
            }
            catch (Exception)
            {
                MelonLogger.LogError("Caught exception destroying player objects");
            }

            MelonLogger.Log("Disconnecting...");
            isConnected = false;
            ServerId = 0;
            playerObjects.Clear();
            playerNames.Clear();
            largePlayerIds.Clear();
            smallPlayerIds.Clear();

            if (connection.IsConnected)
                connection.Disconnect();

            GunHooks.OnGunFire -= BWUtil_OnFire;
        }

        public void Update()
        {
            transportLayer.Update();
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
                    pr.faceAnimator.Update();
                }
            }

            foreach (var so in syncedObjects)
            {
                if (so.owner == localSmallId && so.NeedsSync())
                {
                    var osm = so.CreateSyncMessage();
                    SendToServer(osm, MessageSendType.Reliable);
                }
            }
        }

        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        private void SendToServer(P2PMessage msg, MessageSendType send)
        {
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
