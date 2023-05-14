using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vertex
{
    public Vector3 pos;
    public Vector3 normal;
    public Vector2 id;
}
public struct Triangle
{
    public Vertex vA;
    public Vertex vB;
    public Vertex vC;
}

[ExecuteInEditMode]
public class PlanetManger : MonoBehaviour
{
    [Header("Planet Parameters")]

    [Min(0)]
    public float radius = 1.0f;
    public Material planetMaterial;

    [Header("Water Parameters")]
    [Min(0)]
    public float underwaterViewDistance = 100.0f;
    public Color underwaterColor = Color.blue;
    [Range(0, 1)]
    public float depthFactor;
    [Range(0, 15)]
    public float fresnelFactor;
    [Range(0, 1)]
    public float shoreFadeFactor;
    [Range(0, 1)]
    public float ks = 0.1f;
    [Range(0, 1)]
    public float smoothnessFactor = 1.0f;
    [Range(0, 1)]
    public float kd = 0.9f;
    [Range(0, 1)]
    public float normalFactor;
    [Range(0, 1)]
    public float waveNormalScale;
    public float waveSpeed;
    public float waveHeightMod;
    public Color depthColor;
    public Color shallowColor;
    public Color specularColor;
    public TextAsset waterNormalMapAImage;
    public TextAsset waterNormalMapBImage;
    public Material waterMaterial;
    public Transform oceanTransfrom;

    [Header("Atmosphere Parameters")]
    public bool enableAtmosphere = true;
    public bool enableClouds = true;
    [Range(0,100)]
    public int numInScatterPoints = 10;
    [Range(0, 100)]
    public int numOpticalDepthPoints = 10;
    [Min(0)]
    public float fallOffFactor = 0.5f;
    [Min(0)]
    public float atmosphereRadius = 125.0f;
    [Min(0)]
    public float intensity = 1.0f;
    [Range(0, 1)]
    public float scatteringStrength = 1.0f;
    [Range(0,100)]
    public float outlookingAtmosphereStrength = 1.0f;
    [Range(0, 100)]
    public float inlookingAtmosphereStrength = 1.0f;
    [Range(0, 2)]
    public float atmosphereStrengthOnSurface = 0.01f;
    public Vector3 sunDir = new Vector3(0, 0, 0);
    public Vector3 wavelengths = new Vector3(700, 530, 440);
    public Material atmosphereMaterial;
    public Transform atmosphereTransfrom;

    [Header("Mesh Parameters")]
    public int numPointsPerAxis = 10;
    public float isoLevel = 0;
    public bool regenerateMesh = false;
    public Mesh mesh;

    [Header("Noise Parameters")]

    [Min(0)]
    public float lacunarity = 2.0f;
    [Range(1, 12)]
    public int octaves = 1;
    [Range(0, 1)]
    public float persistence = 0.5f;
    [Min(0.001f)]
    public float noiseScale = 1.0f;
    [Min(0)]
    public float noiseMultiplier = 1.0f;
    [Range(0.0001f, 1)]
    public float hybridMultifractalFactor = 0.5f;
    [Range(0.0001f, 1)]
    public float fBmFactor = 0.5f;

    private int textureSize;

    [Header("References")]
    public ComputeShader mapShader;
    public ComputeShader noiseShader;
    public ComputeShader meshCompute;

    [Header("Texture")]
    public RenderTexture planetMap;
    public RenderTexture noiseMap;

    private Texture2D waterNormalMapA;
    private Texture2D waterNormalMapB;

    private int numThreads = 8;

    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer triangleCountBuffer;

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
    void GeneratePlanetMap()
    {
        mapShader.SetTexture(mapShader.FindKernel("CSMain"), "planetMap", planetMap);
        mapShader.SetTexture(mapShader.FindKernel("CSMain"), "noiseMap", noiseMap);
        mapShader.SetInt("textureSize", textureSize);
        mapShader.SetFloat("radius", radius);
        mapShader.SetFloat("noiseMultiplier", noiseMultiplier);

        mapShader.Dispatch(mapShader.FindKernel("CSMain"), planetMap.width / numThreads, planetMap.height / numThreads, planetMap.volumeDepth / numThreads);
    }

