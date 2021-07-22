using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.OtherPlayerPosition, PeerType.Client)]
    class OtherPlayerPositionHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage(msg);

            if (players.Contains(oppm.playerId))
            {
                PlayerRep pr = players[oppm.playerId].PlayerRep;

                pr.head.transform.position = oppm.headPos;
                pr.handL.transform.position = oppm.lHandPos;
                pr.handR.transform.position = oppm.rHandPos;
                pr.pelvis.transform.position = oppm.pelvisPos;
                pr.ford.transform.position = oppm.pelvisPos - new Vector3(0.0f, 0.3f, 0.0f);
                pr.footL.transform.position = oppm.lFootPos;
                pr.footR.transform.position = oppm.rFootPos;

                pr.head.transform.rotation = oppm.headRot;
                pr.handL.transform.rotation = oppm.lHandRot;
                pr.handR.transform.rotation = oppm.rHandRot;
                pr.pelvis.transform.rotation = oppm.pelvisRot;
                pr.footL.transform.rotation = oppm.lFootRot;
                pr.footR.transform.rotation = oppm.rFootRot;
            }
        }
    }
}
