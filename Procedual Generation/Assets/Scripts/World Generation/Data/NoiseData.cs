using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdateableData
{
    public Noise.NormalizeMode normalizeMode;
    [Range(0, 2)]
    public float normalzieFactor; // Only used if NormalziedMode is equal to global
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (noiseScale <= 0)
        {
            noiseScale = 0.001f;
        }

        base.OnValidate();
    }
#endif
}
