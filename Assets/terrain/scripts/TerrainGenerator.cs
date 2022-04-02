using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSettings
{
    public float size;
    public int resolution;

    public TerrainSettings(float size, int resolution)
    {
        this.size = size;
        this.resolution = resolution;
    }
}

public class TerrainGenerator : MonoBehaviour
{
    public TerrainSettings terrainSettings = new TerrainSettings(10f, 64);
    
    public Material terrainMaterial;

    public ComputeShader meshGridCompute;
    public ComputeShader heightMapCompute;
    
    [HideInInspector] public RenderTexture heightMap;

    private GameObject terrain;

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
        if (heightMap) heightMap.Release();

        heightMap = new RenderTexture(settings.resolution, settings.resolution, 1)
        {
            enableRandomWrite = true,
        };

        int heightMapKernel = 0;

        heightMapCompute.SetTexture(heightMapKernel, "HeightMap", heightMap);

        ComputeHelper.Dispatch(heightMapCompute, settings.resolution, settings.resolution);

        return heightMap;
    }

    public Mesh GenerateMesh(TerrainSettings settings, RenderTexture heightMap)
    {
        int N = settings.resolution;

        int vertBufferLength = N * N;
        int triBufferLength = 6 * (N - 1) * (N - 1);

        ComputeBuffer vertBuffer = new ComputeBuffer(vertBufferLength, 3 * sizeof(float));
        ComputeBuffer triBuffer = new ComputeBuffer(triBufferLength, sizeof(int));

        int meshGridKernel = 0;

        meshGridCompute.SetBuffer(meshGridKernel, "verts", vertBuffer);
        meshGridCompute.SetBuffer(meshGridKernel, "tris", triBuffer);
        
        meshGridCompute.SetInt("N", N);
        meshGridCompute.SetFloat("size", settings.size);

        ComputeHelper.Dispatch(meshGridCompute, N, N);

        var mesh = new Mesh();

        vertBuffer.GetData(mesh.vertices, 0, 0, vertBufferLength);
        triBuffer.GetData(mesh.triangles, 0, 0, triBufferLength);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public void Generate()
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

        if (terrain != null)
        {
            DestroyImmediate(terrain);
        }

        terrain = new GameObject(name);

        var heightMap = GenerateHeightMap(terrainSettings);

        var mf = terrain.AddComponent<MeshFilter>();
        mf.mesh = GenerateMesh(terrainSettings, heightMap);

        var mr = terrain.AddComponent<MeshRenderer>();
        mr.material = terrainMaterial;

        terrain.transform.parent = transform;
    }

    void Start()
    {
        Generate();
    }
}
