using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

public class GrassPlacer : EditorWindow
{
    private Terrain terrain;
    private int grassDensity = 500; // �ݵ��ܶ�

    [MenuItem("Tools/Grass Placer")]
    public static void ShowWindow()
    {
        GetWindow<GrassPlacer>("Grass Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Grass Placement Settings", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        grassDensity = EditorGUILayout.IntSlider("Grass Density", grassDensity, 100, 1000);

        if (GUILayout.Button("Place Grass"))
        {
            PlaceGrass();
        }
    }

    void PlaceGrass()
    {
        if (terrain == null)
        {
            UnityEngine.Debug.LogError("Terrain not set.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // �������Ƿ�����Ч��ϸ�ڲ�
        bool hasValidDetailLayer = false;
        foreach (var detailPrototype in terrainData.detailPrototypes)
        {
            if (detailPrototype.prototypeTexture != null || detailPrototype.prototype != null)
            {
                hasValidDetailLayer = true;
                break;
            }
        }

        // ���û����Ч��ϸ�ڲ㣬�򴴽�һ���µ�ϸ�ڲ�
        if (!hasValidDetailLayer)
        {
            UnityEngine.Debug.Log("No valid detail layers found. Creating a new one.");
            DetailPrototype detailPrototype = new DetailPrototype();
            // ���òݵ���ͼ����Ⱦģʽ
            detailPrototype.renderMode = DetailRenderMode.GrassBillboard;
            // detailPrototype.prototypeTexture = ...; // ָ���ݵ���ͼ

            terrainData.detailPrototypes = new DetailPrototype[] { detailPrototype };
        }

        int[,] detailMap = new int[detailWidth, detailHeight];

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                detailMap[x, y] = UnityEngine.Random.Range(0, grassDensity);
            }
        }

        // ��ֲ��
        terrainData.SetDetailLayer(0, 0, 0, detailMap);
    }
}
