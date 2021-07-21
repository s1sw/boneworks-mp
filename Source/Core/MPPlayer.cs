using MultiplayerMod.Networking;
using MultiplayerMod.Representations;

namespace MultiplayerMod.Core
{
    /// <summary>
    /// Catch-all class representing a player in the game.
    /// </summary>
    public class MPPlayer
    {
        public readonly PlayerRep PlayerRep;
        public readonly string Name;
        public readonly ulong FullID;
        public readonly byte SmallID;
        public readonly ITransportConnection Connection;

        public MPPlayer(string name, ulong fullId, byte smallId, ITransportConnection connection)
        {
            PlayerRep = new PlayerRep(name, fullId);
            Name = name;
            FullID = fullId;
            SmallID = smallId;
            Connection = connection;
        }

        public void Destroy()
        {
            PlayerRep.Delete();
        }
    }
}
