using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod.Core
{
    public static class ObjectIDManager
    {
        public static Dictionary<ushort, GameObject> objects = new Dictionary<ushort, GameObject>();

        public static void Reset()
        {
            objects.Clear();
        }

        public static GameObject GetObject(ushort id)
        {
            return objects[id];
        }

        public static void AddObject(ushort id, GameObject obj)
        {
            objects.Add(id, obj);
        }
    }
}
