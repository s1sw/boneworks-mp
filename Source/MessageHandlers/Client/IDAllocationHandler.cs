using MelonLoader;
using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.IdAllocation, PeerType.Client)]
    class IDAllocationHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var client = (Core.Client)peer;

            IDAllocationMessage iam = new IDAllocationMessage(msg);
            GameObject obj = BWUtil.GetObjectFromFullPath(iam.namePath);

            if (!obj)
            {
                MelonLogger.LogWarning("Got IdAllocation for nonexistent object???");
                return;
            }

            ObjectIDManager.AddObject(iam.allocatedId, obj);

            var so = obj.AddComponent<SyncedObject>();
            so.ID = iam.allocatedId;
            so.owner = iam.initialOwner;
            so.rb = obj.GetComponent<Rigidbody>();

            client.SyncedObjects.Add(so);

            if (so.owner != client.LocalSmallId)
                so.rb.isKinematic = true;

            MelonLogger.Log($"ID Allocation: {iam.namePath}, {so.ID}");
        }
    }
}
