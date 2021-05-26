﻿using MelonLoader;
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
using MultiplayerMod.MonoBehaviours;

namespace MultiplayerMod
{
    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 31;

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

#if DEBUG
            MelonModLogger.LogWarning("Debug build!");
#endif

            MelonLogger.Log($"Multiplayer initialising with protocol version {PROTOCOL_VERSION}. Special brick sync build!");

            // Set up prefs
            MelonPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            MelonPrefs.RegisterBool("MPMod", "BaldFord", false, "90% effective hair removal solution");

            // Initialise transport layer
            TransportLayer = new SteamTransportLayer();

            // Create the UI and cache the PlayerRep's model
            ui = new MultiplayerUI();
            client = new Client(ui, TransportLayer);
            server = new Server(ui, TransportLayer);
            PlayerRep.LoadFord();

            // Configures if the PlayerRep's are showing or hiding certain parts
            PlayerRep.showBody = true;
            PlayerRep.showHair = MelonPrefs.GetBool("MPMod", "BaldFord");

            // Initialize Discord's RichPresence
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

            BWUtil.Hook();

            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<SyncedObject>();
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (level == -1) return;

            MelonLogger.Log("Loaded scene " + level.ToString() + "(" + BoneworksSceneManager.GetSceneNameFromScenePath(level) + ") (from " + SceneManager.GetActiveScene().name + ")");

            OnLevelWasLoadedEvent?.Invoke(level);
            BWUtil.UpdateGunOffset();
        }

        public override void OnLevelWasInitialized(int level)
        {
            ui.Recreate();
            MelonLogger.Log("Initialized scene " + level.ToString());
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !server.IsRunning)
            {
                //Shitty method to fix invisible invites when restarting servers, sure theres better methods but im done with this
                RichPresence.ResetActivity(false);
                // This used to be used to connect to a server by using the SteamID in a config file,
                // but now it only causes confusion.
                if (Input.GetKeyDown(KeyCode.C))
                {
                    MelonLogger.LogError("Manually connecting to a server with the C keybind has been removed. Please use Discord invites.");
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
                    MelonLogger.Log("Stopping server...");
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
                    dummyRep.Delete();
            }

            if (GUILayout.Button("Create Main Panel", null))
            {
                Features.UI.CreateMainPanel();
            }

            if (GUILayout.Button("Test Object IDs", null))
            {
                var testObj = GameObject.Find("[RigManager (Default Brett)]/[SkeletonRig (GameWorld Brett)]/Brett@neutral");
                string fullPath = BWUtil.GetFullNamePath(testObj);

                MelonLogger.Log($"Got path {fullPath} for Brett@neutral");
                MelonLogger.Log("Trying to get object from path...");

                var gotObj = BWUtil.GetObjectFromFullPath(fullPath);

                if (gotObj == testObj)
                {
                    MelonLogger.Log("Success!!!!!");
                }
                else
                {
                    MelonLogger.Log($"Failed :( Got {gotObj.name}");
                }
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
