﻿using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
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
            SendMessage(initialMessage, SendReliability.Reliable);

            MelonLogger.Msg($"Steam: Sent initial message to {id}");
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

        public void SendMessage(P2PMessage msg, SendReliability sendType)
        {
            SteamTransportLayer.StartThreadIfNecessary();
            SteamTransportLayer.messageSendCmds.Enqueue(new SteamTransportLayer.MessageSendCmd() { msg = msg, sendType = sendType, id = ConnectedTo });
        }
    }

    public class SteamTransportLayer : ITransportLayer
    {
        internal struct MessageSendCmd
        {
            public P2PMessage msg;
            public SendReliability sendType;
            public ulong id;
        }

        public event Action<ITransportConnection, ConnectionClosedReason> OnConnectionClosed;
        public event Action<ITransportConnection, P2PMessage> OnMessageReceived;

        private static readonly Thread msgThread = new Thread(SendMessagesThread);
        private readonly Dictionary<ulong, SteamTransportConnection> connections = new Dictionary<ulong, SteamTransportConnection>();
        internal static readonly ConcurrentQueue<MessageSendCmd> messageSendCmds = new ConcurrentQueue<MessageSendCmd>();

        public SteamTransportLayer()
        {
            // Allows for the networking to fallback onto steam's servers
            SteamNetworking.AllowP2PPacketRelay(true);
        }

        internal static void StartThreadIfNecessary()
        {
            if (!msgThread.IsAlive)
            {
                msgThread.Start();
            }
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

            MelonLogger.Msg($"Steam: Connecting to {id}");
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
            MelonLogger.Msg("Accepted session for " + id.ToString());
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

        private static void SendMessagesThread()
        {
            while (true)
            {
                try
                {
                    if (messageSendCmds.Count == 0)
                        Thread.Sleep(15);

                    while (messageSendCmds.Count > 0)
                    {
                        MessageSendCmd sendCmd;
                        while (!messageSendCmds.TryDequeue(out sendCmd)) continue;
                        SteamNetworking.SendP2PPacket(sendCmd.id, sendCmd.msg.GetBytes(), -1, 0, sendCmd.sendType == SendReliability.Reliable ? P2PSend.Reliable : P2PSend.Unreliable);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"Caught exception in message send thread: {e}");
                }
            }
        }
    }
}
