using MelonLoader;
using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.IdRequest, PeerType.Server)]

    class IDRequestHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var idrqm = new IDRequestMessage(msg);
            MelonLogger.Msg("ID request: " + idrqm.namePath);
            var obj = BWUtil.GetObjectFromFullPath(idrqm.namePath);

            ((Core.Server)peer).SetupSyncFor(obj, idrqm.initialOwner, idrqm.priorityLevel);
        }
    }
}
