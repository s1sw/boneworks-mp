using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using StressLevelZero.Utilities;

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
