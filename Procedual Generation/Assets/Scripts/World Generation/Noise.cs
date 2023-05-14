using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise : MonoBehaviour
{
    public enum NormalizeMode { Local, Global};
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, float normalzieFactor)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rand.Next(-100000, 100000) + offset.x;
            float offsetY = rand.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int i = 0; i < mapWidth; ++i)
        {
            for (int j = 0; j < mapHeight; ++j)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int k = 0; k < octaves; ++k)
                {
                    float sampleX = (i - halfWidth + octaveOffsets[k].x) / scale * frequency;
                    float sampleY = (j - halfHeight + octaveOffsets[k].y) / scale * frequency;

                    float perlinVal = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
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

                noiseMap[i, j] = noiseHeight;
            }
        }

        for (int i = 0; i < mapWidth; ++i)
        {
            for (int j = 0; j < mapHeight; ++j)
            {
                if(normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[i, j] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[i, j]);
                }
                else
                {
                    float normlizedHeight = (noiseMap[i, j] + 1) / (2f * maxPossibleHeight / normalzieFactor);
                    noiseMap[i, j] = Mathf.Clamp(normlizedHeight, 0, int.MaxValue);
                }

            }
        }

                return noiseMap;
    }
}
