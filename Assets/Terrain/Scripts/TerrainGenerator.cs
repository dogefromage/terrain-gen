using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FalloffSettings
{
    public float radius = 1000;
    public Vector2 center = Vector2.zero;
    public float minHeight = -50;
    public float maxHeight = 100;
    public float falloffPower = 1.5f;
}

[System.Serializable]
public class NoiseSettings
{
    public int octaves = 5;
    public float amplitude = 1f;
    public float scale = 0.1f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
}

[System.Serializable]
public class TerrainLOD
{
    public int resolution = 128;
    public float screenRelativeHeight = 1f;
}

[System.Serializable]
public class TerrainSettings
{
    [Header("General")]
    public int seed = 0;
    public float chunkSize = 10f;
    public int baseMeshResolution = 256;

    [Range(0, 5)]
    public int numberOfLODS = 3;

    [Header("Generation")]
    public FalloffSettings falloff;
    public NoiseSettings baseHeight;
    public NoiseSettings ridgeHeight;

    public float ridgeOffset = 0.5f;
    public bool displayRidge = false;

    public bool recalcNormals = false;

    [Header("Loading")]
    public Transform worldCenter;
    public float loadDistance = 150f;
    public float unloadDistance = 200f;
    public bool displayBounds = false;
}

public class Chunk
{
    public Vector2Int position;
    public GameObject go;
}

public class HeightMap
{
    public RenderTexture texture;
    public int textureResolution;
    public int baseN;
    public int seam;
}

public class TerrainGenerator : MonoBehaviour
{
    public TerrainSettings terrainSettings;

    public bool autoUpdate;

    public ComputeShader meshGridCompute;
    public ComputeShader heightMapCompute;
    //public ComputeShader albedoCompute;

    public Material terrainMaterial;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private HeightMap GenerateHeightMap(TerrainSettings settings, Vector2Int chunk)
    {
        int N = 1 + Mathf.ClosestPowerOfTwo(settings.baseMeshResolution);
        int seam = Mathf.FloorToInt(Mathf.Pow(2, settings.numberOfLODS));
        int textureRes = N + 2 * seam;

        var heightMap = new RenderTexture(textureRes, textureRes, 1)
        {
            enableRandomWrite = true,
            format = RenderTextureFormat.RFloat,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
        };

        int heightMapKernel = 0;

        heightMapCompute.SetInt("N", N);
        heightMapCompute.SetInt("seam", seam);
        heightMapCompute.SetInt("textureRes", textureRes);
        heightMapCompute.SetFloat("size", settings.chunkSize);

        heightMapCompute.SetInts("chunk", chunk.x, chunk.y);

        heightMapCompute.SetTexture(heightMapKernel, "HeightMap", heightMap);

        heightMapCompute.SetInt("seed", settings.seed);

        heightMapCompute.SetFloat("falloff_radius", settings.falloff.radius);
        heightMapCompute.SetVector("falloff_center", settings.falloff.center);
        heightMapCompute.SetFloat("falloff_minHeight", settings.falloff.minHeight);
        heightMapCompute.SetFloat("falloff_maxHeight", settings.falloff.maxHeight);
        heightMapCompute.SetFloat("falloff_falloffPower", settings.falloff.falloffPower);

        heightMapCompute.SetInt("base_octaves", settings.baseHeight.octaves);
        heightMapCompute.SetFloat("base_amplitude", settings.baseHeight.amplitude);
        heightMapCompute.SetFloat("base_scale", settings.baseHeight.scale);
        heightMapCompute.SetFloat("base_persistence", settings.baseHeight.persistence);
        heightMapCompute.SetFloat("base_lacunarity", settings.baseHeight.lacunarity);

        heightMapCompute.SetInt("ridge_octaves", settings.ridgeHeight.octaves);
        heightMapCompute.SetFloat("ridge_amplitude", settings.ridgeHeight.amplitude);
        heightMapCompute.SetFloat("ridge_scale", settings.ridgeHeight.scale);
        heightMapCompute.SetFloat("ridge_persistence", settings.ridgeHeight.persistence);
        heightMapCompute.SetFloat("ridge_lacunarity", settings.ridgeHeight.lacunarity);

        heightMapCompute.SetFloat("ridgeOffset", settings.ridgeOffset);
        heightMapCompute.SetBool("displayRidge", settings.displayRidge);

        ComputeHelper.Dispatch(heightMapCompute, textureRes, textureRes);

        HeightMap hm = new HeightMap()
        {
            texture = heightMap,
            baseN = N,
            seam = seam,
            textureResolution = textureRes,
        };

        return hm;
    }

