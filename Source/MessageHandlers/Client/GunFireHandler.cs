using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            PlayerRep pr = players[gfmo.playerId].PlayerRep;

            AmmoVariables ammoVariables = new AmmoVariables()
            {
                AttackDamage = gfmo.ammoDamage,
                AttackType = AttackType.Piercing,
                cartridgeType = (Cart)gfmo.cartridgeType,
                ExitVelocity = gfmo.exitVelocity,
                ProjectileMass = gfmo.projectileMass,
                Tracer = false
            };

            if ((StressLevelZero.Handedness)gfmo.handedness == StressLevelZero.Handedness.RIGHT)
            {
                pr.rightGunScript.firePointTransform.position = gfmo.firepointPos;
                pr.rightGunScript.firePointTransform.rotation = gfmo.firepointRotation;
                pr.rightGunScript.muzzleVelocity = gfmo.muzzleVelocity;
                pr.rightBulletObject.ammoVariables = ammoVariables;
                pr.rightGunScript.PullCartridge();
                pr.rightGunScript.Fire();
            }

            if ((StressLevelZero.Handedness)gfmo.handedness == StressLevelZero.Handedness.LEFT)
            {
                pr.leftGunScript.firePointTransform.position = gfmo.firepointPos;
                pr.leftGunScript.firePointTransform.rotation = gfmo.firepointRotation;
                pr.leftGunScript.muzzleVelocity = gfmo.muzzleVelocity;
                pr.leftBulletObject.ammoVariables = ammoVariables;
                pr.leftGunScript.PullCartridge();
                pr.leftGunScript.Fire();
            }

            pr.faceAnimator.faceState = Representations.FaceAnimator.FaceState.Angry;
            pr.faceAnimator.faceTime = 5;
        }
    }
}
