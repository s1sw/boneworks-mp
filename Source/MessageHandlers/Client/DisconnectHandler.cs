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
    [MessageHandler(MessageType.Disconnect, PeerType.Client)]
    class DisconnectHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            byte pid = msg.ReadByte();
            players.Remove(pid);

            foreach (MPPlayer player in players)
            {
                player.PlayerRep.faceAnimator.faceState = Representations.FaceAnimator.FaceState.Sad;
                player.PlayerRep.faceAnimator.faceTime = 10;
            }
        }
    }
}
