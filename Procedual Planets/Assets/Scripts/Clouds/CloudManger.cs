using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CloudManger : MonoBehaviour
{
    public bool regenerateNoise = false;
    public int textureSize = 256;
    public int seed = 0;
    [Min(0)]
    public float innerCloudRadius = 80;
    [Min(0)]
    public float outerCloudRadius = 100;
    [Min(0)]
    public int numSamplePoints = 10;
    [Range(0, 1)]
    public float cloudSpeed = 0.01f;
    [Min(0)]
    public float cloudScale = 1.0f;
    [Range(0, 1)]
    public float cloudThreshold = 0.0f;
    [Range(0, 4)]
    public float lightAbsorptionTowardsSunFactor = 0.5f;
    [Range(0, 4)]
    public float lightAbsorptionThroughCloudFactor = 0.5f;
    [Range(0, 1)]
    public float lightEnergyFactor = 1.0f;
    [Range(0, 1)]
    public float darknessThreshold = 0.1f;
    [Min(0)]
    public float cloudOffset = 0.0f;

    public Color cloudColor = Color.white; 

    public Material cloudMaterial;
    public RenderTexture noiseMap;

    [Header("Noise Parameters")]
    [Range(2, 20)]
    public int gridSize = 2;
    [Range(0, 5)]
    public float pointDensity = 0.5f;
    [Range(0.1f, 4)]
    public float fluffFactor = 1.0f;
    [Range(0, 1)]
    public float smoothness = 0.1f;
    [Min(0)]
    public float lacunarity = 2.0f;
    [Range(1, 12)]
    public int octaves = 1;
    [Range(0, 1)]
    public float persistence = 0.5f;
    [Min(0.001f)]
    public float noiseScale = 1.0f;

    [Header("References")]
    public ComputeShader noiseShader;

    private int numThreads = 8;

    void CreateRenderTexture(ref RenderTexture tex)
    {
        tex = new RenderTexture(textureSize, textureSize, 0);
        tex.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        tex.volumeDepth = textureSize;
        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        tex.enableRandomWrite = true;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Create();
    }

    void GenerateNoiseMap()
    {

        noiseShader.SetTexture(noiseShader.FindKernel("CSMain"), "Result", noiseMap);
        noiseShader.SetFloat("lacunarity", lacunarity);
        noiseShader.SetInt("octaves", octaves);
        noiseShader.SetFloat("persistence", persistence);
        noiseShader.SetFloat("noiseScale", noiseScale);
        noiseShader.SetFloat("fluffFactor", fluffFactor);
        noiseShader.SetFloat("pointDensity", pointDensity);
        noiseShader.SetFloat("smoothness", smoothness);
        noiseShader.SetInt("gridSize", gridSize);
        noiseShader.SetInt("textureSize", textureSize);
        noiseShader.SetInt("_seed", seed);

        noiseShader.Dispatch(noiseShader.FindKernel("CSMain"), noiseMap.width / numThreads, noiseMap.height / numThreads, noiseMap.volumeDepth / numThreads);
    }

    void GenerateClouds()
    {
        GenerateNoiseMap();
        //cloudMaterial.SetFloat("atmosphereRadius", 300.0f);
        //cloudMaterial.SetVector("planetCenter", new Vector3(0.0f, 0.0f, 0.0f));
    }

    void Start()
    {
        CreateRenderTexture(ref noiseMap);
        GenerateClouds();
    }

    void Update()
    {
        if (regenerateNoise)
        {
            GenerateClouds();
            //GenerateNoiseMap();
        }
    }
}
