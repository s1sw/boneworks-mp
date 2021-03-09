using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Source.MonoBehaviours;
using StressLevelZero.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static MultiplayerMod.Source.Structs.Teams;

namespace MultiplayerMod.Source.Boneworks
{
    [Harmony.HarmonyPatch(typeof(StressLevelZero.Combat.Projectile), "Awake")]
    public static class ProjectilePatch
    {
        public static string mpTag = "Tornado";
        public static Team myTeam = Team.Anarchy;
        static bool Prefix(Projectile __instance)
        {
            UnityAction<Collider, Vector3, Vector3> unityAction = new Action<Collider, Vector3, Vector3>((Collider col, Vector3 point, Vector3 c) => {
                //This is what gets called when the bullet hits something
                MelonLogger.Log("Fired Projectile UnityAction");
                int type = 0;
                if (MultiplayerMod.client.isConnected)
                    type = 1;
                if (MultiplayerMod.server.IsRunning)
                    type = 2;

                MelonLogger.Log("Server (2) or client (1): " + type);
                if (type != 0)
                {
                    Transform root = col.transform.root;
                    if (root.tag == mpTag)
                    {
                        MelonLogger.Log("Tag comparison successful. Attempting PlayerInfo grab");
                        PlayerInfo playerInfo = root.GetComponent<PlayerInfo>();
                        if (playerInfo != null)
                        {
                            if (playerInfo.rep.team == Team.Passive)
                                return;
                            if (playerInfo.rep.team != Team.Anarchy && playerInfo.rep.team == myTeam)
                                return;

                            if (type == 1)
                                MultiplayerMod.client.SendProjectileHurt(__instance.bulletObject.ammoVariables.AttackDamage, playerInfo.rep.steamId);
                            else
                                MultiplayerMod.server.SendProjectileHurt(__instance.bulletObject.ammoVariables.AttackDamage, playerInfo.rep.steamId);
                        }
                    }
                }
            });
            if (__instance.onCollision == null)
                __instance.onCollision = new UnityEventCollision();
            __instance.onCollision.AddListener(unityAction);
            return true;
        }
    }
}
