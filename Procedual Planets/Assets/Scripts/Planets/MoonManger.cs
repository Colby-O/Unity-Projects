using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MoonManger : MonoBehaviour
{

    [Header("Moon Parameters")]

    [Min(0)]
    public float radius = 1.0f;
    [Range(0, 1)]
    public float normalScale = 0.5f;
    public Material moonMaterial;
    public TextAsset normalMapImage;

    private Texture2D normalMap;

    [Header("Crater Parameters")]
    [Min(0)]
    public int numCraters = 1;
    [Range(0, 1)]
    public float maxCraterRadius = 0.53f;
    [Range(0, 1)]
    public float maxCraterDepth = 0.3f;
    public int seed = 0;
    [Range(0, 1)]
    public float bias;
    // public Vector3 craterCenter = new Vector3(1.0f, 0.0f, 0.0f);

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
        mapShader.SetTexture(mapShader.FindKernel("CSMain"), "moonMap", planetMap);
        mapShader.SetTexture(mapShader.FindKernel("CSMain"), "noiseMap", noiseMap);
        mapShader.SetInt("textureSize", textureSize);
        mapShader.SetFloat("radius", radius);
        mapShader.SetFloat("noiseMultiplier", noiseMultiplier);
        mapShader.SetInt("numCraters", numCraters);
        mapShader.SetInt("_seed", seed);
        mapShader.SetFloat("maxCraterRadius", maxCraterRadius); 
        mapShader.SetFloat("maxCraterDepth", maxCraterDepth);
        mapShader.SetFloat("bias", bias);

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


    void GeneratePlanet()
    {
        // TODO: Combine Noise and PlanetMap generation into one shader
        // Slow to pass both to GPU twice!!!!
        GenerateNoiseMap();
        GeneratePlanetMap();
        GenerateMesh();
        normalMap.LoadImage(normalMapImage.bytes);
        moonMaterial.SetTexture("normalMap", normalMap);
        moonMaterial.SetFloat("normalScale", normalScale / transform.localScale.x);
        moonMaterial.SetVector("moonCenter", transform.position);
        moonMaterial.SetVector("dirToSun", FindObjectOfType<PlanetManger>().sunDir);
    }

    void Start()
    {
        normalMap = new Texture2D(1, 1);
        //if (Camera.main.depthTextureMode != DepthTextureMode.Depth)
        //    Camera.main.depthTextureMode = DepthTextureMode.Depth;

        textureSize = numPointsPerAxis;
        CreateRenderTexture(ref planetMap);
        CreateRenderTexture(ref noiseMap);
        GeneratePlanet();
    }

    void Update()
    {
        //GeneratePlanet();
        if (regenerateMesh)
        {
            textureSize = numPointsPerAxis;
            CreateRenderTexture(ref planetMap);
            CreateRenderTexture(ref noiseMap);
            GeneratePlanet();
            regenerateMesh = false;
        } else
        {
            moonMaterial.SetTexture("normalMap", normalMap);
            moonMaterial.SetFloat("normalScale", normalScale / transform.localScale.x);
            moonMaterial.SetVector("moonCenter", transform.position);
            moonMaterial.SetVector("dirToSun", FindObjectOfType<PlanetManger>().sunDir);
        }
    }
}
