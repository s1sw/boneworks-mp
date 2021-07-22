using System;
using System.Reflection;
using Facepunch.Steamworks;
using MelonLoader;
using ModThatIsNotMod.BoneMenu;
using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.MonoBehaviours;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using StressLevelZero.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private MenuCategory menuCategory;

        internal static event Action<int> OnLevelWasLoadedEvent;
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

            MelonLogger.Msg($"Multiplayer initialising with protocol version {PROTOCOL_VERSION}.");

            // Initialise transport layer
            TransportLayer = new SteamTransportLayer();

            // Create the UI and cache the PlayerRep's model
            ui = new MultiplayerUI();
            client = new Client(ui, TransportLayer);
            server = new Server(ui, TransportLayer);
            PlayerRep.LoadFord();

            // Configures if the PlayerRep's are showing or hiding certain parts
            PlayerRep.showBody = true;

            // Initialize Discord's RichPresence
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<SyncedObject>();

            menuCategory = MenuManager.CreateCategory("Boneworks Multiplayer", Color.white);
            menuCategory.CreateBoolElement("Show Nameplates", Color.white, false, 
                (bool val) => Features.ClientSettings.hiddenNametags = val);
        }

        private void UpdateBoneMenu()
        {
            menuCategory.RemoveElement("Start Server");
            menuCategory.RemoveElement("Stop Server");
            menuCategory.RemoveElement("Disconnect");

            if (!client.IsConnected && !server.IsRunning)
            {
                menuCategory.CreateFunctionElement("Start Server", Color.white,
                    () => {
                        if (server.IsRunning) return;
                        server.StartServer();
                        UpdateBoneMenu(); 
                    });
            }
            else
            {
                if (client.IsConnected)
                {
                    menuCategory.CreateFunctionElement("Disconnect", Color.white,
                        () => {
                            if (!client.IsConnected) return;
                            client.Disconnect();
                            UpdateBoneMenu();
                        });
                }
                else if (server.IsRunning)
                {
                    menuCategory.CreateFunctionElement("Stop Server", Color.white,
                        () => {
                            if (!server.IsRunning) return;
                            server.StopServer();
                            UpdateBoneMenu();
                        });
                }
            }

            Type mmType = typeof(MenuManager);
            FieldInfo categoryField = mmType.GetField("activeCategory", BindingFlags.NonPublic | BindingFlags.Static);

            if (categoryField.GetValue(null) == menuCategory)
            {
                MethodInfo openCategory = mmType.GetMethod("OpenCategory", BindingFlags.NonPublic | BindingFlags.Static);
                openCategory.Invoke(null, new object[] { menuCategory });
            }
        }

        public override void OnSceneWasLoaded(int level, string name)
        {
            if (level == -1) return;

            MelonLogger.Msg($"Loaded scene {level} ({BoneworksSceneManager.GetSceneNameFromScenePath(level)} (from {SceneManager.GetActiveScene().name})");

            OnLevelWasLoadedEvent?.Invoke(level);
            BWUtil.UpdateGunOffset();
        }

        public override void OnSceneWasInitialized(int level, string name)
        {
            ui.Recreate();
            MelonLogger.Msg($"Initialized scene {name}");
            UpdateBoneMenu();
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (Input.GetKeyDown(KeyCode.U))
                UpdateBoneMenu();

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
            if (client.IsConnected)
                client.Update();

            if (server.IsRunning)
                server.Update();
        }

        public override void OnApplicationQuit()
        {
            if (client.IsConnected)
                client.Disconnect();

            if (server.IsRunning)
                server.StopServer();
        }
    }
}
