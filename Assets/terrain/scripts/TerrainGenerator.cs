using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
public class TerrainSettings
{
    public int seed = 0;

    public float chunkSize = 10f;
    public int chunkResolution = 64;

    public NoiseSettings baseHeight;
    public NoiseSettings ridgeHeight;

    public float ridgeOffset = 0.5f;

    public bool displayRidge = false;
}

public class TerrainGenerator : MonoBehaviour
{
    public TerrainSettings terrainSettings;

    public bool autoUpdate;

    public ComputeShader meshGridCompute;
    public ComputeShader heightMapCompute;

    public Shader terrainShader;

    //// https://github.com/SebLague/Terraforming/blob/main/Assets/Marching%20Cubes/Scripts/GenTest.cs
    //void Create3DTexture(RenderTexture texture, int size, string name)
    //{
    //    var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;

    //    if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format)
    //    {
    //        //Debug.Log ("Create tex: update noise: " + updateNoise);
    //        if (texture != null)
    //        {
    //            texture.Release();
    //        }
    //        const int numBitsInDepthBuffer = 0;
    //        texture = new RenderTexture(size, size, numBitsInDepthBuffer);
    //        texture.graphicsFormat = format;
    //        texture.volumeDepth = size;
    //        texture.enableRandomWrite = true;
    //        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;


    //        texture.Create();
    //    }
    //    texture.wrapMode = TextureWrapMode.Repeat;
    //    texture.filterMode = FilterMode.Bilinear;
    //    texture.name = name;
    //}

    public RenderTexture GenerateHeightMap(TerrainSettings settings)
    {
        var heightMap = new RenderTexture(settings.chunkResolution, settings.chunkResolution, 1)
        {
            enableRandomWrite = true,
            format = RenderTextureFormat.RFloat,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
        };

        int heightMapKernel = 0;

        heightMapCompute.SetInt("seed", settings.seed);
        heightMapCompute.SetInt("N", settings.chunkResolution);
        heightMapCompute.SetFloat("size", settings.chunkSize);

        heightMapCompute.SetTexture(heightMapKernel, "HeightMap", heightMap);

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

        ComputeHelper.Dispatch(heightMapCompute, settings.chunkResolution, settings.chunkResolution);

        return heightMap;
    }

    public Mesh GenerateMesh(TerrainSettings settings, RenderTexture heightMap)
    {
        int N = settings.chunkResolution;

        int vertBufferLength = N * N;
        int triBufferLength = 6 * (N - 1) * (N - 1);

        ComputeBuffer vertBuffer = new ComputeBuffer(vertBufferLength, 3 * sizeof(float));
        ComputeBuffer normalBuffer = new ComputeBuffer(vertBufferLength, 3 * sizeof(float));
        ComputeBuffer triBuffer = new ComputeBuffer(triBufferLength, sizeof(int));

        int meshGridKernel = 0;

        meshGridCompute.SetTexture(meshGridKernel, "HeightMap", heightMap);

        meshGridCompute.SetBuffer(meshGridKernel, "vertices", vertBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "normals", normalBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "triangles", triBuffer);

        meshGridCompute.SetInt("N", N);
        meshGridCompute.SetFloat("size", settings.chunkSize);

        ComputeHelper.Dispatch(meshGridCompute, N, N);

        var mesh = new Mesh();

        Vector3[] verts = new Vector3[vertBufferLength];
        Vector3[] normals = new Vector3[vertBufferLength];
        int[] tris = new int[triBufferLength];

        vertBuffer.GetData(verts, 0, 0, vertBufferLength);
        normalBuffer.GetData(normals, 0, 0, vertBufferLength);
        triBuffer.GetData(tris, 0, 0, triBufferLength);

        vertBuffer.Dispose();
        normalBuffer.Dispose();
        triBuffer.Dispose();

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = tris;

        mesh.RecalculateBounds();

        mesh.RecalculateNormals();

        return mesh;
    }

    public void DestroyOldTerrains()
    {
        string name = "terrain";

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;

            if (child.name.Contains(name))
            {
                DestroyImmediate(child);
            }
        }
    }

    public void Generate()
    {
        DestroyOldTerrains();

        GameObject terrain = new GameObject("Editor terrain");
        terrain.transform.parent = transform;

        var heightMap = GenerateHeightMap(terrainSettings);

        var mf = terrain.AddComponent<MeshFilter>();
        mf.mesh = GenerateMesh(terrainSettings, heightMap);

        var mr = terrain.AddComponent<MeshRenderer>();

        var terrainMaterial = new Material(terrainShader);

        terrainMaterial.SetTexture("HeightMap", heightMap);
        
        
        mr.material = terrainMaterial;

        heightMap.Release();
    }

    void Start()
    {
        DestroyOldTerrains();
    }

    private void Update()
    {
        
    }
}
