using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Combat;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.GunFire, PeerType.Client)]
    public class GunFireHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            GunFireMessageOther gfmo = new GunFireMessageOther(msg);
            GunFireInfo fireInfo = gfmo.fireInfo;
            PlayerRep pr = players[gfmo.playerId].PlayerRep;

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
                pr.rightGunScript.PullCartridge();
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

            pr.faceAnimator.faceState = FaceAnimator.FaceState.Angry;
            pr.faceAnimator.faceTime = 5;
        }
    }
}
