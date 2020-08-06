////using zCubed.Accessories;
//using System.IO;
//using MelonLoader;
//using UnityEngine;

////using Main = zCubed.Accessories.Main;

//namespace MultiplayerMod.Features
//{
//    public static class NetworkedAccesories
//    {
//        public static string[] GetLocalList()
//        {
//            string[] rawPaths = Directory.GetFiles(Main.AccessoryDataPath);
//            string[] correctedPaths = new string[rawPaths.Length];

//            for (int p = 0; p < rawPaths.Length; p++)
//                correctedPaths[p] = rawPaths[p].Replace("\\", "/");

//            return correctedPaths;
//        }

//        public static byte[] BundleToNetBundle(string path)
//        {
//            if (!File.Exists(path))
//                return null;

//            return File.ReadAllBytes(path);
//        }
//    }
//}
