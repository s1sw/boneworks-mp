using MultiplayerMod.Core;
using MultiplayerMod.Networking;
using System;

namespace MultiplayerMod.MessageHandlers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageHandlerAttribute : Attribute
    {
        public MessageType Type;
        public PeerType Peer;

        public MessageHandlerAttribute(MessageType type, PeerType peer)
        {
            Type = type;
            Peer = peer;
        }
    }
}
