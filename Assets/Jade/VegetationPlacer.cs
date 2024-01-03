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
    private bool enableShadows = true; // ����������ֲ���Ƿ���ʾͶӰ
    public Texture2D placementTexture; // ���������ڷ���ֲ��������ͼ

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
        GameObject defaultVegetationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetStoreOriginals/APAC_Garden/Art/Models/Plant/SM_Grass_01.FBX");
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

        // ���������һ���ֶ����û�ѡ�����ֲ��������ͼ
        placementTexture = (Texture2D)EditorGUILayout.ObjectField("Placement Texture", placementTexture, typeof(Texture2D), false);

        // ���������һ����ѡ�����û�ѡ���Ƿ�����ͶӰ
        enableShadows = EditorGUILayout.Toggle("Enable Shadows", enableShadows);

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
        //����������ͼ��д
        if (placementTexture != null)
        {
            string texturePath = AssetDatabase.GetAssetPath(placementTexture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (textureImporter != null && !textureImporter.isReadable)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
            }
        }

        for (int i = 0; i < vegetationPrefabs.Count; i++)
        {
            for (int j = 0; j < vegetationCounts[i]; j++)
            {
                float x = Random.Range(0, terrain.terrainData.size.x);
                float z = Random.Range(0, terrain.terrainData.size.z);
                float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.GetPosition().y;

                // ��������ͼ�����Ƿ����ֲ��
                if (ShouldPlaceVegetation(x, z))
                {
                    GameObject vegetationInstance = PrefabUtility.InstantiatePrefab(vegetationPrefabs[i]) as GameObject;
                    vegetationInstance.transform.position = new Vector3(x, y, z);

                    // �������
                    float scale = Random.Range(minScale, maxScale);
                    vegetationInstance.transform.localScale = new Vector3(scale, scale, scale);

                    // �����ת
                    float rotationY = Random.Range(0f, 360f);
                    vegetationInstance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

                    vegetationInstance.transform.SetParent(terrain.transform);

                    // ����ֲ����ͶӰ
                    if (vegetationInstance.GetComponent<Renderer>() != null)
                    {
                        vegetationInstance.GetComponent<Renderer>().shadowCastingMode = enableShadows
                            ? UnityEngine.Rendering.ShadowCastingMode.On
                            : UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }           
            }
        }
    }
    bool ShouldPlaceVegetation(float x, float z)
    {
        if (placementTexture == null)
            return true;

        // ����������ת��Ϊ��������
        float u = x / terrain.terrainData.size.x;
        float v = z / terrain.terrainData.size.z;
        int texX = Mathf.FloorToInt(u * placementTexture.width);
        int texZ = Mathf.FloorToInt(v * placementTexture.height);

        // ��ȡ�����϶�Ӧλ�õ���ɫ
        Color color = placementTexture.GetPixel(texX, texZ);

        // ���RGB����ͨ����Ϊ0�������ֲ��
        return color.r == 0 && color.g == 0 && color.b == 0;
    }
}
