using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManger_Copy : MonoBehaviour
{

    [Header("References")]
    public ComputeShader waveCompute;

    [Header("Wave States")]
    public RenderTexture pastWave;
    public RenderTexture currentWave;
    public RenderTexture nextWave;

    public Vector2Int resolution = new Vector2Int(100, 100);

    public RenderTexture obstaclesTex;

    public Vector3 effect;

    public Material material;

    // How long for wave to decipation
    [Range(0, 1)]
    public float elasticity = 0.98f;

    public bool useReflectiveBoundaryCondition;

    void InitilizeTexture(ref RenderTexture tex)
    {
        tex = new RenderTexture(resolution.x, resolution.y, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SNorm);
        tex.enableRandomWrite = true;
        tex.Create();
    }

    void Start()
    {
        InitilizeTexture(ref currentWave);
        InitilizeTexture(ref pastWave);
        InitilizeTexture(ref nextWave);
        
        obstaclesTex.enableRandomWrite = true;
        Debug.Assert(obstaclesTex.width == resolution.x && obstaclesTex.height == resolution.y);
        material.mainTexture = currentWave;
    }


    void Update()
    {
        Graphics.CopyTexture(currentWave, pastWave);
        Graphics.CopyTexture(nextWave, currentWave);

        waveCompute.SetTexture(waveCompute.FindKernel("CSMain"), "pastWave", pastWave);
        waveCompute.SetTexture(waveCompute.FindKernel("CSMain"), "currentWave", currentWave);
        waveCompute.SetTexture(waveCompute.FindKernel("CSMain"), "nextWave", nextWave);
        waveCompute.SetVector("effect", effect);
        waveCompute.SetInts("resolution", new int[2] { resolution.x, resolution.y });
        waveCompute.SetFloat("elasticity", elasticity);
        waveCompute.SetBool("useReflectiveBoundaryCondition", useReflectiveBoundaryCondition);
        waveCompute.SetTexture(waveCompute.FindKernel("CSMain"), "obstaclesTex", obstaclesTex);
        
        waveCompute.Dispatch(waveCompute.FindKernel("CSMain"), resolution.x / 8, resolution.y / 8, 1);
    }
}
