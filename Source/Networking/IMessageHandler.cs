using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerMod.Core;

namespace MultiplayerMod.Networking
{
    public interface IMessageHandler
    {
        MessageType GetHandledMessageType();

        void HandleMessage(P2PMessage msg, ITransportConnection connection, NetController networkController);
    }
}
