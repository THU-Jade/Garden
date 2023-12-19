using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Diagnostics;

public class TerrainImporter : EditorWindow
{
    private string heightmapsFolder = "Assets/Jade/Heightmaps"; // ��Ÿ߶�ͼ���ļ���
    private Material terrainMaterial; // ���β���
    private int terrainWidth = 1000; // ���εĿ��
    private int terrainLength = 1000; // ���εĳ���
    private int terrainHeight = 600; // ���ε����߶�

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

        // ��������
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 512; // ������Ҫ����
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);

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
        lightComp.intensity = 1.0f;

        // ���ù�Դ����
        lightGameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }
    //ָ������
    void ApplyTerrainMaterial(TerrainData terrainData)
    {
        if (terrainMaterial != null)
        {
            GameObject terrainObject = GameObject.Find("Terrain");
            if (terrainObject != null)
            {
                Terrain terrain = terrainObject.GetComponent<Terrain>();
                if (terrain != null)
                {
                    terrain.materialTemplate = terrainMaterial;
                }
            }
        }
        else
        {
          //  Debug.LogWarning("No terrain material specified.");
        }
    }
}
