using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveChunk
{

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    GameObject gameObject;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    MeshFilter meshFilter;

    public Bounds bounds;

    public Vector3 chunkPosition;

    float terrianSurface = 0.5f;

    int size 
    {
        get
        {
            return caveData.size;
        }   
    }

    float[,,] terrianMap;

    int configIndex;

    public CaveData caveData;


    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            //Generate();
        }
    }
    public CaveChunk(Vector3 position, CaveData caveData)
    {
        this.caveData = caveData;
        gameObject = new GameObject("Cave Chunk");
        chunkPosition = position * size;
        gameObject.transform.position = chunkPosition;

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshRenderer.material = caveData.caveMaterial;

        bounds = new Bounds(chunkPosition, Vector3.one * size);

        gameObject.transform.tag = "Cave";
        terrianMap = new float[size + 1, size + 1, size + 1];

         GenerateTerrianMap();
         CreateMeshData();
         CreateMesh();
    }

    public void Generate()
    {
        GenerateTerrianMap();
        CreateMeshData();
        CreateMesh();
    }

    void GenerateTerrianMap()
    {
        float noiseScale = caveData.nosieScale;
        float threshold = caveData.threshold;

        float[,,] noiseMap = PerlinNoise3D.Perlin3D(size + 1, size + 1, size + 1, caveData.seed, caveData.nosieScale, caveData.octaves, caveData.persistance, caveData.lacunarity, chunkPosition + caveData.offset);

        for (int i = 0; i < size + 1; ++i)
        {
            for (int j = 0; j < size + 1; ++j)
            {
                for (int k = 0; k < size + 1; ++k)
                {
                    float noise = noiseMap[i, j, k];
                    float point = 0;

                    if (noise >= threshold)
                    {
                        point = 1.0f;
                    }
                    else
                    {
                        point = 0.0f;
                    }

                    terrianMap[i, j, k] = point;
                }
            }
        }
    }

    void CreateMeshData()
    {
        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                for (int k = 0; k < size; ++k)
                {
                    float[] cube = new float[8];

                    for (int c = 0; c < 8; ++c)
                    {
                        Vector3Int corner = new Vector3Int(i, j, k) + CaveData.cornerTable[c];
                        cube[c] = terrianMap[corner.x, corner.y, corner.z];
                    }

                    MarchingCubes(new Vector3(i, j, k), cube);
                }
            }
        }
    }

    int GetCubeConfig(float[] cube)
    {
        int configIndex = 0;
        for (int i = 0; i < 8; ++i)
        {
            if (cube[i] > terrianSurface)
            {
                configIndex |= 1 << i; // 00000000 put a 1 at position i ex. 00000000 -> 00010000
            }
        }

        return configIndex;
    }

    void MarchingCubes(Vector3 position, float[] cube)
    {
        int configIndex = GetCubeConfig(cube);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }
        int edgeIndex = 0;

        for (int i = 0; i < 5; ++i) // never more than 5 triangles
        {
            for (int j = 0; j < 3; ++j) // never more than 3 points per triangle
            {
                int index = CaveData.triangleTable[configIndex, edgeIndex];

                if (index == -1)
                {
                    return;
                }

                // start of an edge
                Vector3 vertice1 = position + CaveData.edgeTable[index, 0];
                // end of an edge 
                Vector3 vertice2 = position + CaveData.edgeTable[index, 1];

                Vector3 vertPos = (vertice1 + vertice2) / 2.0f; // mid point between edge

                vertices.Add(vertPos);
                triangles.Add(vertices.Count - 1); // index of the last item we added
                edgeIndex++;

            }
        }
    }

    public void SetVisable(bool isVisable)
    {
        gameObject.SetActive(isVisable);
    }

    public void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
        meshFilter.mesh = mesh;
    }

    private void OnValidate()
    {
        if (caveData != null)
        {
            caveData.OnValueUpdated -= OnValuesUpdated;
            caveData.OnValueUpdated += OnValuesUpdated;
        }
    }
}
