using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrianMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnSimplfied = borderedSize - 2;

        float topLeftX = (meshSizeUnSimplfied - 1) / -2f;
        float topLeftZ = (meshSizeUnSimplfied - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);

        int[,] vertexIndiceMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;


        for (int i = 0; i < borderedSize; i += meshSimplificationIncrement)
        {
            for (int j = 0; j < borderedSize; j += meshSimplificationIncrement)
            {
                bool isBorderVertex = i == 0 || i == borderedSize - 1 || j == 0 || j == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexIndiceMap[i, j] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndiceMap[i, j] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int i = 0; i < borderedSize; i += meshSimplificationIncrement)
        {
            for (int j = 0; j < borderedSize; j += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndiceMap[i, j];
                Vector2 percent = new Vector2((i - meshSimplificationIncrement) / (float)meshSize, (j - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[i, j]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnSimplfied, height, topLeftZ - percent.y * meshSizeUnSimplfied);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (i < borderedSize - 1 && j < borderedSize - 1)
                {
                    int a = vertexIndiceMap[i, j];
                    int b = vertexIndiceMap[i + meshSimplificationIncrement, j];
                    int c = vertexIndiceMap[i, j + meshSimplificationIncrement];
                    int d = vertexIndiceMap[i + meshSimplificationIncrement, j + meshSimplificationIncrement];

                    meshData.AddTrianlge(a, d, c);
                    meshData.AddTrianlge(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.Finalize();

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTrianglesIndex;

    bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[6 * (verticesPerLine - 1) * (verticesPerLine - 1)];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTrianlge(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTrianglesIndex] = a;
            borderTriangles[borderTrianglesIndex + 1] = b;
            borderTriangles[borderTrianglesIndex + 2] = c;
            borderTrianglesIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; ++i)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; ++i)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if ( vertexIndexA >= 0) 
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; ++i)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA]; 
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // Cross product
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void Finalize()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; ++i)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (useFlatShading) {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
