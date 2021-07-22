using Discord;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Core
{
    public static class RichPresence
    {
        private static Discord.Discord discord;

        public static event Action<string> OnJoin;

        public static void Initialise(long clientId)
        {
            discord = new Discord.Discord(clientId, 0);
            discord.GetActivityManager().RegisterSteam(823500);
            discord.GetActivityManager().UpdateActivity(new Activity() { Details = "Idle" }, ActivityUpdateHandler);
            discord.GetActivityManager().OnActivityJoin += RichPresence_OnActivityJoin;
        }

        private static void RichPresence_OnActivityJoin(string secret)
        {
            OnJoin?.Invoke(secret);
        }

        private static void ActivityUpdateHandler(Result res)
        {
            MelonLogger.Msg("Got result " + res.ToString() + " when updating activity");
        }

        public static void Update()
        {
            discord.RunCallbacks();
        }

        public static void SetActivity(Activity act)
        {
            discord.GetActivityManager().UpdateActivity(act, ActivityUpdateHandler);
        }
    }
}
