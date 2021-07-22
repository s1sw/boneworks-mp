using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers
{
    public class MessageRouter
    {
        private readonly Dictionary<MessageType, MessageHandler> handlers = new Dictionary<MessageType, MessageHandler>();

        public MessageRouter(Players players, Peer peer)
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            // Find and register all message handlers
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = (MessageHandlerAttribute[])type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);

                if (attributes.Length == 0) continue;

                if (!typeof(MessageHandler).IsAssignableFrom(type))
                    throw new ApplicationException("Can't have a message handler that isn't derived from MessageHandler");

                var instance = (MessageHandler)Activator.CreateInstance(type);

                instance.Init(players, peer);

                foreach (MessageHandlerAttribute attribute in attributes)
                {
                    if (attribute.Peer == PeerType.Both || attribute.Peer == peer.Type)
                        handlers.Add(attribute.Type, instance);
                }
            }
        }

        public void HandleMessage(ITransportConnection connection, P2PMessage msg)
        {
            MessageType msgType = (MessageType)msg.ReadByte();

            if (!handlers.TryGetValue(msgType, out MessageHandler handler))
            {
                MelonLogger.Warning($"Unknown message type: {msgType}");
                return;
            }

            try
            {
                handler.HandleMessage(msgType, connection, msg);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Caught exception in message handler for message {msgType}: {e}");
            }
        }
    }
}
