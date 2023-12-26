using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    private string heightmapsFolder = "Assets/Jade/new_hm"; // ��Ÿ߶�ͼ���ļ���
    private Material terrainMaterial; // ���β���
    private int terrainWidth = 440; // ���εĿ��
    private int terrainLength = 440; // ���εĳ���
    private int terrainHeight = 100; // ���ε����߶�

    private int resolution = 1024;
    private int widthOffset = -20;
    private int lengthOffset = -20;

    void OnEnable()
    {
        // ��OnEnable�м���Ĭ�ϲ���//
        LoadDefaultMaterial();
    }

    private void LoadDefaultMaterial()
    {
        if (terrainMaterial == null)
        {
            terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jade/Shader/M_TerrainBlend.mat");
        }
    }

    [MenuItem("Tools/Terrain Importer")]
    public static void ShowWindow()
    {
        GetWindow<TerrainImporter>("Terrain Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Terrain Import Settings", EditorStyles.boldLabel);
        heightmapsFolder = EditorGUILayout.TextField("Heightmaps Folder", heightmapsFolder);
        terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material", terrainMaterial, typeof(Material), false);
        terrainWidth = EditorGUILayout.IntField("Terrain Width", terrainWidth);
        terrainLength = EditorGUILayout.IntField("Terrain Length", terrainLength);
        terrainHeight = EditorGUILayout.IntField("Terrain Height", terrainHeight);
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        widthOffset = EditorGUILayout.IntField("Width Offset", widthOffset);
        lengthOffset = EditorGUILayout.IntField("Length Offset", lengthOffset);

        if (GUILayout.Button("Import Heightmaps"))
        {
            ImportHeightmaps();
        }
    }

    void ImportHeightmaps()
    {
        string[] files = Directory.GetFiles(heightmapsFolder, "*.png"); // ���Ը������ĸ߶�ͼ��ʽ���е���

        foreach (string file in files)
        {
            CreateTerrainFromHeightmap(file);
        }
    }

    void CreateTerrainFromHeightmap(string heightmapPath)
    {
        // �����³���
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(newScene);

        // ���ƽ�й�
        CreateDirectionalLight();

        // ����ȫ�������ˮ��
        GlobalPrefab();

        // ��������
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 1024; // ������Ҫ����
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.transform.position = new Vector3(widthOffset, 0, lengthOffset);

        // Ӧ�õ��β���
        ApplyTerrainMaterial(terrainObject);

        // ����߶�ͼ
        byte[] heightmapBytes = File.ReadAllBytes(heightmapPath);
        Texture2D heightmapTexture = new Texture2D(2, 2);
        heightmapTexture.LoadImage(heightmapBytes);
        float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                heights[y, x] = heightmapTexture.GetPixel(x, y).grayscale;
            }
        }
        terrainData.SetHeights(0, 0, heights);

        // ���泡��
        string sceneName = Path.GetFileNameWithoutExtension(heightmapPath);
        EditorSceneManager.SaveScene(newScene, "Assets/Jade/Scenes/" + sceneName + ".unity");
    }

    void CreateDirectionalLight()
    {
        // �����µĹ�Դ����
        GameObject lightGameObject = new GameObject("Directional Light");
        Light lightComp = lightGameObject.AddComponent<Light>();

        // ���ù�Դ����
        lightComp.type = LightType.Directional;
        lightComp.color = Color.white;
        lightComp.intensity = 1.5f;

        // ������Ӱ
        lightComp.shadows = LightShadows.Soft;

        // ���ù�Դ����
        lightGameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }
    //ָ������
    void ApplyTerrainMaterial(GameObject terrainObject)
    {
        if (terrainMaterial != null && terrainObject != null)
        {
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.materialTemplate = terrainMaterial;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("No terrain material specified or terrain object is null.");
        }
    }

    void GlobalPrefab()
    {
        // ����Prefab
        GameObject globalVolumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Global Volume.prefab");
        GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Water System.prefab");
        if (globalVolumePrefab != null && waterPrefab != null)
        {
            // ʵ����Prefab����ǰ����ĳ�����
            PrefabUtility.InstantiatePrefab(globalVolumePrefab);
            GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);

            // �Զ���ˮ��Y�����ֵ
            float customYValue = 8f; // ˮ��߶�
            Vector3 position = waterInstance.transform.position;
            position.y = customYValue;
            waterInstance.transform.position = position;
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to load Global Volume prefab.");
        }
    }
}
