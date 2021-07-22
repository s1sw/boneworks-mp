using MultiplayerMod.Core;
using MultiplayerMod.MessageHandlers;
using MultiplayerMod.Networking;
using StressLevelZero.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.SceneTransition, PeerType.Client)]
    class SceneTransitionHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            SceneTransitionMessage stm = new SceneTransitionMessage(msg);

            if (BoneworksSceneManager.GetCurrentSceneName() != stm.sceneName)
            {
                BoneworksSceneManager.LoadScene(stm.sceneName);
            }
        }
    }
}
