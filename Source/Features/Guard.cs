using Facepunch.Steamworks;
using MelonLoader;
using System;
using System.IO;
using System.Collections.Generic;

namespace MultiplayerMod.Features
{
    public static class Guard
    {
        public enum GuardLevel
        {
            Maximum,
            FriendsOnly,
            TrustedOnly,
            None
        }

        public enum Lists
        {
            Blocked,
            Trusted
        };

        public static GuardLevel guardLevel = GuardLevel.Maximum;

        public static List<string> friendUsers = new List<string>();
        public static List<string> trustedUsers = new List<string>();
        public static List<string> blockedUsers = new List<string>();

        public static void GetSteamFriends()
        {
            try
            {
                IEnumerable<Friend> friends = SteamFriends.GetFriends();

                foreach (Friend friend in friends)
                {
#if DEBUG
                    MelonLoader.MelonLogger.Log($"DEBUG - Friend: {friend.Name}");
#endif

                    friendUsers.Add(friend.Name);
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.LogError(e.Message);
            }
        }

        public static void GetLocalGuard()
        {
            trustedUsers.Clear();
            blockedUsers.Clear();

            try
            {
                string path = UnityEngine.Application.dataPath.Replace("BONEWORKS_Data", "UserData/Mutliplayer/Guard");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string trustedPath = $"{path}/Trusted.users";
                string blockedPath = $"{path}/Blocked.users";

                if (!File.Exists(trustedPath))
                    File.WriteAllText(trustedPath, "");

                if (!File.Exists(blockedPath))
                    File.WriteAllText(blockedPath, "");

                string[] local_trustedUsers = File.ReadAllLines(trustedPath);
                string[] local_blockedUsers = File.ReadAllLines(blockedPath);

                foreach (string user in local_trustedUsers)
                {
#if DEBUG
                    MelonLogger.Log($"DEBUG - Trusted: {user}");
#endif

                    trustedUsers.Add(user);
                }

                foreach (string user in local_blockedUsers)
                {
#if DEBUG
                    MelonLogger.Log($"DEBUG - Blocked: {user}");
#endif

                    blockedUsers.Add(user);
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogError(e.Message);
            }
        }

        public static void AddUserToList(string entry, Lists list)
        {
            string path = UnityEngine.Application.dataPath.Replace("BONEWORKS_Data", "UserData/Mutliplayer/Guard");

            switch (list)
            {
                case Lists.Blocked:
                    path += "/Blocked.users";
                    break;

                case Lists.Trusted:
                    path += "/Trusted.users";
                    break;
            }

            if (!File.Exists(path))
                File.WriteAllText(path, "");

            string[] trustedLines = File.ReadAllLines(path);
            string[] new_trustedLines = new string[trustedLines.Length + 1];

            for (int l = 0; l < trustedLines.Length; l++)
                new_trustedLines[l] = trustedLines[l];

            new_trustedLines[new_trustedLines.Length - 1] = entry;

            File.WriteAllLines(path, new_trustedLines);

            GetLocalGuard();
        }
    }
}
