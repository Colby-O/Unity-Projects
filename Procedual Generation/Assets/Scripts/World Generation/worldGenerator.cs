using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class worldGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh, FallOffMap}
    public DrawMode drawMode;

    public TerrianData terrianData;
    public NoiseData noiseData;
    public TextureData textureData;
    public ForestData forestData;

    public Material terrianMaterial;

    [Range(0, 6)]
    public int editorLOD;

    public bool autoUpdate;

    float[,] fallOffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake()
    {
        textureData.ApplyToMaterial(terrianMaterial); // might have to comment out
        textureData.UpdateMeshHeights(terrianMaterial, terrianData.minHeight, terrianData.maxHeight);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrianMaterial);
    }

    public int mapChunkSize
    {
        get {

            if(terrianData.useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        textureData.UpdateMeshHeights(terrianMaterial, terrianData.minHeight, terrianData.maxHeight);

        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTextureMap(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrianMesh(mapData.heightMap, terrianData.meshHeightMultiplier, terrianData.meshHeightCurve, editorLOD, terrianData.useFlatShading));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTextureMap(TextureGenerator.TextureFromHeightMap(FallOffGenerator.GeneratefallOffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        new Thread (threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod,  Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrianMesh(mapData.heightMap, terrianData.meshHeightMultiplier, terrianData.meshHeightCurve, lod, terrianData.useFlatShading);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; ++i)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; ++i)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode, noiseData.normalzieFactor);

        if (terrianData.useFallOff)
        {
            if (fallOffMap == null)
            {
                fallOffMap = FallOffGenerator.GeneratefallOffMap(mapChunkSize + 2);
            }

            for (int i = 0; i < mapChunkSize + 2; ++i)
            {
                for (int j = 0; j < mapChunkSize + 2; ++j)
                {
                    if (terrianData.useFallOff)
                    {
                        noiseMap[i, j] = Mathf.Clamp(noiseMap[i, j] - fallOffMap[i, j], 0, 1);
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    private void OnValidate()
    {
        if (terrianData != null)
        {
            terrianData.OnValueUpdated -= OnValuesUpdated;
            terrianData.OnValueUpdated += OnValuesUpdated;
        }

        if(noiseData != null)
        {
            noiseData.OnValueUpdated -= OnValuesUpdated;
            noiseData.OnValueUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValueUpdated -= OnTextureValuesUpdated;
            textureData.OnValueUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }


    }
}

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}
