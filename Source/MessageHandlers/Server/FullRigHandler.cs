using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.FullRig, PeerType.Server)]
    public class FullRigHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            FullRigTransformMessage frtm = new FullRigTransformMessage(msg);

            if (!players.Contains(connection.ConnectedTo)) return;

            MPPlayer player = players[connection.ConnectedTo];
            PlayerRep pr = player.PlayerRep;

            if (pr.rigTransforms.main == null) return;

            //ApplyTransformMessage(pr, frtm);
            pr.ApplyTransformMessage(frtm.transforms);

            OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
            {
                playerId = player.SmallID,
                transforms = frtm.transforms
            };

            players.SendMessageToAllExcept(ofrtm, SendReliability.Unreliable, connection.ConnectedTo);
        }
    }
}
