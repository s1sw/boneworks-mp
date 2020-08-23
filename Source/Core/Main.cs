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
using MultiplayerMod.Networking;

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
        internal static ITransportLayer TransportLayer;

#if DEBUG
        PlayerRep dummyRep;
#endif

        public unsafe override void OnApplicationStart()
        {
            if (!SteamClient.IsValid)
            SteamClient.Init(823500);

            Features.Guard.GetSteamFriends();
            Features.Guard.GetLocalGuard();

#if DEBUG
            MelonModLogger.LogWarning("Debug build!");
#endif

            MelonModLogger.Log($"Multiplayer initialising with protocol version {PROTOCOL_VERSION}.");

            // Set up prefs
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefBool("MPMod", "BaldFord", false, "90% effective hair removal solution");

            // Initialise transport layer
            TransportLayer = new SteamTransportLayer();

            // Create the UI and cache the PlayerRep's model
            ui = new MultiplayerUI();
            client = new Client(ui, TransportLayer);
            server = new Server(ui, TransportLayer);
            PlayerRep.LoadFord();

            // Configures if the PlayerRep's are showing or hiding certain parts
            PlayerRep.showBody = true;
            PlayerRep.showHair = ModPrefs.GetBool("MPMod", "BaldFord");

            // Initialize Discord's RichPresence
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

            Extras.GunResources.Load();
            BWUtil.Hook();
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
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !server.IsRunning)
            {
                // This used to be used to connect to a server by using the SteamID in a config file,
                // but now it only causes confusion.
                if (Input.GetKeyDown(KeyCode.C))
                {
                    MelonModLogger.LogError("Manually connecting to a server with the C keybind has been removed. Please use Discord invites.");
                }

                // If the user is not hosting, start their server
                if (Input.GetKeyDown(KeyCode.S))
                {
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
        }

        public override void OnGUI()
        {
#if DEBUG
            GUILayout.BeginVertical(null);

            if (GUILayout.Button("Create Dummy", null))
            {
                if (dummyRep == null)
                    dummyRep = new PlayerRep("Dummy", SteamClient.SteamId);
                else
                    dummyRep.Destroy();
            }

            if (GUILayout.Button("Create Dummy Accessories", null))
            {
                zCubed.Accessories.Accessory.CreateDummies(zCubed.Accessories.Accessory.GetPlayerRoot());
            }

            if (GUILayout.Button("Create Local Accessories", null))
            {
                zCubed.Accessories.Accessory.CreateLocalAccessories(zCubed.Accessories.Accessory.GetPlayerRoot());
            }

            if (GUILayout.Button("Create Net Accessories", null))
            {
                string[] accessoryPaths = Features.NetworkedAccesories.GetLocalList();
                byte[] rawData = Features.NetworkedAccesories.BundleToNetBundle(accessoryPaths[0]);
            }

            if (GUILayout.Button("Test Guard Lists", null))
            {
                Features.Guard.AddUserToList(Guid.NewGuid().ToString(), Features.Guard.Lists.Blocked);
                Features.Guard.AddUserToList(Guid.NewGuid().ToString(), Features.Guard.Lists.Trusted);
            }

            if (GUILayout.Button("Create Main Panel", null))
            {
                Features.UI.CreateMainPanel();
            }

            GUILayout.EndVertical();
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
