using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.IdRequest, HandlerPeer.Server)]

    class IDRequestHandler : MessageHandler
    {
        private readonly Core.Server server;

        public IDRequestHandler(Players players, Peer peer) : base(players, peer)
        {
            server = (Core.Server)peer;
        }

        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var idrqm = new IDRequestMessage(msg);
            MelonLogger.Log("ID request: " + idrqm.namePath);
            var obj = BWUtil.GetObjectFromFullPath(idrqm.namePath);

            server.SetupSyncFor(obj, idrqm.initialOwner);
        }
    }
}
