using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManger_CPU : MonoBehaviour
{
    public Material mat;
    public Texture2D tex;

    private float[][] currentWave, pastWave, nextWave;

    private float textureWidth = 10;
    private float textureHeight = 10;
    // dx and dy is the x and y axis density respectively. Assume dx = dy
    public float dx = 0.1f;
    float dy { get => dx; }

    // resolution in the x and y directions
    int nx;
    int ny;

    // Courant-Friedrichs-Lewy (CFL) Stability Criterion
    [Range(0,0.99f)]
    public float CFL = 0.5f; 
    public float c = 1;
    // time step
    float dt;
    // current time
    float t;

    public float scale = 2.0f;
    public float pulseFreq = 1.0f;
    public float pulseAmp = 1.0f;

    public Vector2Int pulsePos = new Vector2Int(50, 50);

    public bool useReflectibeBoundaryCondition;

    // How long for wave to decipation
    [Range(0,1)]
    public float elasticity = 0.98f;


    void Start()
    {
        nx = Mathf.FloorToInt(textureWidth / dx);
        ny = Mathf.FloorToInt(textureHeight / dy);
        tex = new Texture2D(nx, ny, TextureFormat.RGBA32, false);

        // Inits fields
        currentWave = new float[nx][];
        pastWave = new float[nx][];
        nextWave = new float[nx][];

        for (int i = 0; i < nx; ++i)
        {
            currentWave[i] = new float[ny];
            pastWave[i] = new float[ny];
            nextWave[i] = new float[ny];
        }

        // color texture
        mat.SetTexture("_MainTex", tex); 
        // dispalcement map
        //mat.SetTexture("_Displacement", tex);
    }

    void WaveStep()
    {
        // Calculates dt for current step
        dt = CFL * dx / c;
        // update current time
        t += dx;

        if (useReflectibeBoundaryCondition)
        {
            ApplyReflectiveBoundary();
        } else
        {
            ApplyAbsorptiveBoundary();
        }

        for (int i = 0; i < nx; ++i)
        {
            for (int j = 0; j < ny; ++j)
            {
                // Update the previous wave state
                pastWave[i][j] = currentWave[i][j];
                // Updates the current wave state
                currentWave[i][j] = nextWave[i][j];


            }
        }

        // dripping effect
        currentWave[pulsePos.x][pulsePos.y] = dt * dt * pulseAmp * Mathf.Cos(t * Mathf.Rad2Deg * pulseFreq);

        // Loop through all elements of the texture ignoring the boundaries for now
        // Boundary conditions will be applied in a different function
        for (int i = 1; i < nx - 1; ++i)
        {
            for (int j = 1; j < ny - 1; ++j)
            {
                float n00 = currentWave[i][j];
                float n10 = currentWave[i + 1][j];
                float nm10 = currentWave[i - 1][j];
                float n01 = currentWave[i][j + 1];
                float n0m1 = currentWave[i][j - 1];
                float n00Past = pastWave[i][j];

                // Computes the wave equation at the current grid point
                nextWave[i][j] = 2.0f * n00 - n00Past + CFL * CFL * (n0m1 + n01 + nm10 + n10 - 4.0f * n00);
                nextWave[i][j] *= elasticity;
            }
        }
    }

    void ApplyReflectiveBoundary()
    {
        // Boundary is assumed to be perfectly reflective
        for (int i = 0; i < nx; ++i)
        {
            currentWave[i][0] = 0.0f;
            currentWave[i][ny - 1] = 0.0f;
        }

        for (int j = 0; j < ny; ++j)
        {
            currentWave[0][j] = 0.0f;
            currentWave[nx - 1][j] = 0.0f;
        }
    }

    void ApplyAbsorptiveBoundary()
    {
        // Boundary is assumed to be perfectly transmissive
        float v = (CFL - 1.0f) / (CFL + 1.0f);
        for (int i = 0; i < nx; ++i)
        {
            nextWave[i][0] = currentWave[i][1] + v * (nextWave[i][1] - currentWave[i][0]);
            nextWave[i][ny - 1] = currentWave[i][ny - 2] + v * (nextWave[i][ny - 2] - currentWave[i][ny - 1]);
        }

        for (int j = 0; j < ny; ++j)
        {
            nextWave[0][j] = currentWave[1][j] + v * (nextWave[1][j] - currentWave[0][j]);
            nextWave[nx - 1][j] = currentWave[nx - 2][j] + v * (nextWave[nx - 2][j] - currentWave[nx - 1][j]);
        }
    }

    void ApplyWaveStateToTexture(float[][] state, ref Texture2D tex, float scale)
    {
        for (int i = 0; i < nx; ++i)
        {
            for (int j = 0; j < ny; ++j)
            {
                // Grey Scale Value
                tex.SetPixel(i, j, new Color(state[i][j] * scale + 0.5f, state[i][j] * scale + 0.5f, state[i][j] * scale + 0.5f, 1.0f));
            }
        }

        tex.Apply();
    }

    void Update()
    {
        WaveStep();
        ApplyWaveStateToTexture(currentWave, ref tex, scale);
    }
}
