using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using TrafikParkuru.Stations;

public static class SceneSetupHelper
{
    private static int checkCount = 0;

    [InitializeOnLoadMethod]
    public static void Init()
    {
        EditorApplication.update += RunUntilSceneLoaded;
    }

    private static void RunUntilSceneLoaded()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        checkCount++;
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        
        if (allObjects.Length > 10 || checkCount > 100)
        {
            EditorApplication.update -= RunUntilSceneLoaded;
            DumpSceneLayout(allObjects);
        }
    }

    public static void DumpSceneLayout(GameObject[] allObjects)
    {
        string outputPath = Path.Combine(Application.dataPath, "../scenelayout.txt");
        using (StreamWriter writer = new StreamWriter(outputPath, false))
        {
            writer.WriteLine("--- Scene GameObjects Dump ---");
            writer.WriteLine($"Total GameObjects found: {allObjects.Length}");
            foreach (GameObject go in allObjects)
            {
                if (go != null && go.transform.parent == null)
                {
                    DumpObject(go, writer, 0);
                }
            }
        }
        Debug.Log($"Dumped scene layout with fields to scenelayout.txt. Total objects: {allObjects.Length}");
    }

    private static void DumpObject(GameObject go, StreamWriter writer, int indent)
    {
        if (go == null) return;
        string indentStr = new string(' ', indent * 2);
        writer.WriteLine($"{indentStr}- {go.name} | Pos: {go.transform.position.ToString("F2")} | Rot: {go.transform.eulerAngles.ToString("F2")} | Scale: {go.transform.localScale.ToString("F2")} | Active: {go.activeInHierarchy}");

        // Print components and their serialized fields
        var components = go.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            string compName = comp.GetType().Name;
            if (compName != "Transform" && compName != "MeshFilter" && compName != "MeshRenderer" && compName != "BoxCollider" && compName != "MeshCollider")
            {
                writer.WriteLine($"{indentStr}    * Component: {compName}");
                
                // Reflection to dump serialized fields
                FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    {
                        object val = field.GetValue(comp);
                        writer.WriteLine($"{indentStr}        + {field.Name}: {val}");
                    }
                }
            }
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child != null)
            {
                DumpObject(child.gameObject, writer, indent + 1);
            }
        }
    }
}
