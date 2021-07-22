using MelonLoader;
using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.IdRequest, PeerType.Server)]

    class IDRequestHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var idrqm = new IDRequestMessage(msg);
            MelonLogger.Log("ID request: " + idrqm.namePath);
            var obj = BWUtil.GetObjectFromFullPath(idrqm.namePath);

            ((Core.Server)peer).SetupSyncFor(obj, idrqm.initialOwner);
        }
    }
}
