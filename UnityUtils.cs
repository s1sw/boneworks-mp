using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace NotRuby
{
    public static class UnityUtils
    {
        public static void LogDepth(Transform root)
        {
            MelonLoader.MelonModLogger.Log("|-" + root);
            var list = root.gameObject.GetComponents<Component>();
            for (int i = 0; i < list.Length; i++)
            {
                Console.WriteLine(" +" + Regex.Replace(list[i].ToString(), @"\r\n?|\n", "\n" + "  #"));
            }
            LogDepth(root, 1);
        }

        private static void LogDepth(Transform root, int depth)
        {
            var children = root.gameObject.GetComponentsInChildren<UnityEngine.Transform>(true);
            foreach (var c in children)
            {
                if (c.parent == root)
                {
                    String line = "";
                    for (int x = 0; x <= depth + 1; x++)
                    {
                        if (x == depth + 1)
                            line += "-";
                        else if (x == depth)
                            line += "|";
                        else
                            line += "  ";
                    }
                    MelonLoader.MelonModLogger.Log(line + c + "(layer " + c.gameObject.layer.ToString() + ")");

                    var list = c.gameObject.GetComponents<Component>();
                    for (int i = 0; i < list.Length; i++)
                    {
                        MelonLoader.MelonModLogger.Log(line.Substring(0, line.Length - 2) + " +" + Regex.Replace(list[i].ToString(), @"\r\n?|\n", "\n" + line.Substring(0, line.Length - 2) + "  #"));
                    }

                    LogDepth(c, depth + 1);
                }
            }
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
    }
}
