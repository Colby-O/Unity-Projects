using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateNoise : MonoBehaviour
{
	[Header("Noise Parameters")]

	[Min(0)]
	public int textureSize = 256;
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

	[Header("Texture")]
	public RenderTexture renderTexture;

	void UpdateNoise(int numThreads)
	{
		noiseShader.SetTexture(0, "Result", renderTexture);
		noiseShader.SetInt("textureSize", textureSize);
		noiseShader.SetFloat("lacunarity", lacunarity);
		noiseShader.SetInt("octaves", octaves);
		noiseShader.SetFloat("persistence", persistence);
		noiseShader.SetFloat("noiseScale", noiseScale);

		noiseShader.Dispatch(noiseShader.FindKernel("CSMain"), renderTexture.width / numThreads, renderTexture.height / numThreads, renderTexture.volumeDepth / numThreads);
	}

	void Start()
	{
		renderTexture = new RenderTexture(textureSize, textureSize, 0);
		renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
		renderTexture.volumeDepth = textureSize;
		renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
		renderTexture.enableRandomWrite = true;
		renderTexture.wrapMode = TextureWrapMode.Repeat;
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.Create();


	}

	void Update()
	{
		UpdateNoise(8);
	}
}
