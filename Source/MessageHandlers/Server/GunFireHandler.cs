using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Combat;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.GunFire, HandlerPeer.Server)]
    class GunFireHandler : MessageHandler
    {
        public GunFireHandler(Players players, Peer peer) : base(players, peer) { }

        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            GunFireMessage gfm = new GunFireMessage(msg);

            if (players.Contains(connection.ConnectedTo))
            {
                MPPlayer player = players[connection.ConnectedTo];
                PlayerRep pr = player.PlayerRep;

                AmmoVariables ammoVariables = new AmmoVariables()
                {
                    AttackDamage = gfm.ammoDamage,
                    AttackType = AttackType.Piercing,
                    cartridgeType = (Cart)gfm.cartridgeType,
                    ExitVelocity = gfm.exitVelocity,
                    ProjectileMass = gfm.projectileMass,
                    Tracer = false
                };

                if ((StressLevelZero.Handedness)gfm.handedness == StressLevelZero.Handedness.RIGHT)
                {
                    pr.rightGunScript.firePointTransform.position = gfm.firepointPos;
                    pr.rightGunScript.firePointTransform.rotation = gfm.firepointRotation;
                    pr.rightGunScript.muzzleVelocity = gfm.muzzleVelocity;
                    pr.rightBulletObject.ammoVariables = ammoVariables;
                    pr.leftGunScript.PullCartridge();
                    pr.rightGunScript.Fire();
                }

                if ((StressLevelZero.Handedness)gfm.handedness == StressLevelZero.Handedness.LEFT)
                {
                    pr.leftGunScript.firePointTransform.position = gfm.firepointPos;
                    pr.leftGunScript.firePointTransform.rotation = gfm.firepointRotation;
                    pr.leftGunScript.muzzleVelocity = gfm.muzzleVelocity;
                    pr.leftBulletObject.ammoVariables = ammoVariables;
                    pr.leftGunScript.PullCartridge();
                    pr.leftGunScript.Fire();
                }

                GunFireMessageOther gfmo = new GunFireMessageOther()
                {
                    playerId = player.SmallID,
                    handedness = gfm.handedness,
                    firepointPos = gfm.firepointPos,
                    firepointRotation = gfm.firepointRotation,
                    ammoDamage = gfm.ammoDamage,
                    projectileMass = gfm.projectileMass,
                    exitVelocity = gfm.exitVelocity,
                    muzzleVelocity = gfm.muzzleVelocity
                };

                pr.faceAnimator.faceState = Source.Representations.FaceAnimator.FaceState.Angry;
                pr.faceAnimator.faceTime = 5;
                players.SendMessageToAllExcept(gfmo, MessageSendType.Reliable, connection.ConnectedTo);
            }
        }
    }
}