    private Mesh GenerateMesh(TerrainSettings settings, HeightMap heightMap, int LOD)
    {
        int step = Mathf.RoundToInt(Mathf.Pow(2, LOD));
        int N = Mathf.ClosestPowerOfTwo(settings.baseMeshResolution) / step + 1;

        if (N < 2) return new Mesh();

        int vertBufferLength = N * N;
        int triBufferLength = 6 * (N - 1) * (N - 1);

        ComputeBuffer vertBuffer = new ComputeBuffer(vertBufferLength, 3 * sizeof(float));
        ComputeBuffer normalBuffer = new ComputeBuffer(vertBufferLength, 3 * sizeof(float));
        ComputeBuffer uvsBuffer = new ComputeBuffer(vertBufferLength, 2 * sizeof(float));
        ComputeBuffer triBuffer = new ComputeBuffer(triBufferLength, sizeof(int));

        int meshGridKernel = 0;

        meshGridCompute.SetTexture(meshGridKernel, "HeightMap", heightMap.texture);

        meshGridCompute.SetBuffer(meshGridKernel, "vertices", vertBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "normals", normalBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "uvs", uvsBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "triangles", triBuffer);

        meshGridCompute.SetInt("N", N);
        meshGridCompute.SetInt("step", step);
        meshGridCompute.SetInt("seam", heightMap.seam);

        meshGridCompute.SetFloat("size", settings.chunkSize);

        ComputeHelper.Dispatch(meshGridCompute, N, N);

        var mesh = new Mesh();

        Vector3[] verts = new Vector3[vertBufferLength];
        Vector3[] normals = new Vector3[vertBufferLength];
        Vector2[] uvs = new Vector2[vertBufferLength];
        int[] tris = new int[triBufferLength];

        vertBuffer.GetData(verts, 0, 0, vertBufferLength);
        normalBuffer.GetData(normals, 0, 0, vertBufferLength);
        uvsBuffer.GetData(uvs, 0, 0, vertBufferLength);
        triBuffer.GetData(tris, 0, 0, triBufferLength);

        vertBuffer.Dispose();
        normalBuffer.Dispose();
        uvsBuffer.Dispose();
        triBuffer.Dispose();

        if (vertBufferLength > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;

        if (terrainSettings.recalcNormals)
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();

        return mesh;
    }
    
    //private RenderTexture GenerateAlbedo(HeightMap heightMap)
    //{
    //    int resolution = terrainSettings.textureSettings.resolution;

    //    var rt = new RenderTexture(resolution, resolution, 1)
    //    {
    //        enableRandomWrite = true,
    //        format = RenderTextureFormat.ARGBFloat,
    //    };

    //    int albedoKernel = 0;

    //    albedoCompute.SetTexture(albedoKernel, "HeightMap", heightMap.texture);
    //    albedoCompute.SetInt("heightMapN", heightMap.baseN);
    //    albedoCompute.SetInt("heightMapSeam", heightMap.seam);

    //    albedoCompute.SetFloat("size", terrainSettings.chunkSize);

    //    albedoCompute.SetTexture(albedoKernel, "Texture", rt);
    //    albedoCompute.SetInt("resolution", resolution);
    //    albedoCompute.SetFloat("minHeight", terrainSettings.textureSettings.minHeight);
    //    albedoCompute.SetFloat("maxHeight", terrainSettings.textureSettings.maxHeight);

    //    albedoCompute.SetFloat("steepBlendStart", terrainSettings.textureSettings.steepBlendStart);
    //    albedoCompute.SetFloat("steepBlendEnd", terrainSettings.textureSettings.steepBlendEnd);

    //    int heightColorCount = terrainSettings.textureSettings.heightColors.Length;
    //    ComputeBuffer heightColorBuffer = new ComputeBuffer(heightColorCount, 6 * sizeof(float));
    //    heightColorBuffer.SetData(terrainSettings.textureSettings.heightColors);
    //    albedoCompute.SetBuffer(albedoKernel, "HeightColors", heightColorBuffer);
    //    albedoCompute.SetInt("heightColorCount", heightColorCount);

    //    int steepColorCount = terrainSettings.textureSettings.steepColors.Length;
    //    ComputeBuffer steepColorBuffer = new ComputeBuffer(steepColorCount, 6 * sizeof(float));
    //    steepColorBuffer.SetData(terrainSettings.textureSettings.steepColors);
    //    albedoCompute.SetBuffer(albedoKernel, "SteepColors", steepColorBuffer);
    //    albedoCompute.SetInt("steepColorCount", steepColorCount);

    //    ComputeHelper.Dispatch(albedoCompute, resolution, resolution);

    //    heightColorBuffer.Dispose();
    //    steepColorBuffer.Dispose();

    //    return rt;
    //}

    private void DestroyOldTerrains()
    {
        string name = "Terrain";

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;

            if (child.name.Contains(name))
            {
                if (Application.isPlaying)
                {
                    for (int j = 0; j < child.transform.childCount; j++)
                    {
                        Destroy(child.transform.GetChild(j).gameObject);
                    }

                    Destroy(child);
                }
                else
                {
                    for (int j = 0; j < child.transform.childCount; j++)
                    {
                        DestroyImmediate(child.transform.GetChild(j).gameObject);
                    }

                    DestroyImmediate(child);
                }
            }
        }
    }

