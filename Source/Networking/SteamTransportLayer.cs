using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Networking
{
    public class SteamTransportConnection : ITransportConnection
    {
        public ulong ConnectedTo { get; private set; }
        public bool IsConnected => IsValid;

        internal bool IsValid { get; private set; } = true;


        internal SteamTransportConnection(ulong id, P2PMessage initialMessage)
        {
            ConnectedTo = id;
            SendMessage(initialMessage, MessageSendType.Reliable);
        }

        internal SteamTransportConnection(ulong id)
        {
            ConnectedTo = id;
        }

        public void Disconnect()
        {
            SteamNetworking.CloseP2PSessionWithUser(ConnectedTo);
            IsValid = false;
        }

        public void SendMessage(P2PMessage msg, MessageSendType sendType)
        {
            SteamNetworking.SendP2PPacket(ConnectedTo, msg.GetBytes(), -1, 0, sendType == MessageSendType.Reliable ? P2PSend.Reliable : P2PSend.Unreliable);
        }
    }

    public class SteamTransportLayer : ITransportLayer
    {
        public event Action<ITransportConnection, ConnectionClosedReason> OnConnectionClosed;
        public event Action<ITransportConnection, P2PMessage> OnMessageReceived;

        private readonly Dictionary<ulong, SteamTransportConnection> connections = new Dictionary<ulong, SteamTransportConnection>();

        public SteamTransportLayer()
        {
            // Allows for the networking to fallback onto steam's servers
            SteamNetworking.AllowP2PPacketRelay(true);
        }

        private ConnectionClosedReason GetConnectionClosedReason(P2PSessionError error)
        {
            switch (error)
            {
                case P2PSessionError.NoRightsToApp:
                case P2PSessionError.DestinationNotLoggedIn:
                    return ConnectionClosedReason.Other;
                case P2PSessionError.NotRunningApp:
                    return ConnectionClosedReason.ClosedByRemote;
                case P2PSessionError.Timeout:
                    return ConnectionClosedReason.Timeout;
                default:
                    return ConnectionClosedReason.Other;
            }
        }

        public ITransportConnection ConnectTo(ulong id, P2PMessage initialMessage)
        {
            if (connections.ContainsKey(id))
            {
                if (connections[id].IsValid)
                    throw new ArgumentException("Already connected to " + id.ToString());
                else
                    connections.Remove(id);
            }

            SteamTransportConnection connection = new SteamTransportConnection(id, initialMessage);
            connections.Add(id, connection);
            SteamNetworking.OnP2PSessionRequest = ClientOnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = ClientOnP2PConnectionFailed;

            return connection;
        }

        private void ClientOnP2PSessionRequest(SteamId id)
        {
            if (connections.ContainsKey(id))
                SteamNetworking.AcceptP2PSessionWithUser(id);
        }

        private void ClientOnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            OnConnectionClosed(connections[id], GetConnectionClosedReason(error));
            connections.Remove(id);
        }

        public void StartListening()
        {
            SteamNetworking.OnP2PSessionRequest = ListenOnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = ListenOnP2PConnectionFailed;
        }

        private void ListenOnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            OnConnectionClosed(connections[id], GetConnectionClosedReason(error));
            connections.Remove(id);
        }

        private void ListenOnP2PSessionRequest(SteamId id)
        {
            SteamNetworking.AcceptP2PSessionWithUser(id);
            MelonModLogger.Log("Accepted session for " + id.ToString());
            connections.Add(id, new SteamTransportConnection(id));
        }

        public void StopListening()
        {
            SteamNetworking.OnP2PConnectionFailed = null;
            SteamNetworking.OnP2PSessionRequest = null;
        }

        public void Update()
        {
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                if (packet.HasValue)
                {
                    OnMessageReceived?.Invoke(connections[packet.Value.SteamId], new P2PMessage(packet.Value.Data));
                }
            }
        }
    }
}
