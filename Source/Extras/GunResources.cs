using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod.Extras
{
    public static class GunResources
    {
        private static AssetBundle bundle;

        public static GameObject LinePrefab 
        {
            get
            {
                if (!linePrefab) linePrefab = bundle.LoadAsset("Assets/bulletTrail.prefab").Cast<GameObject>();
                return linePrefab;
            }
        }

        public static GameObject HurtSFX
        {
            get
            {
                if (!hurtSfx) hurtSfx = bundle.LoadAsset("Assets/fordHurt.prefab").Cast<GameObject>();
                return hurtSfx;
            }
        }

        private static GameObject linePrefab;
        private static GameObject hurtSfx;

        public static void Load()
        {
            bundle = AssetBundle.LoadFromFile("gun.fx");
        }
    }
}
