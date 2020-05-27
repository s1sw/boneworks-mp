using zCubed.Accessories;
using System.IO;
using System.Net;
using MelonLoader;

using Main = zCubed.Accessories.Main;

namespace MultiplayerMod.Features
{
    public static class NetworkedAccesories
    {
        public static string[] GetLocalList()
        {
            string filePath = Main.AccessoryDataPath + "/remote_accessories.list";

            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "");

            return File.ReadAllText(filePath).Split('\n');
        }

        public static UnityEngine.AssetBundle GetRemoteBundle(string link)
        {
            WebClient webClient = new WebClient();

            string tempGuid = System.Guid.NewGuid().ToString();
            string fileLocation = $"{Main.AccessoryDataPath}/TEMP_{tempGuid}.accessory";

            MelonModLogger.Log("Starting download of TempFile!");

            webClient.DownloadFileTaskAsync(new System.Uri(link), fileLocation).Wait(); //MAKE THIS ASYNC SOME TIME LATER!!!

            UnityEngine.AssetBundle bundle = UnityEngine.AssetBundle.LoadFromFile(fileLocation);

            if (File.Exists(fileLocation))
                File.Delete(fileLocation);

            MelonModLogger.Log("Downloaded TempFile!");

            return bundle;
        }
    }
}