    void GenerateNoiseMap()
    {

        noiseShader.SetTexture(meshCompute.FindKernel("CSMain"), "Result", noiseMap);
        noiseShader.SetInt("textureSize", textureSize);
        noiseShader.SetFloat("lacunarity", lacunarity);
        noiseShader.SetInt("octaves", octaves);
        noiseShader.SetFloat("persistence", persistence);
        noiseShader.SetFloat("noiseScale", noiseScale); 
        noiseShader.SetFloat("hybridMultifractalFactor", hybridMultifractalFactor);
        noiseShader.SetFloat("fBmFactor", fBmFactor);

        noiseShader.Dispatch(noiseShader.FindKernel("CSMain"), noiseMap.width / numThreads, noiseMap.height / numThreads, noiseMap.volumeDepth / numThreads);
    }

    void GenerateMesh()
    {
        meshCompute.SetTexture(meshCompute.FindKernel("CSMain"), "densityTexture", planetMap);
        meshCompute.SetInt("textureSize", textureSize);
        meshCompute.SetInt("numPointsPerAxis", numPointsPerAxis);
        meshCompute.SetFloat("planetRadius", 2 * 2 * radius);
        meshCompute.SetFloat("isoLevel", isoLevel);


        triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        trianglesBuffer = new ComputeBuffer((numPointsPerAxis - 1) * (numPointsPerAxis - 1) * (numPointsPerAxis - 1) * 5 * 3, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex)), ComputeBufferType.Append);

        trianglesBuffer.SetCounterValue(0);
        meshCompute.SetBuffer(meshCompute.FindKernel("CSMain"), "triangles", trianglesBuffer);
        meshCompute.Dispatch(meshCompute.FindKernel("CSMain"), (numPointsPerAxis - 1) / numThreads, (numPointsPerAxis - 1) / numThreads, (numPointsPerAxis - 1) / numThreads);

        int[] vertexCountData = new int[1];
        triangleCountBuffer.SetData(vertexCountData);
        ComputeBuffer.CopyCount(trianglesBuffer, triangleCountBuffer, 0);
        triangleCountBuffer.GetData(vertexCountData);

        Vertex[] vertexDataArray = new Vertex[(numPointsPerAxis - 1) * (numPointsPerAxis - 1) * (numPointsPerAxis - 1) * 5 * 3];
        trianglesBuffer.GetData(vertexDataArray, 0, 0, vertexCountData[0] * 3);

        List<Vector3> processedVertices = new List<Vector3>();
        List<Vector3> processedNormals = new List<Vector3>();
        List<int> processedTriangles = new List<int>();
        Dictionary<Vector2, int> vertexIndexMap = new Dictionary<Vector2, int>();

        int triangleIndex = 0;

        for (int i = 0; i < vertexCountData[0] * 3; i++)
        {
            Vertex data = vertexDataArray[i];

            int sharedVertexIndex;
            if (vertexIndexMap.TryGetValue(data.id, out sharedVertexIndex))
            {
                processedTriangles.Add(sharedVertexIndex);
            }
            else
            {
                vertexIndexMap.Add(data.id, triangleIndex);
                processedVertices.Add(data.pos);
                processedNormals.Add(data.normal);
                processedTriangles.Add(triangleIndex);
                triangleIndex++;
            }
        }
        if (mesh != null)
        {
            mesh.Clear();
        }
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(processedVertices);
        processedTriangles.Reverse();
        mesh.SetTriangles(processedTriangles, 0, true);
        mesh.SetNormals(processedNormals);

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        triangleCountBuffer.Dispose();
        trianglesBuffer.Dispose();
    }

    void RenderWater()
    {
        waterNormalMapA.LoadImage(waterNormalMapAImage.bytes);
        waterNormalMapB.LoadImage(waterNormalMapBImage.bytes);
        waterMaterial.SetTexture("planetMap", planetMap);
        waterMaterial.SetTexture("wavePhysicsMap", GetComponent<WaveManger>().currentWave);
        waterMaterial.SetTexture("waveNormalA", waterNormalMapA);
        waterMaterial.SetTexture("waveNormalB", waterNormalMapB);
        waterMaterial.SetColor("depthColor", depthColor);
        waterMaterial.SetColor("specularColor", specularColor);
        waterMaterial.SetColor("shallowColor", shallowColor);
        waterMaterial.SetFloat("depthFactor", depthFactor);
        waterMaterial.SetFloat("fresnelFactor", fresnelFactor);
        waterMaterial.SetFloat("shoreFadeFactor", shoreFadeFactor);
        waterMaterial.SetFloat("smoothnessFactor", smoothnessFactor);
        waterMaterial.SetFloat("waveHeightMod", waveHeightMod);
        waterMaterial.SetFloat("ks", ks);
        waterMaterial.SetFloat("kd", kd);
        waterMaterial.SetFloat("waveSpeed", waveSpeed);
        waterMaterial.SetFloat("normalFactor", normalFactor);
        waterMaterial.SetFloat("waveNormalScale", waveNormalScale);
        waterMaterial.SetFloat("radius", radius);
        waterMaterial.SetFloat("scale", transform.localScale.x);
        waterMaterial.SetVector("dirToSun", sunDir.normalized);
    }

    void RenderAtmosphere()
    {
        /*
        atmosphereMaterial.SetVector("planetCenter", transform.position);
        atmosphereMaterial.SetFloat("scale", transform.localScale.x);
        atmosphereMaterial.SetFloat("atmosphereRadius", atmosphereTransfrom.localScale.x * transform.localScale.x * radius);
        atmosphereMaterial.SetFloat("oceanRadius", oceanTransfrom.localScale.x * transform.localScale.x * radius);
        atmosphereMaterial.SetFloat("planetRadius", transform.localScale.x * radius);
        atmosphereMaterial.SetFloat("fallOffFactor", fallOffFactor);
        atmosphereMaterial.SetInt("numOpticalDepthPoints", numOpticalDepthPoints);
        atmosphereMaterial.SetInt("numInScatterPoints", numInScatterPoints);
        atmosphereMaterial.SetVector("dirToSun", new Vector3(0, 0, -1));
        */
    }


    void GeneratePlanet()
    {
        // TODO: Combine Noise and PlanetMap generation into one shader
        // Slow to pass both to GPU twice!!!!
        GenerateNoiseMap();
        GeneratePlanetMap();
        GenerateMesh();
        RenderWater();
        RenderAtmosphere();
    }

    void Start()
    {
        if (Camera.main.depthTextureMode != DepthTextureMode.Depth)
            Camera.main.depthTextureMode = DepthTextureMode.Depth;

        textureSize = numPointsPerAxis;
        waterNormalMapA = new Texture2D(1, 1);
        waterNormalMapB = new Texture2D(1, 1);
        CreateRenderTexture(ref planetMap);
        CreateRenderTexture(ref noiseMap);
        GeneratePlanet();
    }

    void Update()
    {
        RenderWater();
        RenderAtmosphere();
        planetMaterial.SetTexture("planetMap", planetMap);
        planetMaterial.SetVector("planetCenter", transform.position);
        planetMaterial.SetFloat("radius", radius);
        planetMaterial.SetFloat("scale", transform.localScale.x);
        if (regenerateMesh)
        {
            textureSize = numPointsPerAxis;
            CreateRenderTexture(ref planetMap);
            CreateRenderTexture(ref noiseMap);
            GeneratePlanet();
            regenerateMesh = false;
        }
    }
}
