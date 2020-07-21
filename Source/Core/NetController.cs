using Facepunch.Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerMod.Representations;
using MultiplayerMod.Networking;

namespace MultiplayerMod.Core
{
    public class PlayerInfo
    {
        public string name;
        public ulong netID;
        public byte smallID;
        public PlayerRep playerRep;
        public ITransportConnection connection;
    }

    // Handles common functionality of both the server and the client
    public class NetController
    {
        private readonly Dictionary<byte, PlayerInfo> smallIdPlayers = new Dictionary<byte, PlayerInfo>();
        private readonly Dictionary<ulong, PlayerInfo> largeIdPlayers = new Dictionary<ulong, PlayerInfo>();
        private readonly MessageRouter messageRouter = new MessageRouter();
        private byte smallIDCounter = 0;
        protected ITransportLayer transportLayer;

        public NetController(ITransportLayer layer)
        {
            transportLayer = layer;
            transportLayer.OnMessageReceived += OnMessageReceived;
        }

        protected void HandlePlayerLeave(ulong largeId)
        {
            // Remove all the player info of the disconnected player
            byte smallId = largeIdPlayers[largeId].smallID;

            smallIdPlayers.Remove(smallId);
            largeIdPlayers.Remove(largeId);
        }

        private void OnMessageReceived(ITransportConnection connection, P2PMessage msg)
        {
            messageRouter.RouteMessage(msg, connection, this);
        }

        public void Update()
        {
            transportLayer.Update();
        }

        public PlayerInfo GetPlayerInfo(byte smallId) => smallIdPlayers[smallId];
        public PlayerInfo GetPlayerInfo(ulong largeId) => largeIdPlayers[largeId];
        public int GetPlayerCount() => smallIdPlayers.Count;

        // Returns the new player ID
        public byte RegisterNewPlayer(ITransportConnection connection, string name)
        {
            byte newPlayerId = smallIDCounter;

            PlayerInfo newPlayerInfo = new PlayerInfo() {
                smallID = newPlayerId,
                netID = connection.ConnectedTo,
                playerRep = new PlayerRep(name, connection.ConnectedTo),
                connection = connection
            };

            smallIdPlayers.Add(newPlayerId, newPlayerInfo);
            largeIdPlayers.Add(connection.ConnectedTo, newPlayerInfo);

            return smallIDCounter++;
        }
    }
}
