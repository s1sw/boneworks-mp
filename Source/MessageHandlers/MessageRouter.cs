using MultiplayerMod.Networking;
using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MultiplayerMod.Core;

namespace MultiplayerMod.MessageHandlers
{
    public class MessageRouter
    {
        private Dictionary<MessageType, MessageHandler> handlers = new Dictionary<MessageType, MessageHandler>();

        public MessageRouter(Players players, HandlerPeer peer)
        {
            if (peer == HandlerPeer.Both) 
                throw new ArgumentException("Can't create a message router routing for server and client");

            Assembly assembly = Assembly.GetCallingAssembly();

            foreach (Type type in assembly.GetTypes())
            {
                var attributes = (MessageHandlerAttribute[])type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);

                if (attributes.Length == 0) continue;

                if (!typeof(MessageHandler).IsAssignableFrom(type))
                    throw new ApplicationException("Can't have a message handler that doesn't implement IMessageHandler");

                var instance = (MessageHandler)Activator.CreateInstance(type, players);

                foreach (MessageHandlerAttribute attribute in attributes)
                {
                    if (attribute.Peer == HandlerPeer.Both || attribute.Peer == peer)
                        handlers.Add(attribute.Type, instance);
                }
            }
        }

        public void HandleMessage(ITransportConnection connection, P2PMessage msg)
        {
            MessageType msgType = (MessageType)msg.ReadByte();

            if (!handlers.TryGetValue(msgType, out MessageHandler handler))
            {
                MelonLogger.LogWarning($"Unknown message type: {msgType}");
                return;
            }

            handler.HandleMessage(msgType, connection, msg);
        }
    }
}
