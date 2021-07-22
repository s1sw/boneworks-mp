using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
