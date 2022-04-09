using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HeightColor
{
    public Color color;
    public float height;
    public float blend;
}

[System.Serializable]
public class TerrainMaterialSettings
{
    public float minHeight = -10;
    public float maxHeight = 10;

    public float steepBlendThreshold = 0.707f;
    public float steepBlendBlend = 0.05f;

    public HeightColor[] baseColors;
    //public HeightColor[] steepColors;
    public Color steepColor;
}

[ExecuteInEditMode]
public class TerrainMaterialProgrammer : MonoBehaviour
{
    public TerrainMaterialSettings settings;

    public Material terrainMaterial;

    public void Program()
    {
        if (terrainMaterial == null || settings == null) return;

        int base_N = Mathf.Min(8, settings.baseColors.Length);
        Color[] base_colors = new Color[base_N];
        float[] base_heights = new float[base_N];
        float[] base_blends = new float[base_N];

        for (int i = 0; i < base_N; i++)
        {
            HeightColor hc = settings.baseColors[i];
            base_colors[i] = hc.color;
            base_heights[i] = hc.height;
            base_blends[i] = hc.blend;
        }

        terrainMaterial.SetInt("_base_N", base_N);
        terrainMaterial.SetColorArray("_base_colors", base_colors);
        terrainMaterial.SetFloatArray("_base_heights", base_heights);
        terrainMaterial.SetFloatArray("_base_blends", base_blends);

        //int steep_N = Mathf.Min(8, settings.steepColors.Length);
        //Color[] steep_colors = new Color[steep_N];
        //float[] steep_heights = new float[steep_N];
        //float[] steep_blends = new float[steep_N];

        //for (int i = 0; i < steep_N; i++)
        //{
        //    HeightColor hc = settings.baseColors[i];
        //    steep_colors[i] = hc.color;
        //    steep_heights[i] = hc.height;
        //    steep_blends[i] = hc.blend;
        //}

        //terrainMaterial.SetInt("_steep_N", steep_N);
        //terrainMaterial.SetColorArray("_steep_colors", steep_colors);
        //terrainMaterial.SetFloatArray("_steep_heights", steep_heights);
        //terrainMaterial.SetFloatArray("_steep_blends", steep_blends);

        terrainMaterial.SetColor("_steep_color", settings.steepColor);

        terrainMaterial.SetFloat("_minHeight", settings.minHeight);
        terrainMaterial.SetFloat("_maxHeight", settings.maxHeight);

        terrainMaterial.SetFloat("_steepBlendThreshold", settings.steepBlendThreshold);
        terrainMaterial.SetFloat("_steepBlendBlend", settings.steepBlendBlend);
    }

    private void Awake()
    {
        Program();
    }
}
