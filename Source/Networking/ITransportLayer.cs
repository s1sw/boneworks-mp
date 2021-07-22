using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Networking
{
    public enum SendReliability
    {
        Reliable,
        Unreliable
    }

    public enum ConnectionClosedReason
    {
        Timeout,
        ClosedByRemote,
        Other
    }

    public enum TransportLayers
    {
        Steam,
        Discord
    }

    public interface ITransportConnection
    {
        ulong ConnectedTo { get; }
        bool IsConnected { get; }
        void SendMessage(P2PMessage msg, SendReliability sendType);
        void Disconnect();
    }

    public interface ITransportLayer
    {
        event Action<ITransportConnection, P2PMessage> OnMessageReceived;
        event Action<ITransportConnection, ConnectionClosedReason> OnConnectionClosed;
        void StartListening();
        void StopListening();
        void Update();
        ITransportConnection ConnectTo(ulong id, P2PMessage initialMessage);
    }
}
