using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using Facepunch.Steamworks;
using Discord;
using StressLevelZero.Props;
using StressLevelZero.Props.Weapons;

using static UnityEngine.Object;
using Utilties;
using StressLevelZero.Interaction;
using StressLevelZero.Combat;
using StressLevelZero.Utilities;
using StressLevelZero.Pool;
using StressLevelZero.AI;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;

using MultiplayerMod.Core;
using MultiplayerMod.Structs;
using MultiplayerMod.Representations;
using UnhollowerRuntimeLib;
using Valve.VR.InteractionSystem;
using StressLevelZero.Player;
using BoneworksModdingToolkit;

namespace MultiplayerMod
{
    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 30;

        private MultiplayerUI ui;
        private Client client;
        private Server server;

        internal static event Action<int> OnLevelWasLoadedEvent;
        internal static event Action<int> OnLevelWasInitializedEvent;

#if DEBUG
        PlayerRep dummyRep;
#endif

        public unsafe override void OnApplicationStart()
        {
            SteamClient.Init(823500);

#if DEBUG
            MelonModLogger.Log(ConsoleColor.Red, "Debug build!");
#endif

            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());

            // Set up prefs
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");
            ModPrefs.RegisterPrefBool("MPMod", "BaldFord", false, "90% effective hair removal solution");

            // Allows for the networking to fallback onto steam's servers
            SteamNetworking.AllowP2PPacketRelay(true);

            // Create the UI and cache the PlayerRep's model
            ui = new MultiplayerUI();
            client = new Client(ui);
            server = new Server(ui);
            PlayerRep.LoadFord();

            // Configures if the PlayerRep's are showing or hiding certain parts
            PlayerRep.showBody = true;
            PlayerRep.showHair = ModPrefs.GetBool("MPMod", "BaldFord");

            // Initialize Discord's RichPresence
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();
            
            #region Unused Code
            //PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject += PlayerHooks_OnPlayerLetGoObject;
            //BWUtil.InitialiseGunPrefabs();
            #endregion
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (level == -1) return;

            MelonModLogger.Log("Loaded scene " + level.ToString() + "(" + BoneworksSceneManager.GetSceneNameFromScenePath(level) + ") (from " + SceneManager.GetActiveScene().name + ")");

            OnLevelWasLoadedEvent?.Invoke(level);
        }
        public override void OnLevelWasInitialized(int level)
        {
            ui.Recreate();
            MelonModLogger.Log("Initialized scene " + level.ToString());
            Server.brett = GameObject.Find("[RigManager (Default Brett)]");
            Server.brett_Health = Server.brett.GetComponent<Player_Health>();
            Client.brett = GameObject.Find("[RigManager (Default Brett)]");
            Client.brett_Health = Client.brett.GetComponent<Player_Health>();
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !server.IsRunning)
            {
                // If the user is not connected, start their client and attempt a connection
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Connect(ModPrefs.GetString("MPMod", "HostSteamID"));
                    SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + client.ServerId);
                    SteamFriends.SetRichPresence("steam_player_group", client.ServerId.ToString());
                }

                // If the user is not hosting, start their server
                if (Input.GetKeyDown(KeyCode.S))
                {
                    SteamFriends.SetRichPresence("steam_display", "Hosting multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + SteamClient.SteamId);
                    SteamFriends.SetRichPresence("steam_player_group", SteamClient.SteamId.ToString());
                    server.StartServer();
                }
            }
            else
            {
                // If the user is connected, disconnect them
                if (Input.GetKeyDown(KeyCode.C))
                    client.Disconnect();

                // If the user is hosting, stop their server
                if (Input.GetKeyDown(KeyCode.S))
                {
                    MelonModLogger.Log("Stopping server...");
                    server.StopServer();
                }
            }

            if (Input.GetKeyDown(KeyCode.X))
                Features.ClientSettings.hiddenNametags = !Features.ClientSettings.hiddenNametags;

#if DEBUG
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (dummyRep == null)
                    dummyRep = new PlayerRep("Dummy", SteamClient.SteamId);
                else
                    dummyRep.Destroy();
            }

            /*if (Input.GetKeyDown(KeyCode.A))
            {
                zCubed.Accessories.Accessory.CreateDummies(zCubed.Accessories.Accessory.GetPlayerRoot());
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                zCubed.Accessories.Accessory.CreateLocalAccessories(zCubed.Accessories.Accessory.GetPlayerRoot());
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                string[] accessoryPaths = Features.NetworkedAccesories.GetLocalList();
                byte[] rawData = Features.NetworkedAccesories.BundleToNetBundle(accessoryPaths[0]);
            }*/
#endif
        }

        public override void OnFixedUpdate()
        {
            if (client.isConnected)
                client.Update();

            if (server.IsRunning)
                server.Update();
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
                client.Disconnect();

            if (server.IsRunning)
                server.StopServer();
        }

    }
}
