using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                colorMap[j * width + i] = Color.Lerp(Color.black, Color.white, heightMap[i, j]);
            }
        }
        return TextureFromColorMap(colorMap, width, height);
    }
}
