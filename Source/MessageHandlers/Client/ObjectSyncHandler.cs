using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.ObjectSync, PeerType.Client)]
    class ObjectSyncHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
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
        }
    }
}
