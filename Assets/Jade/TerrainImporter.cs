using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    private string heightmapsFolder = "Assets/Jade/Heightmaps_Garden"; // ��Ÿ߶�ͼ���ļ���
    private string labelmapsFolder = "Assets/Jade/Labelmaps_Garden"; // ���labelͼ���ļ���
    private string jsonFolder = "Assets/Jade/Json_Garden"; // ���json���ļ���
    private Material terrainMaterial; // ���β���
    private int terrainWidth = 440; // ���εĿ��
    private int terrainLength = 440; // ���εĳ���
    private int terrainHeight = 80; // ���ε����߶�
    private float waterHeight = 6.4f; // ���ε����߶�

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
        heightmapsFolder = EditorGUILayout.TextField("Heightmaps Folder", heightmapsFolder);
        labelmapsFolder = EditorGUILayout.TextField("Labelmaps Folder", labelmapsFolder);
        jsonFolder = EditorGUILayout.TextField("Json Folder", jsonFolder);
        terrainWidth = EditorGUILayout.IntField("Terrain Width", terrainWidth);
        terrainLength = EditorGUILayout.IntField("Terrain Length", terrainLength);
        terrainHeight = EditorGUILayout.IntField("Terrain Height", terrainHeight);
        waterHeight = EditorGUILayout.FloatField("Water Height", waterHeight);
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
        string[] heightmapFiles = Directory.GetFiles(heightmapsFolder, "*.png");
        string[] labelmapFiles = Directory.GetFiles(labelmapsFolder, "*.png");
        string[] jsonFiles = Directory.GetFiles(jsonFolder, "*.json");

        for (int i = 0; i < heightmapFiles.Length; i++)
        {
            string labelmapPath = i < labelmapFiles.Length ? labelmapFiles[i] : null;
            CreateTerrainFromHeightmap(heightmapFiles[i], labelmapPath);

            if (i < jsonFiles.Length)
            {
                string jsonText = File.ReadAllText(jsonFiles[i]);
                Scene activeScene = EditorSceneManager.GetActiveScene();
                JsonTreePlacerEditor.PlaceTreesFromJson(jsonText, activeScene);
            }
        }
    }

    void CreateTerrainFromHeightmap(string heightmapPath, string labelmapPath)
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

        // ������Ӧ���µĵ��β���
        ApplyNewTerrainMaterial(terrainObject, heightmapPath, labelmapPath);

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
        EditorSceneManager.SaveScene(newScene, "Assets/Jade/Scene_Garden/" + sceneName + ".unity");
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
    void ApplyNewTerrainMaterial(GameObject terrainObject, string heightmapPath, string labelmapPath)
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
        string materialPath = "Assets/Jade/Mat_Garden/" + Path.GetFileNameWithoutExtension(heightmapPath) + "_Material.mat";
        AssetDatabase.CreateAsset(newTerrainMaterial, materialPath);

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
