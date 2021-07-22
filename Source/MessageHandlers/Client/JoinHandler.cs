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
    [MessageHandler(MessageType.Join, PeerType.Client)]
    class JoinHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            ClientJoinMessage cjm = new ClientJoinMessage(msg);

            var player = new MPPlayer(cjm.name, cjm.steamId, cjm.playerId, connection);

            foreach (MPPlayer p in players)
            {
                p.PlayerRep.faceAnimator.faceState = Representations.FaceAnimator.FaceState.Happy;
                p.PlayerRep.faceAnimator.faceTime = 15;
            }
        }
    }
}
