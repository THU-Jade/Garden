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
    private float minScale = 0.8f; // ��С����
    private float maxScale = 3.5f; // �������
    private GameObject vegetationParent; // �洢����ֲ���ĸ�����

    [MenuItem("Tools/Vegetation Placer")]
    public static void ShowWindow()
    {
        GetWindow<VegetationPlacer>("Vegetation Placer");
    }

    void OnEnable()
    {
        FindTerrainInScene();
        // ������ʱ����Ĭ�ϵ�FBXģ��
        LoadDefaultVegetation();
    }

    void LoadDefaultVegetation()
    {
        // ����FBXģ��
        GameObject defaultVegetationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetStoreOriginals/APAC_Garden/Art/Models/Tree/beech_tree_05.FBX");
        if (defaultVegetationPrefab != null)
        {
            // ����б�Ϊ�գ����Ĭ��ģ�ͺͼ���
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
        // ���ҳ����еĵ�һ��Terrain����
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

        // ����һ����ʱ�б�洢�����Ӷ���
        List<GameObject> children = new List<GameObject>();

        // ���� terrain �������Ӷ�����ӵ��б���
        foreach (Transform child in terrainTransform)
        {
            children.Add(child.gameObject);
        }

        // �����б����������Ӷ���
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

                // �������
                float scale = Random.Range(minScale, maxScale);
                vegetationInstance.transform.localScale = new Vector3(scale, scale, scale);

                // �����ת
                float rotationY = Random.Range(0f, 360f);
                vegetationInstance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

                vegetationInstance.transform.SetParent(terrain.transform);
            }
        }
    }
}
