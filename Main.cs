using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using NET_SDK;
using Facepunch.Steamworks;
using Discord;
using StressLevelZero.Props;
using StressLevelZero.Props.Weapons;
using BoneHook;

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

namespace MultiplayerMod
{
    public static class FileInfo
    {
        public const string Name = "Multiplayer Mod";
        public const string Author = "Someone Somewhere";
        public const string Company = "Lava Gang";
        public const string Version = "0.11.0";
        public const string DownloadLink = "https://discord.gg/2Wn3N2P";
    }

    public struct BoneworksRigTransforms
    {
        public Transform main;
        public Transform root;
        public Transform lHip;
        public Transform rHip;
        public Transform spine1;
        public Transform spine2;
        public Transform spineTop;
        public Transform lClavicle;
        public Transform rClavicle;
        public Transform neck;
        public Transform lShoulder;
        public Transform rShoulder;
        public Transform lElbow;
        public Transform rElbow;
        public Transform lKnee;
        public Transform rKnee;
        public Transform lAnkle;
        public Transform rAnkle;
        public Transform lWrist;
        public Transform rWrist;
    }


    public enum MessageType
    {
        Join,
        PlayerName,
        OtherPlayerName,
        PlayerPosition,
        OtherPlayerPosition,
        Disconnect,
        ServerShutdown,
        JoinRejected,
        SceneTransition,
        FullRig,
        OtherFullRig,
        HandGunChange,
        OtherHandGunChange,
        SetPartyId,
        EnemyRigTransform,
        Attack
    }

    public partial class MultiplayerMod : MelonMod
    {
        // TODO: Enforce player limit
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 30;

        private bool isServer = false;

        private MultiplayerUI ui;
        private readonly Client client = new Client();
        private readonly Server server = new Server();

        internal static event Action<int> OnLevelWasLoadedEvent;
        internal static event Action<int> OnLevelWasInitializedEvent;

        public unsafe override void OnApplicationStart()
        {
            try
            {
                if (!SteamClient.IsValid)
                    SteamClient.Init(823500);
            }
            catch (Exception e)
            {
                MelonModLogger.LogError("Caught exception while initialising Steam client. This is likely a result of having the Boneworks Modding Toolkit installed.");
            }
            MelonModLogger.Log("Multiplayer initialising with SteamID " + SteamClient.SteamId.ToString() + ". Protocol version " + PROTOCOL_VERSION.ToString());
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");
            ModPrefs.RegisterPrefBool("MPMod", "BaldFord", false, "90% effective hair removal solution");

            SteamNetworking.AllowP2PPacketRelay(true);
            ui = new MultiplayerUI();
            PlayerRep.LoadFord();

            PlayerRep.hideBody = false;
            PlayerRep.showHair = ModPrefs.GetBool("MPMod", "BaldFord");
            RichPresence.Initialise(701895326600265879);
            client.SetupRP();

            //PlayerHooks.OnPlayerGrabObject += PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject += PlayerHooks_OnPlayerLetGoObject;
            //BWUtil.InitialiseGunPrefabs();
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (level == -1) return;
            MelonModLogger.Log("Loaded scene " + level.ToString() + "(" + BoneworksSceneManager.GetSceneNameFromScenePath(level) + ") (from " + SceneManager.GetActiveScene().name + ")");

            OnLevelWasLoadedEvent?.Invoke(level);
        }

        private void FixObjectShaders(GameObject obj)
        {
            foreach (SkinnedMeshRenderer smr in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

            foreach (MeshRenderer smr in obj.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    string sName = m.shader.name;
                    sName = sName.Replace(" (to_replace)", "");
                    m.shader = Shader.Find(sName);
                }
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            ui.Recreate();
            MelonModLogger.Log("Initialized scene " + level.ToString());
        }



        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !isServer)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Connect(ModPrefs.GetString("MPMod", "HostSteamID"));
                    SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--boneworks-multiplayer-id-connect " + client.ServerId);
                    SteamFriends.SetRichPresence("steam_player_group", client.ServerId.ToString());
                }

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
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Disconnect();
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    MelonModLogger.Log("Stopping server...");
                    server.StopServer();
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (client.isConnected)
                client.Update();

            if (isServer)
                server.Update();
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
                client.Disconnect();

            if (isServer)
                server.StopServer();
        }
    }
}
