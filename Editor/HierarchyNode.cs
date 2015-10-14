using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace HierarchyNode
{
    public static class Util
    {
        [MenuItem("GameObject/HierarchyNode/Generate")]
        public static void GenerateHierarchyNode()
        {
            GenerateHierarchyNodeCore(false);
        }

        [MenuItem("GameObject/HierarchyNode/Generate with Component")]
        public static void GenerateHierarchyNodeWithComponent()
        {
            GenerateHierarchyNodeCore(true);
        }

        public static void GenerateHierarchyNodeCore(bool withComponent)
        {
            var destDir = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(Application.dataPath, "HierarchyNode/Scripts/Generated"));
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            Selection.objects
                .Where(o => o is GameObject)
                .Cast<GameObject>()
                .ToList()
                .ForEach(go =>
                {
                    var generator = new HierarchyNode.HierarchyNodeGenerator(go);
                    var scriptPath = Path.Combine(destDir, string.Format("{0}.cs", generator.FileName));
                    var componentScriptPath = Path.Combine(destDir, string.Format("{0}Component.cs", generator.FileName));

                    WriteToFile(scriptPath, generator.ScriptString);
                    if (withComponent)
                    {
                        WriteToFile(componentScriptPath, generator.ComponentScriptString);
                    }
                });

            AssetDatabase.Refresh();
        }

        private static void WriteToFile(string path, string str)
        {
            using (var sw = new StreamWriter(path))
            {
                sw.Write(str);
                Debug.LogFormat("Write to {0}", path);
            }
        }
    }
}
