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
        private static ushort latestId = 0;

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
            if (id > latestId)
                latestId = id;
            objects.Add(id, obj);
        }

        public static ushort AllocateID()
        {
            return latestId++;
        }
    }
}
