using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.ObjectSync, PeerType.Server)]

    class ObjectSyncHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            ObjectSyncMessage osm = new ObjectSyncMessage(msg);
            GameObject obj = ObjectIDManager.GetObject(osm.id);
            var player = players[connection.ConnectedTo];

            var so = obj.GetComponent<SyncedObject>();

            if (!obj)
            {
                MelonLogger.Error($"Couldn't find object with ID {osm.id}");
            }
            else
            {
                if (so.owner != player.SmallID)
                {
                    MelonLogger.Error("Got object sync from client that doesn't own the object");
                    var coom = new ChangeObjectOwnershipMessage(msg)
                    {
                        ownerId = so.owner,
                        objectId = so.ID,
                        linVelocity = so.rb.velocity,
                        angVelocity = so.rb.angularVelocity
                    };
                    player.Connection.SendMessage(coom.MakeMsg(), SendReliability.Reliable);
                }
                else
                {
                    obj.transform.position = osm.position;
                    obj.transform.rotation = osm.rotation;

                    players.SendMessageToAllExcept(osm, SendReliability.Reliable, connection.ConnectedTo);
                }
            }
        }
    }
}
