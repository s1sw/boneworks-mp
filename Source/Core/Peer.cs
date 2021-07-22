using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.Networking;

namespace MultiplayerMod.Core
{
    public enum PeerType
    {
        Client,
        Server,
        Both
    }

    /// <summary>
    /// Base class for both the client and the server.
    /// </summary>
    public abstract class Peer
    {
        public abstract PeerType Type { get; }
        public Players Players => players;

        protected readonly Players players = new Players();
        protected readonly ITransportLayer transportLayer;
        protected MessageRouter messageRouter;

        protected Peer(ITransportLayer transportLayer)
        {
            this.transportLayer = transportLayer;
        }
    }
}
