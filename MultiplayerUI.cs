using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;

namespace MultiplayerMod
{
    enum MultiplayerUIState
    {
        PreStart,
        Started
    }

    class MultiplayerUI
    {
        private AssetBundle uiBundle;
        private GameObject uiPrefab;

        private GameObject uiObj;
        private Text playerCountText;
        private InputField steamIdField;
        private GameObject playerCountParent;
        private GameObject connectParent;

        public event Action<string> Connect;
        public event Action StartServer;

        public MultiplayerUI()
        {
            // Would like to use the generic version here, but that breaks due to IL2CPP
            uiBundle = AssetBundle.LoadFromFile("canvasbundle.canvas");

            if (uiBundle == null)
            {
                MelonModLogger.LogError("Failed to load canvas bundle");
                // Resort to creating a UI thing manually
                GameObject tmObj = new GameObject("TextMesh");
                TextMesh tm = tmObj.AddComponent<TextMesh>();
                tm.text = "You haven't installed the mod correctly!";
                tm.fontSize = 20;
                tm.color = new Color(1.0f, 0.0f, 0.0f);
            }
            else
            {
                MelonLoader.MelonModLogger.Log("Loaded canvas bundle");
                uiPrefab = uiBundle.LoadAsset("Assets/Prefabs/Canvas.prefab").Cast<GameObject>();
                if (uiPrefab == null)
                    MelonLoader.MelonModLogger.LogError("Couldn't find prefab");
                Recreate();
                
            }
        }

        public void Recreate()
        {
            uiObj = GameObject.Instantiate(uiPrefab);
            uiObj.GetComponent<Canvas>().worldCamera = Camera.current;
            UnityEngine.Object.DontDestroyOnLoad(uiObj);

            Transform panelTransform = uiObj.transform.Find("Panel");

            connectParent = panelTransform.Find("ConnectParent").gameObject;

            steamIdField = connectParent.transform.Find("TargetSteamID").GetComponent<InputField>();

            connectParent.transform.Find("ConnectButton").GetComponent<Button>().onClick.AddListener((Action)OnConnectClick);
            connectParent.transform.Find("StartServerButton").GetComponent<Button>().onClick.AddListener((Action)OnServerStartClick);

            playerCountParent = panelTransform.Find("PlayerCountText").gameObject;
            playerCountText = playerCountParent.GetComponent<Text>();
        }

        private void OnConnectClick()
        {
            Connect(steamIdField.text);
        }

        private void OnServerStartClick()
        {
            StartServer();
            SetState(MultiplayerUIState.Started);
        }

        public void SetState(MultiplayerUIState f)
        {
            if (f == MultiplayerUIState.Started)
            {
                connectParent.SetActive(false);
                playerCountParent.SetActive(true);
            }
            else
            {
                connectParent.SetActive(true);
                playerCountParent.SetActive(false);
            }
        }

        public void SetPlayerCount(int nPlayers)
        {
            playerCountText.text = "Players: " + nPlayers;
        }
    }
}
