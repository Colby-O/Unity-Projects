Shader "Custom/MoonTerrian"
{
    Properties
    {
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
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        sampler2D normalMap;
        float normalScale;
        float3 sunDir;
        float3 moonCenter;

        float3 blend_rnm(float3 n1, float3 n2)
        {
            n1.z += 1;
            n2.xy = -n2.xy;

            return n1 * dot(n1, n2) / n1.z - n2;
        }

        float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap) {
            float3 absNormal = abs(normal);

            float3 blendWeight = saturate(pow(normal, 4));
            // Divide blend weight by the sum of its components. This will make x + y + z = 1
            blendWeight /= dot(blendWeight, 1);

            // Calculate triplanar coordinates
            float2 uvX = vertPos.zy * scale + offset;
            float2 uvY = vertPos.xz * scale + offset;
            float2 uvZ = vertPos.xy * scale + offset;

            // Sample tangent space normal maps
            float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
            float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
            float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

            // Swizzle normals to match tangent space and apply reoriented normal mapping blend
            tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
            tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
            tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

            // Apply input normal sign to tangent space Z
            float3 axisSign = sign(normal);
            tangentNormalX.z *= axisSign.x;
            tangentNormalY.z *= axisSign.y;
            tangentNormalZ.z *= axisSign.z;

            // Swizzle tangent normals to match input normal and blend together
            float3 outputNormal = normalize(
                tangentNormalX.zyx * blendWeight.x +
                tangentNormalY.xzy * blendWeight.y +
                tangentNormalZ.xyz * blendWeight.z
            );

            return outputNormal;
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 col = tex2D(_MainTex, IN.uv_MainTex);
            float3 lightNormal = triplanarNormal(IN.worldPos - moonCenter, IN.worldNormal, normalScale, 0, normalMap);
            float lightShading = saturate(dot(lightNormal, _WorldSpaceLightPos0.xyz));

            o.Albedo = col.rgb * lightShading;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
