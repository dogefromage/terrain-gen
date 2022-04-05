Shader "Custom/TerrainShader"
{
    Properties
    {
        //_Color ("Color", Color) = (1,1,1,1)
        //_MainTex ("Albedo (RGB)", 2D) = "white" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        int _base_N;
        float _base_colors[8];
        float _base_heights;
        float _base_blends;
        int _steep_N;
        float _steep_colors[8];
        float _steep_heights;
        float _steep_blends;
        float _minHeight;
        float _maxHeight;
        float _steepBlendStart;
        float _steepBlendEnd;

        struct Input
        {
            float3 worldNormal;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 baseColor = float4(0, 0, 0, 1);

            for (int i = 0; i < heightColorCount; i++)
            {
                HeightColor hc = HeightColors[i];
                float normHeight = inverseLerp(minHeight, maxHeight, rawHeight);
                float blend = saturate(hc.blend * (normHeight - hc.height));

                baseColor = (1 - blend) * baseColor + blend * hc.color;
            }

            o.Albedo = baseColor;

            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
