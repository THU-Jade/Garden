using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    // private string heightmapsFolder = "Assets/Jade/Heightmaps_Garden"; // ��Ÿ߶�ͼ���ļ���
    // private string labelmapsFolder = "Assets/Jade/Labelmaps_Garden"; // ���labelͼ���ļ���
    // private string jsonFolder = "Assets/Jade/Json_Garden"; // ���json���ļ���
    private string inputFolder = "Assets/Jade/Inputs"; // �����������ļ�
    private string sceneFolder = "Assets/Jade/Scene_Garden"; // ��ų������ļ���
    private string matFolder = "Assets/Jade/Mat_Garden"; // ��Ų��ʵ��ļ���
    private Material terrainMaterial; // ���β���
    private int terrainWidth = 440; // ���εĿ��
    private int terrainLength = 440; // ���εĳ���
    private int terrainHeight = 80; // ���ε����߶�
    private float waterHeight = 6.4f; // ˮ��߶�

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
        // �����³���
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(newScene);

        // ���ƽ�й�
        CreateDirectionalLight();

        // ����ȫ�������ˮ��
        GlobalPrefab();

        // ��������
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = resolution; // ������Ҫ����
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.transform.position = new Vector3(widthOffset, 0, lengthOffset);

        // ������Ӧ���µĵ��β���
        ApplyNewTerrainMaterial(terrainObject, labelmapPath, sceneName);

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
        string savePath = sceneFolder + "/" + sceneName + ".unity";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        EditorSceneManager.SaveScene(newScene, savePath);
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
    void ApplyNewTerrainMaterial(GameObject terrainObject, string labelmapPath, string sceneName)
    {
        // �����²���
        Shader terrainShader = Shader.Find("Shader Graphs/TerrainBlend"); // ȷ��Shader·����ȷ
        if (terrainShader == null)
        {
            UnityEngine.Debug.LogError("TerrainBlend shader not found.");
            return;
        }

        Material newTerrainMaterial = new Material(terrainShader);

        // Ӧ�ñ�ǩͼ������
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

        // �����²���
        string savePath = matFolder + "/" + sceneName + "_Material.mat";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        AssetDatabase.CreateAsset(newTerrainMaterial, savePath);

        // Ӧ���²���
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
        // ����Prefab
        GameObject globalVolumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Global Volume.prefab");
        GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jade/Environment/Water System.prefab");
        if (globalVolumePrefab != null && waterPrefab != null)
        {
            // ʵ����Prefab����ǰ����ĳ�����
            PrefabUtility.InstantiatePrefab(globalVolumePrefab);
            GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);

            // �Զ���ˮ��Y�����ֵ
            float customYValue = waterHeight; // ˮ��߶�
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
