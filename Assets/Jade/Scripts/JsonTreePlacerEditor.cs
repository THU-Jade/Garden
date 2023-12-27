using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using UnityEngine.SceneManagement; // 导入SceneManager命名空间
using UnityEditor.SceneManagement; // 导入EditorSceneManager

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
            //PlaceTreesFromJson(jsonFile.text);
        }
    }

    public static void PlaceTreesFromJson(string json, Scene scene)
    {
        TreesData treesData = JsonUtility.FromJson<TreesData>(json);

        foreach (var tree in treesData.tree)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(tree.path);
            if (model == null)
            {
                UnityEngine.Debug.LogWarning("Model not found at path: " + tree.path);
                continue;
            }

            foreach (var transformData in tree.transforms)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance != null)
                {
                    instance.transform.position = new Vector3(transformData.position.x, transformData.position.y, transformData.position.z);
                    instance.transform.rotation = new Quaternion(transformData.rotation.x, transformData.rotation.y, transformData.rotation.z, transformData.rotation.w);
                    instance.transform.localScale = new Vector3(transformData.scale.x, transformData.scale.y, transformData.scale.z);

                    // 使实例成为场景的一部分
                    SceneManager.MoveGameObjectToScene(instance, scene);
                }
            }
        }

        // 标记场景为“脏”，以保存更改
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
