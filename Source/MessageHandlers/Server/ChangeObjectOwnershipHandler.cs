using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.ChangeObjectOwnership, PeerType.Server)]
    class ChangeObjectOwnershipHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var coom = new ChangeObjectOwnershipMessage(msg);
            var player = players[connection.ConnectedTo];

            if (coom.ownerId != player.SmallID && coom.ownerId != 0)
            {
                MelonLogger.Error("Invalid object ownership change??");
            }

            if (!ObjectIDManager.objects.ContainsKey(coom.objectId))
            {
                MelonLogger.Error($"Got ownership change for invalid object ID {coom.objectId}");
            }

            MelonLogger.Msg($"Object {coom.objectId} is now owned by {coom.ownerId}");

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

            players.SendMessageToAll(coom, SendReliability.Reliable);
        }
    }
}
