using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.Disconnect, PeerType.Server)]
    class DisconnectHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            MelonLogger.Log("Player left with ID: " + connection.ConnectedTo);

            var smallId = players[connection.ConnectedTo].SmallID;
            players.Remove(connection.ConnectedTo);

            P2PMessage disconnectMsg = new P2PMessage();
            disconnectMsg.WriteByte((byte)MessageType.Disconnect);
            disconnectMsg.WriteByte(smallId);

            foreach (MPPlayer p in players)
            {
                p.Connection.SendMessage(disconnectMsg, MessageSendType.Reliable);
            }

            foreach (MPPlayer p in players)
            {
                p.PlayerRep.faceAnimator.faceState = Representations.FaceAnimator.FaceState.Sad;
                p.PlayerRep.faceAnimator.faceTime = 6;
            }
        }
    }
}
