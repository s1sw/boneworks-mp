using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            pr.ApplyTransformMessage(frtm);

            OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
            {
                playerId = player.SmallID,

                posMain = frtm.posMain,
                posRoot = frtm.posRoot,
                posLHip = frtm.posLHip,
                posRHip = frtm.posRHip,
                posLKnee = frtm.posLKnee,
                posRKnee = frtm.posRKnee,
                posLAnkle = frtm.posLAnkle,
                posRAnkle = frtm.posRAnkle,

                posSpine1 = frtm.posSpine1,
                posSpine2 = frtm.posSpine2,
                posSpineTop = frtm.posSpineTop,
                posLClavicle = frtm.posLClavicle,
                posRClavicle = frtm.posRClavicle,
                posNeck = frtm.posNeck,
                posLShoulder = frtm.posLShoulder,
                posRShoulder = frtm.posRShoulder,
                posLElbow = frtm.posLElbow,
                posRElbow = frtm.posRElbow,
                posLWrist = frtm.posLWrist,
                posRWrist = frtm.posRWrist,

                rotMain = frtm.rotMain,
                rotRoot = frtm.rotRoot,
                rotLHip = frtm.rotLHip,
                rotRHip = frtm.rotRHip,
                rotLKnee = frtm.rotLKnee,
                rotRKnee = frtm.rotRKnee,
                rotLAnkle = frtm.rotLAnkle,
                rotRAnkle = frtm.rotRAnkle,
                rotSpine1 = frtm.rotSpine1,
                rotSpine2 = frtm.rotSpine2,
                rotSpineTop = frtm.rotSpineTop,
                rotLClavicle = frtm.rotLClavicle,
                rotRClavicle = frtm.rotRClavicle,
                rotNeck = frtm.rotNeck,
                rotLShoulder = frtm.rotLShoulder,
                rotRShoulder = frtm.rotRShoulder,
                rotLElbow = frtm.rotLElbow,
                rotRElbow = frtm.rotRElbow,
                rotLWrist = frtm.rotLWrist,
                rotRWrist = frtm.rotRWrist
            };

            players.SendMessageToAllExcept(ofrtm, MessageSendType.Unreliable, connection.ConnectedTo);
        }
    }
}
