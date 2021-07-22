using MultiplayerMod.Networking;
using MultiplayerMod.Core;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.ServerShutdown, PeerType.Client)]
    class ServerShutdownHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            ((Core.Client)peer).Disconnect();
        }
    }
}
