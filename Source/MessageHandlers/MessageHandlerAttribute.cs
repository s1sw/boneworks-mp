using MultiplayerMod.Networking;
using System;

namespace MultiplayerMod.MessageHandlers
{
    public enum HandlerPeer
    {
        Client,
        Server,
        Both
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageHandlerAttribute : Attribute
    {
        public MessageType Type;
        public HandlerPeer Peer;

        public MessageHandlerAttribute(MessageType type, HandlerPeer peer)
        {
            Type = type;
            Peer = peer;
        }
    }
}
