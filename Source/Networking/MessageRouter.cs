using MultiplayerMod.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace MultiplayerMod.Networking
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class MessageHandlerAttribute : Attribute
    {
        public readonly MessageType type;

        public MessageHandlerAttribute(MessageType type)
        {
            this.type = type;
        }
    }

    public class MessageRouter
    {
        private Dictionary<MessageType, IMessageHandler> messageHandlers = new Dictionary<MessageType, IMessageHandler>();

        public MessageRouter()
        {
            // TODO: Search for message handlers and add them to the dictionary
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = type.GetCustomAttribute(typeof(MessageHandlerAttribute));
                if (attr != null)
                {
                    messageHandlers.Add(((MessageHandlerAttribute)attr).type, (IMessageHandler)Activator.CreateInstance(type));
                }
            }
        }

        public void RouteMessage(P2PMessage msg, ITransportConnection connection, NetController controller)
        {
            MessageType type = (MessageType)msg.ReadByte();

            messageHandlers[type].HandleMessage(msg, connection, controller);
        }
    }
}
