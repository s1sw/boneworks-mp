using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers
{
    public abstract class MessageHandler
    {
        protected Players players;
        protected Peer peer;

        public void Init(Players players, Peer peer)
        {
            this.players = players;
            this.peer = peer;
        }

        public abstract void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg);
    }
}
