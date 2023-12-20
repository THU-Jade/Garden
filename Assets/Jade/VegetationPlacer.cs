using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

public class VegetationPlacer : EditorWindow
{
    private Terrain terrain;
    private List<GameObject> vegetationPrefabs = new List<GameObject>();
    private List<int> vegetationCounts = new List<int>();
    private float minScale = 0.8f; // 最小缩放
    private float maxScale = 3.5f; // 最大缩放
    private GameObject vegetationParent; // 存储所有植被的父对象

    [MenuItem("Tools/Vegetation Placer")]
    public static void ShowWindow()
    {
        GetWindow<VegetationPlacer>("Vegetation Placer");
    }

    void OnEnable()
    {
        FindTerrainInScene();
        // 在启用时加载默认的FBX模型
        LoadDefaultVegetation();
    }

    void LoadDefaultVegetation()
    {
        // 加载FBX模型
        GameObject defaultVegetationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetStoreOriginals/APAC_Garden/Art/Models/Tree/beech_tree_05.FBX");
        if (defaultVegetationPrefab != null)
        {
            // 如果列表为空，添加默认模型和计数
            if (vegetationPrefabs.Count == 0)
            {
                vegetationPrefabs.Add(defaultVegetationPrefab);
                vegetationCounts.Add(200);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to load default vegetation prefab.");
        }
    }

    void FindTerrainInScene()
    {
        // 查找场景中的第一个Terrain对象
        Terrain foundTerrain = GameObject.FindObjectOfType<Terrain>();
        if (foundTerrain != null)
        {
            terrain = foundTerrain;
        }
        else
        {
            UnityEngine.Debug.LogWarning("No Terrain found in the current scene.");
        }
    }

        void OnGUI()
    {
        GUILayout.Label("Vegetation Placement Settings", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

        if (GUILayout.Button("Add Vegetation Type"))
        {
            vegetationPrefabs.Add(null);
            vegetationCounts.Add(100);
        }

        for (int i = 0; i < vegetationPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            vegetationPrefabs[i] = (GameObject)EditorGUILayout.ObjectField("Prefab " + (i + 1), vegetationPrefabs[i], typeof(GameObject), false);
            vegetationCounts[i] = EditorGUILayout.IntField("Count", vegetationCounts[i]);
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Place Vegetation"))
        {
            ClearVegetation();
            PlaceVegetation();
        }
    }

    void ClearVegetation()
    {
         Transform terrainTransform = terrain.transform;

        // 创建一个临时列表存储所有子对象
        List<GameObject> children = new List<GameObject>();

        // 遍历 terrain 的所有子对象并添加到列表中
        foreach (Transform child in terrainTransform)
        {
            children.Add(child.gameObject);
        }

        // 遍历列表并销毁所有子对象
        foreach (GameObject child in children)
        {
            DestroyImmediate(child);
        }
    }

    void PlaceVegetation()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned!");
            return;
        }

        foreach (var vegetation in vegetationPrefabs)
        {
            if (vegetation == null)
            {
                Debug.LogError("One or more vegetation prefabs are not assigned!");
                return;
            }
        }

        for (int i = 0; i < vegetationPrefabs.Count; i++)
        {
            for (int j = 0; j < vegetationCounts[i]; j++)
            {
                float x = Random.Range(0, terrain.terrainData.size.x);
                float z = Random.Range(0, terrain.terrainData.size.z);
                float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.GetPosition().y;

                GameObject vegetationInstance = PrefabUtility.InstantiatePrefab(vegetationPrefabs[i]) as GameObject;
                vegetationInstance.transform.position = new Vector3(x, y, z);

                // 随机缩放
                float scale = Random.Range(minScale, maxScale);
                vegetationInstance.transform.localScale = new Vector3(scale, scale, scale);

                // 随机旋转
                float rotationY = Random.Range(0f, 360f);
                vegetationInstance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

                vegetationInstance.transform.SetParent(terrain.transform);
            }
        }
    }
}
