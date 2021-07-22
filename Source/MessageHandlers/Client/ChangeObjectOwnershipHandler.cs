using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.ChangeObjectOwnership, PeerType.Client)]
    class ChangeObjectOwnershipHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var client = (Core.Client)peer;

            var coom = new ChangeObjectOwnershipMessage(msg);
            GameObject obj = ObjectIDManager.GetObject(coom.objectId);
            var so = obj.GetComponent<SyncedObject>();
            so.owner = coom.ownerId;

            if (so.owner == client.LocalSmallId)
            {
                so.rb.isKinematic = false;
                so.rb.velocity = coom.linVelocity;
                so.rb.angularVelocity = coom.angVelocity;
            }
            else
                so.rb.isKinematic = true;

            MelonLogger.Log($"Object {coom.objectId} is now owned by {coom.ownerId} (kinematic: {so.rb.isKinematic})");
        }
    }
}
