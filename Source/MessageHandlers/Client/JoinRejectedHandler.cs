using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.JoinRejected, PeerType.Client)]
    class JoinRejectedHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            MelonLogger.Error("Join rejected - you are using an incompatible version of the mod!");
            ((Core.Client)peer).Disconnect();
        }
    }
}
