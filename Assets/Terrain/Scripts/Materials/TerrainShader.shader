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
        
        //int _steep_N;
        //float3 _steep_colors[8];
        //float _steep_heights[8];
        //float _steep_blends[8];

        float3 _steep_color;
        
        float _minHeight;
        float _maxHeight;

        float _steepBlendThreshold;
        float _steepBlendBlend;

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

        float smoothSign(float x, float blend)
        {
            if (blend < 0.0000001) return saturate(sign(x));

            if (x < -blend) return 0;

            if (x > blend) return 1;

            float a = x / blend;
            float b = a + 1;

            return 0.25 * (a + 1) * (a + 1) * (2 - a);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float normHeight = inverseLerp(_minHeight, _maxHeight, IN.worldPos.y);
            
            if (_base_N <= 0) return;
            
            float3 base_final = _base_colors[0];
            
            for (int i = 1; i < _base_N; i++)
            {
                float diff = normHeight - _base_heights[i];
                
                float t = smoothSign(diff, _base_blends[i]);

                base_final = (1 - t) * base_final + t * _base_colors[i];
            }

            o.Albedo = base_final;

            //if (_steep_N <= 0) return;

            //float steep_final = _steep_colors[0];
         
            //o.Albedo = steep_final;

            //return;

            //for (int i = 1; i < _steep_N; i++)
            //{
            //    float diff = normHeight - _steep_heights[i];
                
            //    float t = smoothSign(diff, _steep_blends[i]);

            //    steep_final = (1 - t) * steep_final + t * _steep_colors[i];
            //}

            float gradient = dot(float3(0, 1, 0), IN.worldNormal);

            float steepBlend = smoothSign(_steepBlendThreshold - gradient, _steepBlendBlend);

            o.Albedo = (1 - steepBlend) * base_final + steepBlend * _steep_color;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
