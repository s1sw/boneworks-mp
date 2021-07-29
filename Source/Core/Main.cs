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
using StressLevelZero.Combat;
using StressLevelZero.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MultiplayerMod
{
    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 31;

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
            MelonLogger.Warning("Debug build!");
#endif

            MelonLogger.Msg($"Multiplayer initialising with protocol version {PROTOCOL_VERSION}.");

            // Initialise transport layer
            TransportLayer = new SteamTransportLayer();

            client = new Client(TransportLayer);
            server = new Server(TransportLayer);

            // Cache the PlayerRep's model
            PlayerRep.LoadFord();

            // Initialise Boneworks hooks
            BWUtil.Hook();

            // Initialize Discord's RichPresence
            //RichPresence.Initialise(701895326600265879);
            //client.SetupRP();

            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<SyncedObject>();

            menuCategory = MenuManager.CreateCategory("Boneworks Multiplayer", Color.white);
            menuCategory.CreateBoolElement("Show Nameplates", Color.white, false, 
                (bool val) => Features.ClientSettings.hiddenNametags = val);
        }

        private string lastStatusDisplayText = "0/0 players";
        private void UpdateBoneMenu()
        {
            menuCategory.RemoveElement("Start Server");
            menuCategory.RemoveElement("Stop Server");
            menuCategory.RemoveElement("Disconnect");
            menuCategory.RemoveElement(lastStatusDisplayText);

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

                    lastStatusDisplayText = $"{client.Players.Count} / {MAX_PLAYERS} players";
                }
                else if (server.IsRunning)
                {
                    menuCategory.CreateFunctionElement("Stop Server", Color.white,
                        () => {
                            if (!server.IsRunning) return;
                            server.StopServer();
                            UpdateBoneMenu();
                        });

                    lastStatusDisplayText = $"{server.Players.Count} / {MAX_PLAYERS} players";
                }

                menuCategory.CreateFunctionElement(lastStatusDisplayText, Color.white, null);
            }

            // Janky reflection stuff to get the menu to update and display our new elements
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
            MelonLogger.Msg($"Initialized scene {name}");
            UpdateBoneMenu();
        }

        public override void OnUpdate()
        {
            //RichPresence.Update();

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
                {
                    MPPlayer fakePlayer = new MPPlayer("Rabscuttle", 76561197960287930, 0, null);
                    dummyRep = new PlayerRep("Dummy", fakePlayer);
                    dummyRep.OnDamage += DummyRep_OnDamage;
                }
                else
                    dummyRep.Delete();
            }

            if (GUILayout.Button("Test Object IDs", null))
            {
                var testObj = GameObject.Find("[RigManager (Default Brett)]/[SkeletonRig (GameWorld Brett)]/Brett@neutral");
                string fullPath = BWUtil.GetFullNamePath(testObj);

                MelonLogger.Msg($"Got path {fullPath} for Brett@neutral");
                MelonLogger.Msg("Trying to get object from path...");
                var gotObj = BWUtil.GetObjectFromFullPath(fullPath);

                if (gotObj == testObj)
                {
                    MelonLogger.Msg("Success!!!!!");
                }
                else
                {
                    MelonLogger.Msg($"Failed :( Got {gotObj.name}");
                }
            }

            if (GUILayout.Button("Create testing damage receiver", null))
            {
                GameObject testReceiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var attackRecv = testReceiver.AddComponent<GenericAttackReceiver>();
                attackRecv.AttackEvent = new UnityEventFloat();
                attackRecv.AttackEvent.AddListener(new Action<float>((float f) => { MelonLogger.Msg($"Received {f} damage"); }));

                ImpactPropertiesManager bloodManager = testReceiver.AddComponent<ImpactPropertiesManager>();
                bloodManager.material = ImpactPropertiesVariables.Material.Blood;
                bloodManager.modelType = ImpactPropertiesVariables.ModelType.Model;
                bloodManager.MainColor = Color.red;
                bloodManager.SecondaryColor = Color.red;
                bloodManager.PenetrationResistance = 0.9f;
                bloodManager.megaPascalModifier = 5f;
                
                ImpactProperties blood = testReceiver.AddComponent<ImpactProperties>();
                blood.material = ImpactPropertiesVariables.Material.Blood;
                blood.modelType = ImpactPropertiesVariables.ModelType.Model;
                blood.MainColor = Color.red;
                blood.SecondaryColor = Color.red;
                blood.PenetrationResistance = 0.9f;
                blood.megaPascalModifier = 5f;
                blood.MyCollider = testReceiver.GetComponent<Collider>();
                blood.hasManager = true;
                blood.Manager = bloodManager;
                testReceiver.AddComponent<Rigidbody>().isKinematic = true;
            }

            GUILayout.EndVertical();
#endif
        }

        private void DummyRep_OnDamage(float arg1, PlayerRep arg2)
        {
            MelonLogger.Msg($"Dummy rep took {arg1} damage");
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
