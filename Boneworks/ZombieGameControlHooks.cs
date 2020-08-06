using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using StressLevelZero;
using StressLevelZero.Pool;
using MelonLoader;

namespace MultiplayerMod.Boneworks
{
    public class ZombieGameControlHooks
    {
        public static event Action<int> OnWaveStart;
        public static event Action OnModeStart;
        public static event Action<Zombie_GameControl.Difficulty> OnDifficultyChanged;
        public static event Action<int> OnAmmoRewarded;
        public static event Action<int, EnemyType> OnPuppetDeath;
        public static event Action<float, bool> OnPlayerTakeDamage;
        public static int currentGameMode;

        public static void PatchMethods()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("MPMod");
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("StartNextWave"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchStartNextWave"));
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("StartSelectedMode"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchStartSelectedMode"));
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("SetGameMode"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchSetGameMode"));
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("RewardAmmo"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchRewardAmmo"));
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("ToggleDifficulty"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchToggleDifficulty"));
            harmonyInstance.Patch(typeof(Zombie_GameControl).GetMethod("OnPuppetDeath"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchOnPuppetDeath"));
            harmonyInstance.Patch(typeof(Player_Health).GetMethod("TAKEDAMAGE"), null, new HarmonyMethod(typeof(ZombieGameControlHooks), "PatchTAKEDAMAGE"));
        }

        static void PatchStartNextWave() 
        {
            MelonModLogger.Log("Next wave started");
            OnWaveStart?.Invoke(Zombie_GameControl.instance.currWaveIndex);
        }

        static void PatchStartSelectedMode()
        {
            MelonModLogger.Log("Starting game");
            OnModeStart?.Invoke();
            foreach (var eType in Zombie_GameControl.instance.customEnemyTypeList)
            {
                MelonModLogger.Log($"eType: {eType}");
            }
        }

        static void PatchSetGameMode(int mode)
        {
            MelonModLogger.Log($"Set game mode {mode}");
            currentGameMode = mode;
        }

        static void PatchRewardAmmo(int awardType)
        {
            MelonModLogger.Log($"REWARD: {awardType}");
            OnAmmoRewarded?.Invoke(awardType);
        }

        static void PatchToggleDifficulty()
        {
            OnDifficultyChanged?.Invoke(Zombie_GameControl.instance.difficulty);
            MelonModLogger.Log($"Toggle difficulty called, current difficulty is {Zombie_GameControl.instance.difficulty}");
        }

        private static void Log(string msg)
        {
            MelonModLogger.Log(msg);
        }

        private static Dictionary<string, EnemyType> enemyUUIDS = new Dictionary<string, EnemyType>()
        {
            { "c0f56de8-093e-4505-9d5d-0c3600af6001", EnemyType.Crablet },
            { "4c68514d-55f2-417d-964e-cfb32fae9f80", EnemyType.FordEarlyExit }
        };

        static void PatchOnPuppetDeath(PuppetMasta.PuppetMaster puppet)
        {
            MelonModLogger.Log("OnPuppetDeath: " + puppet.transform.parent.gameObject.name);

            var poolee = puppet.transform.parent.GetComponent<Poolee>();
            int id = int.Parse(puppet.transform.parent.gameObject.name.Split('[')[1].Split(']')[0]);
            Pool pool = poolee.pool;
            OnPuppetDeath?.Invoke(id, enemyUUIDS[poolee.spawnObject.UUID]);
        }

        static void PatchTAKEDAMAGE(float damage, bool crit)
        {
            if (!Zombie_GameControl.instance) return;
            OnPlayerTakeDamage?.Invoke(damage, crit);
        }
    }
}
