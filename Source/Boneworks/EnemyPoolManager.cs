using MelonLoader;
using StressLevelZero.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Object;

namespace MultiplayerMod.Boneworks
{
    public class EnemyPoolManager
    {
        private readonly Dictionary<EnemyType, Pool> enemyPools = new Dictionary<EnemyType, Pool>();

        public void FindAllPools()
        {
            enemyPools.Clear();

            foreach (Pool p in FindObjectsOfType<Pool>())
            {
                MelonLogger.Log("Prefab: " + p.Prefab.name);
                if (p.Prefab != null && p.Prefab.name.ToLowerInvariant().Contains("nullbody"))
                {
                    enemyPools.Add(EnemyType.NullBody, p); 
                }
            }
        }

        public void FindMissingPools()
        {
            if (!enemyPools.ContainsKey(EnemyType.NullBody))
            {
                foreach (Pool p in FindObjectsOfType<Pool>())
                {
                    MelonLogger.Log("Prefab: " + p.Prefab.name);
                    if (p.Prefab != null && p.Prefab.name.ToLowerInvariant().Contains("nullbody"))
                    {
                        enemyPools.Add(EnemyType.NullBody, p);
                    }
                }
            }
        }

        public Pool GetPool(EnemyType type)
        {
            return enemyPools[type];
        }
    }
}
