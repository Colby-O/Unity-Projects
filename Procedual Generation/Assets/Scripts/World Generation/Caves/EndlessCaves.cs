using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCaves : MonoBehaviour
{
    const float viwerMoveThresholdForChunkUpdate = 25f;
    const float sqrViwerMoveThresholdForChunkUpdate = viwerMoveThresholdForChunkUpdate* viwerMoveThresholdForChunkUpdate;
    public CaveData caveData;
    public static float maxViewDistacne;

    public Transform viewer;
    public static Vector3 viewerPosition;
    Vector3 viewerPositionOld;

    int chunkSize;
    int chunksVisableInViewDist;

    Dictionary<Vector3, CaveChunk> caveChunks = new Dictionary<Vector3, CaveChunk>();
    static List<CaveChunk> caveChunksVisableLastUpdate = new List<CaveChunk>();

    void Start()
    {
        Generate();
        chunkSize = caveData.size - 1;
        maxViewDistacne = caveData.viewDist;
        chunksVisableInViewDist = Mathf.RoundToInt(maxViewDistacne / chunkSize);
    }

    void Update()
    {
        viewerPosition = new Vector3(viewer.position.x, viewer.position.y, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViwerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            Generate();
        }
    }

    void Generate()
    {
        for (int i = 0; i < caveChunksVisableLastUpdate.Count; ++i)
        {
            caveChunksVisableLastUpdate[i].SetVisable(false);
        }

        caveChunksVisableLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);

        for (int i = -chunksVisableInViewDist; i <= chunksVisableInViewDist; ++i)
        {
            for (int j = -chunksVisableInViewDist; j <= chunksVisableInViewDist; ++j)
            {
                for (int k = -chunksVisableInViewDist; k <= chunksVisableInViewDist; ++k)
                {
                    Vector3 viewedChunkCoord = new Vector3(currentChunkCoordX + i, currentChunkCoordY + j, currentChunkCoordZ + k);

                    if (!caveChunks.ContainsKey(viewedChunkCoord))
                    {
                        caveChunks.Add(viewedChunkCoord, new CaveChunk(viewedChunkCoord, caveData));
                        caveChunks[viewedChunkCoord].SetVisable(false);
                    }

                    float viewerDistFromNearestEdge = Mathf.Sqrt(caveChunks[viewedChunkCoord].bounds.SqrDistance(viewerPosition));
                    bool isVisble = viewerDistFromNearestEdge <= maxViewDistacne;
                    if (isVisble)
                    {
                        caveChunksVisableLastUpdate.Add(caveChunks[viewedChunkCoord]);
                    }
                    caveChunks[viewedChunkCoord].SetVisable(isVisble);
                }
            }
        }


    }
}
