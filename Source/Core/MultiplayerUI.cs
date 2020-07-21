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
    public enum MultiplayerUIState
    {
        PreConnect,
        Server,
        Client
    }

    public class MultiplayerUI
    {
        private readonly AssetBundle uiBundle;
        private GameObject uiObj;
        private Text statusText;
        private MultiplayerUIState currentState = MultiplayerUIState.PreConnect;

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

        // Recreates the UI...
        public void Recreate()
        {
            GameObject uiPrefab = uiBundle.LoadAsset("Assets/Prefabs/Canvas.prefab").Cast<GameObject>();
            uiObj = Instantiate(uiPrefab);
            uiObj.GetComponent<Canvas>().worldCamera = Camera.current;
            DontDestroyOnLoad(uiObj);

            Transform panelTransform = uiObj.transform.Find("Panel");

            statusText = panelTransform.Find("PlayerCountText").GetComponent<Text>();
            SetState(currentState);
        }

        // Updates the UI based on the client's status
        public void SetState(MultiplayerUIState uiState)
        {
            currentState = uiState;
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
    }
}
