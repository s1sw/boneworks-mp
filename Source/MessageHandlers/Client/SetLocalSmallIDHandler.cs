using MultiplayerMod.Core;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.Networking;

namespace MultiplayerMod.Source.MessageHandlers.Client
{
    [MessageHandler(MessageType.SetLocalSmallId, PeerType.Client)]
    class SetLocalSmallIDHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var client = (Core.Client)peer;
            var slsi = new SetLocalSmallIdMessage(msg);

            client.LocalSmallId = slsi.smallId;
        }
    }
}
