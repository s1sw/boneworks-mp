using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace MultiplayerMod.Features
{
    public static class UI
    {
        public static GameObject mainPanelInstance;

        public static void CreateMainPanel()
        {
            try
            {
                if (mainPanelInstance == null)
                {
                    GameObject mainPanel = Core.MultiplayerUI.uiBundle.LoadAsset("Assets/Prefabs/MainPanel.prefab").TryCast<GameObject>();

                    if (!mainPanel)
                        throw new NullReferenceException("Missing MainPanel Prefab");

                    //mainPanelInstance = GameObject.Instantiate(mainPanel);
                    mainPanelInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mainPanelInstance.transform.localScale = Vector3.one / 10f;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Log(e.Message);
            }
        }
    }
}
