using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

                Transform playerRoot = zCubed.Accessories.Accessory.GetPlayerRoot();

                if (!playerRoot)
                    throw new NullReferenceException("Player Root Null");

                mainPanelInstance.transform.position = playerRoot.position + (playerRoot.forward * 2);
                mainPanelInstance.transform.LookAt(zCubed.Accessories.Accessory.GetTransformPoint(playerRoot, zCubed.Accessories.Globals.AttachPoint.Head));
                mainPanelInstance.transform.eulerAngles = -mainPanelInstance.transform.eulerAngles;
            }
            catch (Exception e)
            {
                MelonLoader.MelonModLogger.Log(e.Message);
            }
        }
    }
}
