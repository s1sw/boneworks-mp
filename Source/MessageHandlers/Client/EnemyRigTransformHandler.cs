using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using MultiplayerMod.Structs;
using StressLevelZero.Pool;
using UnityEngine;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.EnemyRigTransform, PeerType.Client)]
    class EnemyRigTransformHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            var client = (Core.Client)peer;

            client.EnemyPoolManager.FindMissingPools();
            EnemyRigTransformMessage ertm = new EnemyRigTransformMessage(msg);
            Pool pool = client.EnemyPoolManager.GetPool(ertm.enemyType);

            // HORRID PERFORMANCE
            Transform enemyTf = pool.transform.GetChild(ertm.poolChildIdx);
            GameObject rootObj = enemyTf.Find("enemyBrett@neutral").gameObject;
            BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(rootObj);
            BWUtil.ApplyRigTransform(brt, ertm.transforms);
        }
    }
}