    private GameObject Generate(Vector2Int chunk, string id)
    {
        string name = "Terrain_" + id;

        GameObject terrain = new GameObject(name);
        terrain.transform.parent = transform;

        var group = terrain.AddComponent<LODGroup>();

        LOD[] lods = new LOD[terrainSettings.numberOfLODS];

        var heightMap = GenerateHeightMap(terrainSettings, chunk);
        
        //var mat = new Material(terrainMaterial);
        
        //var albedo = GenerateAlbedo(heightMap);
        //mat.SetTexture("_MainTex", albedo);

        for (int lod = 0; lod < terrainSettings.numberOfLODS; lod++)
        {
            GameObject go = new GameObject(name + "_LOD" + lod);
            go.transform.parent = terrain.transform;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = GenerateMesh(terrainSettings, heightMap, lod);

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = terrainMaterial;

            float frac = Mathf.Exp(-lod);

            lods[lod] = new LOD(frac, new Renderer[] { mr });
        }

        group.SetLODs(lods);
        group.RecalculateBounds();

        heightMap.texture.Release();

        terrain.transform.position = terrainSettings.chunkSize * new Vector3(chunk.x, 0, chunk.y);

        return terrain;
    }

    private void UpdateChunks()
    {
        if (terrainSettings.loadDistance > terrainSettings.unloadDistance)
            terrainSettings.unloadDistance = terrainSettings.loadDistance;

        Vector2 worldCenter = new Vector2(terrainSettings.worldCenter.position.x, terrainSettings.worldCenter.position.z);

        Vector2 minPos = worldCenter - terrainSettings.unloadDistance * Vector2.one;
        Vector2 maxPos = worldCenter + terrainSettings.unloadDistance * Vector2.one;

        Vector2Int min = Vector2Int.FloorToInt(minPos / terrainSettings.chunkSize);
        Vector2Int max = Vector2Int.CeilToInt(maxPos / terrainSettings.chunkSize);

        for (int y = min.y; y < max.y; y++)
        {
            for (int x = min.x; x < max.x; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                float sqrDistance = (terrainSettings.chunkSize * (Vector2)position - worldCenter).sqrMagnitude;

                if (sqrDistance < terrainSettings.loadDistance * terrainSettings.loadDistance)
                {
                    if (chunks.ContainsKey(position))
                    {
                        chunks[position].go.SetActive(true);
                    }
                    else
                    {
                        GameObject go = Generate(position, $"({position.x},{position.y})");

                        Chunk chunk = new Chunk
                        {
                            position = position,
                            go = go,
                        };

                        chunks.Add(position, chunk);
                    }
                }
                else if (sqrDistance > terrainSettings.unloadDistance * terrainSettings.unloadDistance)
                {
                    if (chunks.ContainsKey(position))
                    {
                        chunks[position].go.SetActive(false);
                    }
                }
            }
        }
    }

    private void Start()
    {
        DestroyOldTerrains();
    }

    private void Update()
    {
        UpdateChunks();
    }

    public void GenerateEditorTerrain()
    {
        if (!Application.isPlaying)
        {
            DestroyOldTerrains();
            Generate(Vector2Int.zero, "Editor");
        }
    }

    private void OnDrawGizmos()
    {
        if (terrainSettings.displayBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(terrainSettings.worldCenter.position, terrainSettings.loadDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(terrainSettings.worldCenter.position, terrainSettings.unloadDistance);
        }
    }
}
