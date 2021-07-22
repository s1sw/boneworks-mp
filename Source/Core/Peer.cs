using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Core
{
    public enum PeerType
    {
        Client,
        Server,
        Both
    }

    // For now, this simply acts as a base class for the server and client so we can cast
    // to the correct class in message handlers. However, there's lots of functionality that could be deduplicated
    // later.
    public abstract class Peer
    {
        public abstract PeerType Type { get; }
    }
}
