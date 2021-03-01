using MelonLoader;
using MultiplayerMod.Core;
using StressLevelZero.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace MultiplayerMod.Source.Boneworks
{
    [Harmony.HarmonyPatch(typeof(StressLevelZero.Combat.Projectile), "OnEnable")]
    public static class ProjectilePatch
    {
        public static string mpTag = "Tornado";
        static bool Prefix(Projectile __instance)
        {
            UnityAction<Collider, Vector3, Vector3> unityAction = new Action<Collider, Vector3, Vector3>((Collider col, Vector3 point, Vector3 c) => {
                //This is what gets called when the bullet hits something

                int type = 0;
                if (MultiplayerMod.client.isConnected)
                    type = 1;
                if (MultiplayerMod.server.IsRunning)
                    type = 2;

                if (type != 0)
                {
                    if (col.tag == mpTag)
                    {
                        string id = col.transform.root.name;
                        byte steamid = (byte)Int32.Parse(id);
                        if (type == 1)
                        {
                            MultiplayerMod.client.SendProjectileHurt(__instance.bulletObject.ammoVariables.AttackDamage, steamid);
                        } else
                        {
                            MultiplayerMod.server.SendProjectileHurt(__instance.bulletObject.ammoVariables.AttackDamage, steamid);
                        }
                    }
                }
            });
            __instance.onCollision = new UnityEventCollision();
            __instance.onCollision.AddListener(unityAction);
            return true;
        }
    }
}
