using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Combat;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.GunFire, PeerType.Server)]
    class GunFireHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            GunFireMessage gfm = new GunFireMessage(msg);

            if (players.Contains(connection.ConnectedTo))
            {
                MPPlayer player = players[connection.ConnectedTo];
                PlayerRep pr = player.PlayerRep;

                GunFireInfo fireInfo = gfm.fireInfo;

                AmmoVariables ammoVariables = new AmmoVariables()
                {
                    AttackDamage = 0.0f,
                    AttackType = AttackType.Piercing,
                    cartridgeType = (Cart)fireInfo.cartridgeType,
                    ExitVelocity = fireInfo.exitVelocity,
                    ProjectileMass = fireInfo.projectileMass,
                    Tracer = false
                };

                if ((StressLevelZero.Handedness)fireInfo.handedness == StressLevelZero.Handedness.RIGHT)
                {
                    pr.rightGunScript.firePointTransform.position = fireInfo.firepointPos;
                    pr.rightGunScript.firePointTransform.rotation = fireInfo.firepointRotation;
                    pr.rightGunScript.muzzleVelocity = fireInfo.muzzleVelocity;
                    pr.rightBulletObject.ammoVariables = ammoVariables;
                    pr.leftGunScript.PullCartridge();
                    pr.rightGunScript.Fire();
                }

                if ((StressLevelZero.Handedness)fireInfo.handedness == StressLevelZero.Handedness.LEFT)
                {
                    pr.leftGunScript.firePointTransform.position = fireInfo.firepointPos;
                    pr.leftGunScript.firePointTransform.rotation = fireInfo.firepointRotation;
                    pr.leftGunScript.muzzleVelocity = fireInfo.muzzleVelocity;
                    pr.leftBulletObject.ammoVariables = ammoVariables;
                    pr.leftGunScript.PullCartridge();
                    pr.leftGunScript.Fire();
                }

                GunFireMessageOther gfmo = new GunFireMessageOther()
                {
                    playerId = player.SmallID,
                    fireInfo = fireInfo
                };

                pr.faceAnimator.faceState = FaceAnimator.FaceState.Angry;
                pr.faceAnimator.faceTime = 5;
                players.SendMessageToAllExcept(gfmo, SendReliability.Reliable, connection.ConnectedTo);
            }
        }
    }
}
