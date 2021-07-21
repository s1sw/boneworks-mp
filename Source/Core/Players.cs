using MultiplayerMod.Networking;
using System.Collections;
using System.Collections.Generic;

namespace MultiplayerMod.Core
{
    /// <summary>
    /// Collection of players in a game. Enables looking up players by full + small IDs and iterating over the list of
    /// players in the game.
    /// </summary>
    public class Players : IEnumerable<MPPlayer>
    {
        public int Count => playerList.Count;

        // We frequently need to look up players with different IDs and also iterate over them.
        // It's therefore useful to maintain multiple structures for players.
        private readonly Dictionary<ulong, MPPlayer> fullIdPlayers = new Dictionary<ulong, MPPlayer>();
        private readonly Dictionary<byte, MPPlayer> smallIdPlayers = new Dictionary<byte, MPPlayer>();
        private readonly List<MPPlayer> playerList = new List<MPPlayer>();

        public IEnumerator<MPPlayer> GetEnumerator() => playerList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => playerList.GetEnumerator();

        /// <summary>
        /// Adds a new player.
        /// </summary>
        public void Add(MPPlayer player)
        {
            fullIdPlayers.Add(player.FullID, player);
            smallIdPlayers.Add(player.SmallID, player);
            playerList.Add(player);
        }

        /// <summary>
        /// Removes a player. 
        /// </summary>
        public void Remove(MPPlayer player, bool destroyRep = true)
        {
            if (destroyRep)
                player.Destroy();

            fullIdPlayers.Remove(player.FullID);
            smallIdPlayers.Remove(player.SmallID);
            playerList.Remove(player);
        }

        /// <summary>
        /// Removes a player by their full ID.
        /// </summary>
        public void Remove(ulong fullId, bool destroyRep = true)
        {
            Remove(fullIdPlayers[fullId], destroyRep);
        }

        /// <summary>
        /// Removes a player by their small ID.
        /// </summary>
        public void Remove(byte smallId, bool destroyRep = true)
        {
            Remove(smallIdPlayers[smallId], destroyRep);
        }

        public void Clear(bool destroyReps = true)
        {
            foreach (MPPlayer player in playerList)
            {
                player.Destroy();
            }

            fullIdPlayers.Clear();
            smallIdPlayers.Clear();
            playerList.Clear();
        }

        public bool Contains(ulong fullId) => fullIdPlayers.ContainsKey(fullId);
        public bool Contains(byte smallId) => smallIdPlayers.ContainsKey(smallId);

        public void SendMessageToAll(INetworkMessage msg, MessageSendType send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in playerList)
            {
                p.Connection.SendMessage(pMsg, send);
            }
        }

        public void SendMessageToAllExcept(INetworkMessage msg, MessageSendType send, ulong except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in playerList)
            {
                if (p.FullID != except)
                    p.Connection.SendMessage(pMsg, send);
            }
        }

        /// <summary>
        /// Accesses a player by their full ID.
        /// </summary>
        /// <param name="fullId">A player's full ID.</param>
        /// <returns>The player with that full ID.</returns>
        public MPPlayer this[ulong fullId]
        {
            get => fullIdPlayers[fullId];
        }

        /// <summary>
        /// Accesses a player by their small ID.
        /// </summary>
        /// <param name="smallId">A player's small ID.</param>
        /// <returns>The player with that full ID.</returns>
        public MPPlayer this[byte smallId]
        {
            get => smallIdPlayers[smallId];
        }
    }
}
