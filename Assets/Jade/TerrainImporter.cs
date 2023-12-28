using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    // private string heightmapsFolder = "Assets/Jade/Heightmaps_Garden"; // 存放高度图的文件夹
    // private string labelmapsFolder = "Assets/Jade/Labelmaps_Garden"; // 存放label图的文件夹
    // private string jsonFolder = "Assets/Jade/Json_Garden"; // 存放json的文件夹
    private string inputFolder = "Assets/Jade/Inputs"; // 存所有输入文件
    private string sceneFolder = "Assets/Jade/Scene_Garden"; // 存放场景的文件夹
    private string matFolder = "Assets/Jade/Mat_Garden"; // 存放材质的文件夹
    private Material terrainMaterial; // 地形材质
    private int terrainWidth = 440; // 地形的宽度
    private int terrainLength = 440; // 地形的长度
    private int terrainHeight = 80; // 地形的最大高度
    private float waterHeight = 6.4f; // 水体高度

    private int resolution = 1024;
    private int widthOffset = -20;
    private int lengthOffset = -20;



    [MenuItem("Tools/Terrain Importer")]
    public static void ShowWindow()
    {
        GetWindow<TerrainImporter>("Terrain Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Terrain Import Settings", EditorStyles.boldLabel);
        // heightmapsFolder = EditorGUILayout.TextField("Heightmaps Folder", heightmapsFolder);
        // labelmapsFolder = EditorGUILayout.TextField("Labelmaps Folder", labelmapsFolder);
        // jsonFolder = EditorGUILayout.TextField("Json Folder", jsonFolder);
        inputFolder = EditorGUILayout.TextField("Input Folder", inputFolder);
        sceneFolder = EditorGUILayout.TextField("Scene Folder", sceneFolder);
        matFolder = EditorGUILayout.TextField("Mat Folder", matFolder);

        if (GUILayout.Button("Import Heightmaps"))
        {
            if (!Directory.Exists(sceneFolder))
            {
                Directory.CreateDirectory(sceneFolder);
            }
            if (!Directory.Exists(matFolder))
            {
                Directory.CreateDirectory(matFolder);
            }
            if (!Directory.Exists(inputFolder))
            {
                Directory.CreateDirectory(inputFolder);
            }
            ImportHeightmaps();
        }
    }

    void ImportHeightmaps()
    {
        // string[] heightmapFiles = Directory.GetFiles(heightmapsFolder, "*.png");
        // string[] labelmapFiles = Directory.GetFiles(labelmapsFolder, "*.png");
        // string[] jsonFiles = Directory.GetFiles(jsonFolder, "*.json");
        string[] jsonFiles = Directory.GetFiles(inputFolder, "*.json");
        string firstSceneName = "";

        for (int i = 0; i < jsonFiles.Length; i++)
        {
            string sceneName = Path.GetFileNameWithoutExtension(jsonFiles[i]);
            // parse json
            string jsonText = File.ReadAllText(jsonFiles[i]);
            JsonData jsonData = JsonUtility.FromJson<JsonData>(jsonText);
            string heightmapPath = inputFolder + "/" + jsonData.height_map_path;
            string labelmapPath = inputFolder + "/" + jsonData.label_map_path;
            // UnityEngine.Debug.Log(heightmapPath + " " + labelmapPath);

            // generate
            terrainWidth = jsonData.real_width;
            terrainLength = jsonData.real_height;
            terrainHeight = jsonData.max_height;
            waterHeight = jsonData.water_height;
            widthOffset = jsonData.width_offset;
            lengthOffset = jsonData.height_offset;
            resolution = Mathf.Max(jsonData.map_width, jsonData.map_height);
            CreateTerrainFromHeightmap(heightmapPath, labelmapPath, sceneName);

            Scene activeScene = EditorSceneManager.GetActiveScene();
            JsonTreePlacerEditor.PlaceTreesFromJson(jsonData, activeScene);
            if (i == 0)
            {
                firstSceneName = sceneName;
            }
        }
        // close current scene and switch to first scene
        // EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
        EditorSceneManager.OpenScene(sceneFolder + "/" + firstSceneName + ".unity");
    }

    void CreateTerrainFromHeightmap(string heightmapPath, string labelmapPath, string sceneName)
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
        terrainData.heightmapResolution = resolution; // 根据需要调整
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.transform.position = new Vector3(widthOffset, 0, lengthOffset);

        // 创建并应用新的地形材质
        ApplyNewTerrainMaterial(terrainObject, labelmapPath, sceneName);

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
        string savePath = sceneFolder + "/" + sceneName + ".unity";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        EditorSceneManager.SaveScene(newScene, savePath);
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
    void ApplyNewTerrainMaterial(GameObject terrainObject, string labelmapPath, string sceneName)
    {
        // 创建新材质
        Shader terrainShader = Shader.Find("Shader Graphs/TerrainBlend"); // 确保Shader路径正确
        if (terrainShader == null)
        {
            UnityEngine.Debug.LogError("TerrainBlend shader not found.");
            return;
        }

        Material newTerrainMaterial = new Material(terrainShader);

        // 应用标签图到材质
        if (!string.IsNullOrEmpty(labelmapPath))
        {
            Texture2D labelmapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(labelmapPath);
            if (labelmapTexture != null)
            {
                newTerrainMaterial.SetTexture("_Tex_Blend", labelmapTexture);
                UnityEngine.Debug.Log("Texture applied: " + labelmapPath);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Labelmap not found at path: " + labelmapPath);
            }
        }

        // 保存新材质
        string savePath = matFolder + "/" + sceneName + "_Material.mat";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        AssetDatabase.CreateAsset(newTerrainMaterial, savePath);

        // 应用新材质
        if (terrainObject != null)
        {
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.materialTemplate = newTerrainMaterial;
            }
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
            float customYValue = waterHeight; // 水体高度
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
