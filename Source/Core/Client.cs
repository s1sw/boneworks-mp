using System.Collections.Generic;
using BoneworksModdingToolkit.BoneHook;
using Facepunch.Steamworks;
using MelonLoader;
using MultiplayerMod.Boneworks;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using MultiplayerMod.Structs;
using StressLevelZero.Combat;
using StressLevelZero.Interaction;
using StressLevelZero.Props.Weapons;
using UnityEngine;
using ModThatIsNotMod;

namespace MultiplayerMod.Core
{
    public class Client : Peer
    {
        private BoneworksRigTransforms localRigTransforms;

        public ulong ServerFullId { get; private set;  }
        public override PeerType Type => PeerType.Client;
        public bool IsConnected { get; private set; }
        public EnemyPoolManager EnemyPoolManager { get; private set; } = new EnemyPoolManager();
        public List<SyncedObject> SyncedObjects { get; private set; } = new List<SyncedObject>();
        public Players Players => players;
        
        public byte LocalSmallId;

        private readonly Players players = new Players();
        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;
        private ITransportConnection connection;
        private MessageRouter messageRouter;

        public Client(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
        }

        public void SetupRP()
        {
            RichPresence.OnJoin += RichPresence_OnJoin;
        }

        public void Connect(string obj)
        {
            MelonLogger.Msg("Starting client and connecting");

            ServerFullId = ulong.Parse(obj);
            MelonLogger.Msg("Connecting to " + obj);

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(MultiplayerMod.PROTOCOL_VERSION);
            msg.WriteUnicodeString(SteamClient.Name);

            connection = transportLayer.ConnectTo(ServerFullId, msg);
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;
            transportLayer.OnMessageReceived += TransportLayer_OnMessageReceived;

            IsConnected = true;
            localRigTransforms = BWUtil.GetLocalRigTransforms();

            ui.SetState(MultiplayerUIState.Client);
            Hooking.OnPostFireGun += OnPostFireGun;
            Hooking.OnGripAttached += OnGripAttached;
            Hooking.OnGripDetached += OnGripDetached;

            MultiplayerMod.OnLevelWasLoadedEvent += OnLevelWasLoaded;

            messageRouter = new MessageRouter(Players, this);
        }

        private void OnLevelWasLoaded(int obj)
        {
            SyncedObjects.Clear();
        }

        private void OnGripDetached(Grip grip, Hand hand)
        {
            GameObject grabObj = grip.gameObject;

            MelonLogger.Msg($"Released {grabObj.name}");
            var rb = grabObj.GetComponentInParent<Rigidbody>();

            Grip[] grips = rb.GetComponentsInChildren<Grip>();
            foreach (Grip objectGrip in grips)
            {
                MelonLogger.Msg(objectGrip.gameObject.name);
                HandToGripState htgsL = objectGrip.GetHandState(Player.leftHand);
                if (htgsL != null)
                    if (htgsL.isActive == true)
                        return;

                HandToGripState htgsR = objectGrip.GetHandState(Player.rightHand);
                if (htgsR != null)
                    if (htgsR.isActive == true)
                        return;
            }

            if (rb == null) return;

            var obj = rb.gameObject;
            var so = obj.GetComponent<SyncedObject>();

            if (so && so.owner == LocalSmallId)
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

        private void OnGripAttached(Grip grip, Hand hand)
        {
            GameObject grabObj = grip.gameObject;
            var rb = grabObj.GetComponentInParent<Rigidbody>();

            if (rb == null)
            {
                return;
            }

            MelonLogger.Msg($"Grabbed {rb.gameObject.name}");

            var obj = rb.gameObject;
            var so = obj.GetComponent<SyncedObject>();

            if (!so)
            {
                MelonLogger.Msg($"Requesting ID for {obj.name}");
                var req = new IDRequestMessage
                {
                    namePath = BWUtil.GetFullNamePath(obj),
                    initialOwner = LocalSmallId
                };

                SendToServer(req, MessageSendType.Reliable);
            }
            else
            {
                MelonLogger.Msg($"Grapped object has ID of {so.ID}");
                var coom = new ChangeObjectOwnershipMessage
                {
                    objectId = so.ID,
                    ownerId = LocalSmallId
                };

                SendToServer(coom, MessageSendType.Reliable);
            }
        }

        private void OnPostFireGun(Gun obj)
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

                GunFireMessage gfm = new GunFireMessage()
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

                SendToServer(gfm.MakeMsg(), MessageSendType.Reliable);
            }
            catch
            {

            }
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection arg1, P2PMessage msg)
        {
            messageRouter.HandleMessage(connection, msg);
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            if (connection.ConnectedTo != ServerFullId)
            {
                MelonLogger.Error("Connection with non-server ID was closed - but we're a client???");
                return;
            }

            ui.SetState(MultiplayerUIState.PreConnect);
            MelonLogger.Error("Got P2P connection error " + reason.ToString());
            Disconnect();
        }

        private void RichPresence_OnJoin(string obj)
        {
            Connect(obj);
        }

        public void Disconnect()
        {
            ui.SetState(MultiplayerUIState.PreConnect);

            MelonLogger.Msg("Disconnecting...");
            IsConnected = false;
            ServerFullId = 0;
            Players.Clear();
            messageRouter = null;

            if (connection.IsConnected)
                connection.Disconnect();

            Hooking.OnPostFireGun -= OnPostFireGun;
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

                foreach (MPPlayer p in Players)
                {
                    p.PlayerRep.UpdateNameplateFacing(Camera.current.transform);
                    p.PlayerRep.faceAnimator.Update();
                }
            }

            foreach (var so in SyncedObjects)
            {
                if (so.owner == LocalSmallId && so.NeedsSync())
                {
                    var osm = so.CreateSyncMessage();
                    SendToServer(osm, MessageSendType.Reliable);
                }
            }
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
        }
    }
}
