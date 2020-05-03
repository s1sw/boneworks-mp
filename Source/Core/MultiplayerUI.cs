using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using static UnityEngine.Object;

namespace MultiplayerMod.Core
{
    enum MultiplayerUIState
    {
        PreConnect,
        Server,
        Client
    }

    class MultiplayerUI
    {
        private readonly AssetBundle uiBundle;
        private GameObject uiObj;
        private Text statusText;

        public MultiplayerUI()
        {
            uiBundle = AssetBundle.LoadFromFile("canvasbundle.canvas");

            if (uiBundle == null)
            {
                MelonModLogger.LogError("Failed to load canvas bundle");
                
                // Create a world space UI to display the error message
                GameObject tmObj = new GameObject("TextMesh");
                TextMesh tm = tmObj.AddComponent<TextMesh>();
                tm.text = "You haven't installed the mod correctly!";
                tm.fontSize = 20;
                tm.color = new Color(1.0f, 0.0f, 0.0f);
            }
            else
            {
                MelonModLogger.Log("Loaded canvas bundle");
                Recreate();

            }
        }

        public void Recreate()
        {
            GameObject uiPrefab = uiBundle.LoadAsset("Assets/Prefabs/Canvas.prefab").Cast<GameObject>();
            uiObj = Instantiate(uiPrefab);
            uiObj.GetComponent<Canvas>().worldCamera = Camera.current;
            DontDestroyOnLoad(uiObj);

            Transform panelTransform = uiObj.transform.Find("Panel");

            statusText = panelTransform.Find("StatusText").GetComponent<Text>();
        }

        // Updates the UI based on the client's status
        public void SetState(MultiplayerUIState uiState)
        {
            statusText.enabled = true;

            switch (uiState)
            {
                case MultiplayerUIState.PreConnect:
                    statusText.text = "Not connected. Press S to start a server.";
                    break;

                case MultiplayerUIState.Client:
                    statusText.text = "Connected";
                    break;

                case MultiplayerUIState.Server:
                    statusText.text = "Hosting";
                    break;
            }
        }

        // Updates the UI to reflect the Player Count
        public void SetPlayerCount(int nPlayers, MultiplayerUIState uiState)
        {
            if (uiState == MultiplayerUIState.Server)
                statusText.text = $"Currently hosting {nPlayers} players";
        }

        // Recreates the UI canvas
        public void Recreate()
        {
            uiObj = GameObject.Instantiate(uiBundle.LoadAsset("Assets/Prefabs/Canvas.prefab").Cast<GameObject>());
            uiObj.GetComponent<Canvas>().worldCamera = Camera.current;
            UnityEngine.Object.DontDestroyOnLoad(uiObj);

            Transform panelTransform = uiObj.transform.Find("Panel");

            clientStatusText = panelTransform.Find("PlayerCountText").GetComponent<Text>();
        }
    }
}
