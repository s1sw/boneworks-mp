using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using StressLevelZero;

namespace MultiplayerMod.Boneworks
{
    public class ZombieGameControlHooks
    {
        public static void PatchMethods()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("MPMod");
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("StartNextWave"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchStartNextWave"));
        }

        static void PatchStartNextWave() 
        {
            MelonLoader.MelonModLogger.Log("Next wave started");
        }
    }
}
