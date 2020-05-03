using UnityEngine;
using MelonLoader;

namespace MultiplayerMod.Extras
{
    public static class Utilities
    {
        // Takes a component type and outputs all properties, only ones that don't throw exceptions
        public static void PrintComponentProps<T>(GameObject go)
        {
            try
            {
                if (go == null)
                    MelonModLogger.LogError("go was null???");

                T t = go.GetComponent<T>();

                if (t == null)
                    MelonModLogger.LogError("Couldn't find component " + t.GetType().Name);

                MelonModLogger.Log("====== Component type " + t.ToString() + "======");

                System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

                foreach (var pi in props)
                {
                    //if (pi.PropertyType.IsPrimitive)
                    try
                    {
                        var val = pi.GetValue(t);
                        if (val != null)
                            MelonModLogger.Log(pi.Name + ": " + val.ToString());
                        else
                            MelonModLogger.Log(pi.Name + ": null");
                    }
                    catch
                    {
                        MelonModLogger.LogError("Error tring to get property " + pi.Name);
                    }
                }
            }
            catch
            {
                MelonModLogger.LogError("i don't know anymore");
            }
        }

        // Takes a type and outputs all properties, only ones that don't throw exceptions
        public static void PrintProps<T>(T t)
        {
            MelonModLogger.Log("====== Type " + t.ToString() + "======");

            System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

            foreach (var pi in props)
            {
                //if (pi.PropertyType.IsPrimitive)
                try
                {
                    var val = pi.GetValue(t);
                    if (val != null)
                        MelonModLogger.Log(pi.Name + ": " + val.ToString());
                    else
                        MelonModLogger.Log(pi.Name + ": null");
                }
                catch
                {
                    MelonModLogger.LogError("Error tring to get property " + pi.Name);
                }
            }
        }

        // Outputs the hierarchy of a given object, with a configurable depth
        public static void PrintChildHierarchy(GameObject parent, int currentDepth = 0)
        {
            string offset = "";

            for (int j = 0; j < currentDepth; j++)
            {
                offset += "\t";
            }

            MelonModLogger.Log(offset + " Has components:");

            foreach (Component c in parent.GetComponents<Component>())
            {
                MelonModLogger.Log(offset + c.ToString());
            }

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;



                MelonModLogger.Log(offset + "-" + child.name);



                PrintChildHierarchy(child, currentDepth + 1);


            }
        }

        // Attempts to correct the shaders of objects that need it
        public static void FixObjectShaders(GameObject obj)
        {
            foreach (SkinnedMeshRenderer smr in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Valve/vr_standard");
                }
            }

            foreach (MeshRenderer smr in obj.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    string sName = m.shader.name;
                    sName = sName.Replace(" (to_replace)", "");
                    m.shader = Shader.Find(sName);
                }
            }
        }
    }
}
