using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;

[Serializable]
public class TreeData
{
    public string name;
    public string path;
    public List<TransformData> transforms;
}

[Serializable]
public class TransformData
{
    public VectorData position;
    public QuaternionData rotation;
    public VectorData scale;
}

[Serializable]
public class VectorData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class QuaternionData
{
    public float x;
    public float y;
    public float z;
    public float w;
}

[Serializable]
public class TreesData
{
    public List<TreeData> tree;
}

public class JsonTreePlacerEditor : EditorWindow
{
    private TextAsset jsonFile;

    [MenuItem("Tools/Json Tree Placer")]
    public static void ShowWindow()
    {
        GetWindow<JsonTreePlacerEditor>("Json Tree Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Place Trees from JSON", EditorStyles.boldLabel);

        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);

        if (GUILayout.Button("Place Trees") && jsonFile != null)
        {
            PlaceTreesFromJson(jsonFile.text);
        }
    }

    private void PlaceTreesFromJson(string json)
    {
        TreesData treesData = JsonUtility.FromJson<TreesData>(json);

        foreach (var tree in treesData.tree)
        {
#if UNITY_EDITOR
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(tree.path);
#else
            GameObject model = null;
#endif

            if (model == null)
            {
                UnityEngine.Debug.LogWarning("Model not found at path: " + tree.path);
                continue;
            }

            foreach (var transformData in tree.transforms)
            {
                GameObject instance = Instantiate(model, Vector3.zero, Quaternion.identity);
                instance.transform.position = new Vector3(transformData.position.x, transformData.position.y, transformData.position.z);
                instance.transform.rotation = new Quaternion(transformData.rotation.x, transformData.rotation.y, transformData.rotation.z, transformData.rotation.w);
                instance.transform.localScale = new Vector3(transformData.scale.x, transformData.scale.y, transformData.scale.z);
            }
        }
    }
}
