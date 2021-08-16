using ModThatIsNotMod;
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
    [MessageHandler(MessageType.PlayerDamage, PeerType.Both)]
    class PlayerDamageHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            PlayerDamageMessage pdm = new PlayerDamageMessage(msg);

            if (peer.Type == PeerType.Server)
            {
                players.SendMessageToAllExcept(pdm, SendReliability.Reliable, connection.ConnectedTo);
            }

            if (peer.Type == PeerType.Client && pdm.playerId != Client.LocalSmallId) return;
            else if (peer.Type == PeerType.Server && pdm.playerId != 0) return;

            Player_Health.Cache.Get(Player.GetRigManager()).TAKEDAMAGE(pdm.damage);
        }
    }
}
