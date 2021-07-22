using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerMod.Networking;
using MultiplayerMod.Core;
using MultiplayerMod.Representations;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.OtherFullRig, PeerType.Client)]
    class OtherFullRig : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage(msg);
            byte playerId = ofrtm.playerId;

            if (players.Contains(ofrtm.playerId))
            {
                PlayerRep pr = players[playerId].PlayerRep;

                pr.ApplyTransformMessage(ofrtm);
            }
        }
    }
}
