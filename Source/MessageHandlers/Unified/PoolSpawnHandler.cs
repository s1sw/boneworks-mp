using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModThatIsNotMod;
using MultiplayerMod.Core;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Unified
{
    [MessageHandler(MessageType.PoolSpawn, PeerType.Both)]
    class PoolSpawnHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            PoolSpawnMessage spawnMessage = new PoolSpawnMessage(msg);

            MelonLoader.MelonLogger.Msg($"Spawning {spawnMessage.poolId}");

            CustomItems.SpawnFromPool(spawnMessage.poolId, spawnMessage.position, spawnMessage.rotation);

            if (peer.Type == PeerType.Server)
            {
                Server.Players.SendMessageToAllExcept(spawnMessage, SendReliability.Reliable, connection.ConnectedTo);
            }
        }
    }
}
