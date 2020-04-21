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
        PreConnect,
        Server,
        Client
    }

    class MultiplayerUI
    {
        private AssetBundle uiBundle;
        private GameObject uiPrefab;

        private GameObject uiObj;
        private Text playerCountText;
        private Text preconnectText;
        private Text clientConnectedText;

        public MultiplayerUI()
        {
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
                // Would like to use the generic version here, but that breaks due to IL2CPP
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

            playerCountText = panelTransform.Find("PlayerCountText").GetComponent<Text>();
            preconnectText = panelTransform.Find("PreconnectText").GetComponent<Text>();

        }

        public void SetState(MultiplayerUIState f)
        {
            switch (f)
            {
                case MultiplayerUIState.PreConnect:
                    preconnectText.enabled = true;
                    playerCountText.enabled = false;
                    clientConnectedText.enabled = false;
                    break;
                case MultiplayerUIState.Client:
                    preconnectText.enabled = false;
                    clientConnectedText.enabled = true;
                    playerCountText.enabled = false;
                    break;
                case MultiplayerUIState.Server:
                    preconnectText.enabled = false;
                    playerCountText.enabled = true;
                    clientConnectedText.enabled = false;
                    break;
            }
        }

        public void SetPlayerCount(int nPlayers)
        {
            playerCountText.text = "Players: " + nPlayers;
        }
    }
}
