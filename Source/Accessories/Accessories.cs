using System;
using System.Text;
using System.IO;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using Enum = System.Enum;

namespace MultiplayerMod.Accessories
{
    public static partial class Accessories
    {
        public static GameObject CreateAccessories(AssetBundle bundle, Transform root)
        {
            SerializedMasterList masterList;

            TextAsset masterAsset = bundle.LoadAsset("Assets/MasterList.txt").TryCast<TextAsset>();

            XmlDocument document = new XmlDocument();

            XmlSerializer serializer = new XmlSerializer(typeof(SerializedParameters));

            using (TextReader reader = new StringReader(masterAsset.text))
            {
                masterList = (SerializedMasterList)serializer.Deserialize(document);
                
                MelonLoader.MelonModLogger.Log(masterList.serializedParams[0].fileLocation);
            }

            return new GameObject();
        }

        // Helper function for creating dummy objects at each point
        public static void CreateDummies(Transform root)
        {
            for (int t = 0; t < Constants.mountPoints.Length; t++)
            {
                GameObject rep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rep.transform.localScale = Vector3.one / 10f;

                rep.transform.parent = root.Find(Constants.mountPoints[t]);

                rep.transform.localPosition = Vector3.zero;
                rep.transform.localRotation = Quaternion.identity;

                rep.GetComponent<Collider>().enabled = false;
            }
        }
    }
}
