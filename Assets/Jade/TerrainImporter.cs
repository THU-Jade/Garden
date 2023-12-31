using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Diagnostics;
using UnityEngine.Rendering.HighDefinition; // 导入HDRP命名空间


public class TerrainImporter : EditorWindow
{
    // private string heightmapsFolder = "Assets/Jade/Heightmaps_Garden"; // 存放高度图的文件夹
    // private string labelmapsFolder = "Assets/Jade/Labelmaps_Garden"; // 存放label图的文件夹
    // private string jsonFolder = "Assets/Jade/Json_Garden"; // 存放json的文件夹
    private string inputFolder = "Assets/Jade/Inputs"; // 存所有输入文件
    private string sceneFolder = "Assets/Jade/Scene_Garden"; // 存放场景的文件夹
    private string matFolder = "Assets/Jade/Mat_Garden"; // 存放材质的文件夹
    private string imageFolder = "Assets/Jade/Image_Garden"; // 存放图片的文件夹
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
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
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
        // string[] sceneNames= new string[jsonFiles.Length];

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
            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            CaptureBirdsEyeView(terrainWidth, terrainWidth * jsonData.map_height / jsonData.map_width, widthOffset, lengthOffset);
            CaptureFrontView(terrainWidth, terrainWidth * jsonData.map_height / jsonData.map_width, widthOffset, lengthOffset);
            CaptureRandomView(jsonData.viewpoints);
        }
        // for(int i = 0; i < sceneNames.Length; i++)
        // {
        //     EditorSceneManager.OpenScene(EditorSceneManager.GetSceneByName(sceneNames[i]), OpenSceneMode.Single);
        // }
    }

    void CaptureRandomView(List<ViewpointsData> viewpoints)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        GameObject camera = new GameObject("Camera");
        camera.AddComponent<Camera>();
        int resx = 2048, resy = (int)(resx * 9 / 16);
        camera.GetComponent<Camera>().targetTexture = new RenderTexture(resx, resy, 24);
        for (int i = 0; i < viewpoints.Count; i++)
        {
            string savePath = imageFolder + "/" + sceneName + "_view" + i + ".png";
            camera.transform.position = new Vector3(viewpoints[i].x, viewpoints[i].y, viewpoints[i].z);
            camera.transform.rotation = Quaternion.Euler(viewpoints[i].xrot * 180 / Mathf.PI, viewpoints[i].yrot * 180 / Mathf.PI, 0);
            camera.GetComponent<Camera>().Render();
            RenderTexture.active = camera.GetComponent<Camera>().targetTexture;
            Texture2D image = new Texture2D(resx, resy, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, resx, resy), 0, 0);
            image.Apply();
            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(savePath, bytes);
        }
        DestroyImmediate(camera);
    }

    void CaptureFrontView(float xlength, float zlength, float xoffset, float zoffset)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        GameObject camera = new GameObject("Camera");
        camera.AddComponent<Camera>();
        int resx = 2048, resy = (int)(resx * 9 / 16);
        camera.GetComponent<Camera>().targetTexture = new RenderTexture(resx, resy, 24);
        for (int i = 0; i < 4; i++)
        {
            if (i == 0)
            {
                camera.transform.position = new Vector3(xlength / 2 + xoffset, 50, -50);
                camera.transform.rotation = Quaternion.Euler(15, 0, 0);
            }
            else if (i == 1)
            {
                camera.transform.position = new Vector3(xlength / 2 + xoffset, 50, zlength + zoffset + 50);
                camera.transform.rotation = Quaternion.Euler(15, 180, 0);
            }
            else if (i == 2)
            {
                camera.transform.position = new Vector3(-50, 50, zlength / 2 + zoffset);
                camera.transform.rotation = Quaternion.Euler(15, 90, 0);
            }
            else
            {
                camera.transform.position = new Vector3(xlength + xoffset + 50, 50, zlength / 2 + zoffset);
                camera.transform.rotation = Quaternion.Euler(15, -90, 0);
            }
            string savePath = imageFolder + "/" + sceneName + "_front" + i + ".png";
            camera.GetComponent<Camera>().Render();
            RenderTexture.active = camera.GetComponent<Camera>().targetTexture;
            Texture2D image = new Texture2D(resx, resy, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, resx, resy), 0, 0);
            image.Apply();
            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(savePath, bytes);
        }
        DestroyImmediate(camera);
    }

    void CaptureBirdsEyeView(float xlength, float zlength, float xoffset, float zoffset)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string savePath = imageFolder + "/" + sceneName + ".png";

        GameObject camera = new GameObject("Camera");
        camera.AddComponent<Camera>();
        camera.transform.position = new Vector3(xlength / 2 + xoffset, 200, zlength / 2 + zoffset);
        camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().orthographicSize = Mathf.Min(xlength, zlength) / 2;
        camera.GetComponent<Camera>().nearClipPlane = 3f;
        int resx = 2048, resy = (int)(resx * 9 / 16);
        camera.GetComponent<Camera>().targetTexture = new RenderTexture(resx, resy, 24);
        camera.GetComponent<Camera>().Render();
        RenderTexture.active = camera.GetComponent<Camera>().targetTexture;
        Texture2D image = new Texture2D(resx, resy, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, resx, resy), 0, 0);
        image.Apply();
        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        DestroyImmediate(camera);

        // ScreenCapture.CaptureScreenshot(savePath);
    }

    void CreateTerrainFromHeightmap(string heightmapPath, string labelmapPath, string sceneName)
    {
        // 创建新场景
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(newScene);

        // 添加平行光
        //CreateDirectionalLight();

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

        // 添加HDRP特定的光源数据组件
        HDAdditionalLightData hdLight = lightGameObject.AddComponent<HDAdditionalLightData>();
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
