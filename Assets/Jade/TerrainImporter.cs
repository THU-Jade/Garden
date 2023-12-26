using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    private string heightmapsFolder = "Assets/Jade/new_hm"; // 存放高度图的文件夹
    private Material terrainMaterial; // 地形材质
    private int terrainWidth = 440; // 地形的宽度
    private int terrainLength = 440; // 地形的长度
    private int terrainHeight = 100; // 地形的最大高度

    private int resolution = 1024;
    private int widthOffset = -20;
    private int lengthOffset = -20;

    void OnEnable()
    {
        // 在OnEnable中加载默认材质//
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
        string[] files = Directory.GetFiles(heightmapsFolder, "*.png"); // 可以根据您的高度图格式进行调整

        foreach (string file in files)
        {
            CreateTerrainFromHeightmap(file);
        }
    }

    void CreateTerrainFromHeightmap(string heightmapPath)
    {
        // 创建新场景
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(newScene);

        // 添加平行光
        CreateDirectionalLight();

        // 创建全局体积和水体
        GlobalPrefab();

        // 创建地形
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 1024; // 根据需要调整
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.transform.position = new Vector3(widthOffset, 0, lengthOffset);

        // 应用地形材质
        ApplyTerrainMaterial(terrainObject);

        // 导入高度图
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

        // 保存场景
        string sceneName = Path.GetFileNameWithoutExtension(heightmapPath);
        EditorSceneManager.SaveScene(newScene, "Assets/Jade/Scenes/" + sceneName + ".unity");
    }

    void CreateDirectionalLight()
    {
        // 创建新的光源对象
        GameObject lightGameObject = new GameObject("Directional Light");
        Light lightComp = lightGameObject.AddComponent<Light>();

        // 设置光源属性
        lightComp.type = LightType.Directional;
        lightComp.color = Color.white;
        lightComp.intensity = 1.5f;

        // 开启阴影
        lightComp.shadows = LightShadows.Soft;

        // 设置光源方向
        lightGameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }
    //指定材质
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
        // 加载Prefab
        GameObject globalVolumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Global Volume.prefab");
        GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Water System.prefab");
        if (globalVolumePrefab != null && waterPrefab != null)
        {
            // 实例化Prefab到当前激活的场景中
            PrefabUtility.InstantiatePrefab(globalVolumePrefab);
            GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);

            // 自定义水体Y轴的数值
            float customYValue = 8f; // 水体高度
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
