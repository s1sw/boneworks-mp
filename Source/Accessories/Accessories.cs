using System;
using System.Text;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using MelonLoader;

namespace MultiplayerMod.Accessories
{
    public static partial class Accessories
    {
        public static void CreateAccessories()
        {
            Transform playerRoot = GameObject.Find(
                        "[RigManager (Default Brett)]").transform.Find(
                        "[SkeletonRig (GameWorld Brett)]/Brett@neutral/SHJntGrp/MAINSHJnt");

            string bundlepath = Application.dataPath.Replace("BONEWORKS_Data", "UserData/Accessories");

            if (!Directory.Exists(bundlepath))
                Directory.CreateDirectory(bundlepath);

            string[] bundles = Directory.GetFiles(bundlepath);

            foreach (string bundleFile in bundles)
            {
                if (bundleFile.Contains(".accessory"))
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(bundleFile);

                    if (bundle)
                    {
                        MelonModLogger.Log($"Loading accessories from: {bundleFile}");

                        for (int a = 0; a < 10; a++)
                            CreateAccessory(bundle, $"Accessory{a}", playerRoot);

                        bundle.Unload(false);
                    }
                }
            }
        }

        public static void CreateAccessory(AssetBundle bundle, string path, Transform root)
        {
            UnityEngine.Object originalAsset = bundle.LoadAsset($"Assets/{path}.prefab");

            if (!originalAsset)
                return;

            GameObject accessory = GameObject.Instantiate(originalAsset.TryCast<GameObject>());

            if (!accessory)
                return;

            AttachPoint point = AttachPoint.Belt;
            for (int p = 0; p < 22; p++)
            {
                string point_path = System.Enum.GetName(typeof(AttachPoint), p);

                if (accessory.transform.Find(point_path))
                {
                    point = (AttachPoint)p;
                    break;
                }
            }

            Extras.ShaderFixer.Fix(accessory);

            accessory.transform.parent = root.Find(Constants.mountPoints[(int)point]);
            accessory.transform.localPosition = Vector3.zero;
            accessory.transform.localRotation = Quaternion.identity;
        }

        // Helper function for creating dummy objects at each point
        public static void CreateDummies(Transform root)
        {
            for (int t = 0; t < Constants.mountPoints.Length; t++)
            {
                GameObject rep = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rep.transform.localScale = Vector3.one / 15f;

                rep.transform.parent = root.Find(Constants.mountPoints[t]);

                rep.transform.localPosition = Vector3.zero;
                rep.transform.localRotation = Quaternion.identity;

                rep.GetComponent<Collider>().enabled = false;
            }

            Il2CppReferenceArray<Transform> bones = root.GetComponentsInChildren<Transform>(false);

            foreach (Transform bone in bones)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.GetComponent<Collider>().enabled = false;
                
                cube.transform.localScale = Vector3.one / 50f;

                cube.transform.parent = bone;

                cube.transform.localPosition = Vector3.zero;
                cube.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
