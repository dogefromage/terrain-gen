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
        float3 _base_colors[8];
        float _base_heights[8];
        float _base_blends[8];
        int _steep_N;
        float3 _steep_colors[8];
        float _steep_heights[8];
        float _steep_blends[8];
        float _minHeight;
        float _maxHeight;
        float _steepBlendStart;
        float _steepBlendEnd;

        struct Input
        {
            float3 worldNormal;
            float3 worldPos;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        float inverseLerp(float a, float b, float t)
        {
            return saturate((t - a) / (b - a));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 base_final = float3(0, 0, 0);
            
            float normHeight = inverseLerp(_minHeight, _maxHeight, IN.worldPos.y);

            for (int i = 0; i < _base_N; i++)
            {
                float blend = saturate(_base_blends[i] * (normHeight - _base_heights[i]));

                base_final = (1 - blend) * base_final + blend * _base_colors[i];
            }

            o.Albedo = base_final;

            //o.Albedo = float3(normHeight, normHeight, normHeight);

            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
