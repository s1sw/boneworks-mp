using System;
using System.Collections;
using System.Collections.Generic;
using MultiplayerMod.Networking;

namespace MultiplayerMod.Core
{
    /// <summary>
    /// Collection of players in a game. Enables looking up players by full + small IDs and iterating over the list of
    /// players in the game.
    /// </summary>
    public class Players : IEnumerable<MPPlayer>
    {
        public int Count => playerList.Count;
        public event Action<MPPlayer> OnPlayerAdd;
        public event Action<MPPlayer> OnPlayerRemove;

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
            OnPlayerAdd?.Invoke(player);
        }

        /// <summary>
        /// Removes a player. 
        /// </summary>
        /// <param name="destroyRep">If true, the player's representation GameObject is destroyed.</param>
        public void Remove(MPPlayer player, bool destroyRep = true)
        {
            if (destroyRep)
                player.Destroy();

            fullIdPlayers.Remove(player.FullID);
            smallIdPlayers.Remove(player.SmallID);
            playerList.Remove(player);
            OnPlayerRemove?.Invoke(player);
        }

        /// <summary>
        /// Removes a player by their full ID.
        /// </summary>
        /// <param name="destroyRep">If true, the player's representation GameObject is destroyed.</param>
        public void Remove(ulong fullId, bool destroyRep = true)
        {
            Remove(fullIdPlayers[fullId], destroyRep);
        }

        /// <summary>
        /// Removes a player by their small ID.
        /// </summary>
        /// <param name="destroyRep">If true, the player's representation GameObject is destroyed.</param>
        public void Remove(byte smallId, bool destroyRep = true)
        {
            Remove(smallIdPlayers[smallId], destroyRep);
        }

        /// <summary>
        /// Removes all players from the list.
        /// </summary>
        /// <param name="destroyReps">If true, the representation GameObjects of every player are destroyed.</param>
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

        /// <summary>
        /// Sends a message directly to every player in the list. Should really only be used by servers.
        /// </summary>
        public void SendMessageToAll(INetworkMessage msg, SendReliability reliability)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in playerList)
            {
                p.Connection.SendMessage(pMsg, reliability);
            }
        }

        /// <summary>
        /// Sends a message directly to every player in the list except the specified full ID. 
        /// Should really only be used by servers.
        /// </summary>
        /// <param name="except">The full ID of the player to exclude from being sent the message.</param>
        public void SendMessageToAllExcept(INetworkMessage msg, SendReliability reliability, ulong except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (MPPlayer p in playerList)
            {
                if (p.FullID != except)
                    p.Connection.SendMessage(pMsg, reliability);
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
