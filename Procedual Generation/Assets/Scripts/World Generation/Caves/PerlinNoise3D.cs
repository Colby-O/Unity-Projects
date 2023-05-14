using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise3D : MonoBehaviour
{
    public static float[,,] Perlin3D(int width, int height, int depth, int seed, float scale, int octaves, float persistance, float lacunarity, Vector3 offset)
    {
        float[,,] noiseMap = new float[width, height, depth];

        System.Random rand = new System.Random(seed);
        Vector3[] octaveOffsets = new Vector3[octaves];

        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rand.Next(-100000, 100000) + offset.x;
            float offsetY = rand.Next(-100000, 100000) - offset.y;
            float offsetZ = rand.Next(-100000, 100000) + offset.z;
            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float halfDepth = depth / 2f;

        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                for (int k = 0; k < depth; ++k)
                {
                    amplitude = 1;
                    frequency = 1;
                    float noiseHeight = 0;
                    for (int o = 0; o< octaves; ++o)
                    {
                        float sampleX = (i - halfWidth + octaveOffsets[o].x) / scale * frequency;
                        float sampleY = (j - halfHeight + octaveOffsets[o].y) / scale * frequency;
                        float sampleZ = (k - halfDepth + octaveOffsets[o].z) / scale * frequency;

                        float xy = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        float xz = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
                        float yz = Mathf.PerlinNoise(sampleY, sampleZ) * 2 - 1;

                        float yx = Mathf.PerlinNoise(sampleY, sampleX) * 2 - 1;
                        float zx = Mathf.PerlinNoise(sampleZ, sampleX) * 2 - 1;
                        float zy = Mathf.PerlinNoise(sampleZ, sampleY) * 2 - 1;

                        float perlinVal = (xy + xz + yz + yx + zx + zy) / 6.0f;
                        noiseHeight += perlinVal * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    noiseMap[i, j, k] = noiseHeight;
                }
            }
        }
        return noiseMap;
    }
}
