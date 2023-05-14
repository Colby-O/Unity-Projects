using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrian : MonoBehaviour
{
    const float viwerMoveThresholdForChunkUpdate = 25f;
    const float sqrViwerMoveThresholdForChunkUpdate = viwerMoveThresholdForChunkUpdate * viwerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDistacne;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static worldGenerator _worldGenerator;
    int chunkSize;
    int chunksVisableInViewDist;

    Dictionary<Vector2, TerrainChunk> terrianChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> chunksVisableLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        _worldGenerator = FindObjectOfType<worldGenerator>();

        maxViewDistacne = detailLevels[detailLevels.Length - 1].visableDistThresHold;
        chunkSize = _worldGenerator.mapChunkSize - 1;
        chunksVisableInViewDist = Mathf.RoundToInt(maxViewDistacne / chunkSize);

        UpdateVisableChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / _worldGenerator.terrianData.uniformScale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViwerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisableChunks();
        }
    }

    void UpdateVisableChunks()
    {
        for (int i = 0; i < chunksVisableLastUpdate.Count; ++i)
        {
            chunksVisableLastUpdate[i].SetVisable(false);
        }

        chunksVisableLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int i = -chunksVisableInViewDist; i <= chunksVisableInViewDist; ++i)
        {
            for (int j = -chunksVisableInViewDist; j <= chunksVisableInViewDist; ++j)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + i, currentChunkCoordY + j);

                if (terrianChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrianChunkDict[viewedChunkCoord].Update();
                }
                else
                {
                    terrianChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial, _worldGenerator.forestData, _worldGenerator.terrianData));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        readonly ForestData forestData;
        readonly TerrianData terrianData;
        int size;

        bool isForestGenerated;
        bool isForestInit;
        List<GameObject> forestObjects;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parnet, Material material, ForestData forestData, TerrianData terrianData)
        {
            this.detailLevels = detailLevels;
            this.forestData = forestData;
            this.terrianData = terrianData;
            this.size = size;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrian Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * _worldGenerator.terrianData.uniformScale;
            meshObject.transform.parent = parnet;
            meshObject.transform.localScale = Vector3.one * _worldGenerator.terrianData.uniformScale;

            SetVisable(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; ++i)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, Update);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            _worldGenerator.RequestMapData(position,OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Update();
        }

        public void Update()
        {
            if (mapDataReceived)
            {
                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool isVisble = viewerDistFromNearestEdge <= maxViewDistacne;
                bool isForestVisble = viewerDistFromNearestEdge <= forestData.forestRenderRange;
                // Terrian Generation
                if (isVisble)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; ++i)
                    {
                        if (viewerDistFromNearestEdge > detailLevels[i].visableDistThresHold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    if (lodIndex == 0)
                    {
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        else if (!collisionLODMesh.hasRequestedMesh)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }

                    chunksVisableLastUpdate.Add(this);
                }

                SetVisable(isVisble);

                // Forest Generation
                if (!isForestInit && isForestVisble)
                {
                    forestObjects = ForestGenerator.GenerateForest(forestData, size, mapData.heightMap, position, terrianData, meshObject.transform.parent);
                    isForestInit = true;
                    isForestGenerated = true;
                }
                else if (!isForestGenerated && isForestVisble)
                {
                    ForestGenerator.ReactivateForest(forestObjects);
                    isForestGenerated = true;
                }
                else if (isForestGenerated && !isForestVisble)
                {
                    ForestGenerator.DeactivateForest(forestObjects);
                    isForestGenerated = false;
                }
            }
        }

        public void SetVisable(bool isVisble)
        {
            meshObject.SetActive(isVisble);
        }

        public bool IsVisable()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            _worldGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visableDistThresHold;
        public bool useForCollider;
    }

}
