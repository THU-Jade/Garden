using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using UnityEngine.SceneManagement; // ����SceneManager�����ռ�
using UnityEditor.SceneManagement; // ����EditorSceneManager

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
public class JsonData
{
    public List<TreeData> tree;
    public string height_map_path;
    public string label_map_path;
    public int max_height;
    public float water_height;
    public int map_width;
    public int map_height;
    public int real_width;
    public int real_height;
    public int width_offset;
    public int height_offset;
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

    public static void PlaceTreesFromJson(JsonData jsonData, Scene scene)
    {
        GameObject vegetationParent = new GameObject("Vegetation");
        foreach (var tree in jsonData.tree)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(tree.path);
            if (model == null)
            {
                UnityEngine.Debug.LogWarning("Model not found at path: " + tree.path);
                continue;
            }
            GameObject instanceParent = new GameObject(tree.name);

            foreach (var transformData in tree.transforms)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance != null)
                {
                    instance.transform.position = new Vector3(transformData.position.x, transformData.position.y, transformData.position.z);
                    instance.transform.rotation = new Quaternion(transformData.rotation.x, transformData.rotation.y, transformData.rotation.z, transformData.rotation.w);
                    instance.transform.localScale = new Vector3(transformData.scale.x, transformData.scale.y, transformData.scale.z);
                    instance.transform.parent = instanceParent.transform;
                }
            }
            instanceParent.transform.parent = vegetationParent.transform;
        }
        SceneManager.MoveGameObjectToScene(vegetationParent, scene);

        // ��ǳ����?���ࡱ���Ա������?
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void PlaceTreesFromJson(string json, Scene scene)
    {
        JsonData jsonData = JsonUtility.FromJson<JsonData>(json);

        PlaceTreesFromJson(jsonData, scene);
    }
}