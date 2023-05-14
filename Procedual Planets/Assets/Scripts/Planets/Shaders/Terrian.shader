Shader "Custom/Terrian"
{
    Properties
    {
        _ShollowRockColor("Shollow Rock Color", Color) = (1,1,1,1)
        _DeepRockColor("Deep Rock Color", Color) = (1,1,1,1)
        _LightGrassColor("Light Grass Color", Color) = (1,1,1,1)
        _DarkGrassColor("Dark Grass Color", Color) = (1,1,1,1)
        _ShollowSandColor("Shollow Sand Color", Color) = (1,1,1,1)
        _DeepSandColor("Deep Sand Color", Color) = (1,1,1,1)
        _Weight("Weight", float) = 0.0

        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        sampler3D planetMap;
        float3 planetCenter;
        float radius;
        float scale;

        fixed4 _ShollowRockColor;
        fixed4 _DeepRockColor;
        fixed4 _LightGrassColor;
        fixed4 _DarkGrassColor;
        fixed4 _ShollowSandColor;
        fixed4 _DeepSandColor;
        float _Weight;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float3 worldToTexPos(float3 worldPos) {
            return (worldPos / (2.0 * 2.0 * radius)) + 0.5;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float3 texCoord = worldToTexPos((IN.worldPos - planetCenter) / scale);

            float density = tex3D(planetMap, texCoord);

            float threshold = 0.003;
            if (density < threshold) {
                float depth = saturate(abs(density + threshold) * 20);
                o.Albedo = lerp(_ShollowRockColor, _DeepRockColor, depth);
            }
            else if (dot(IN.worldPos / scale, IN.worldPos / scale) < 0.58) {
                float depth = saturate(abs(density + threshold) * 20);
                o.Albedo = lerp(_ShollowSandColor, _DeepSandColor, depth);
            }
            else if (density > threshold) {
                float depth = saturate(abs(density + threshold) * 20);
                o.Albedo = lerp(_LightGrassColor, _DarkGrassColor, depth);
            }

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
