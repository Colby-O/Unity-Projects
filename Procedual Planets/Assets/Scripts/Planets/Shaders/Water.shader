Shader "Unlit/Water"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 screenPos : TEXCOORD1;
                float3 viewVector : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float3 worldNormal : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            uniform sampler2D _CameraDepthTexture;
            sampler2D wavePhysicsMap;
            sampler2D waveNormalA;
            sampler2D waveNormalB;
            sampler3D planetMap;
            float4 depthColor;
            float4 shallowColor;
            float4 specularColor;
            float3 dirToSun;
            float normalFactor;
            float depthFactor;
            float fresnelFactor;
            float shoreFadeFactor;
            float smoothnessFactor;
            float ks;
            float kd;
            float waveSpeed;
            float waveHeightMod;
            float waveNormalScale;
            float radius;
            float scale;

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

            v2f vert (appdata_base v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);

                float waveHeight = tex2Dlod(wavePhysicsMap, float4(v.texcoord.xy, 0, 0)).x;

                v.vertex = v.vertex + float4(worldNormal, 0) * waveHeight * waveHeightMod;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.screenPos = ComputeScreenPos(o.vertex);

                float3 viewVector = mul(unity_CameraInvProjection, float4((o.screenPos.xy / o.screenPos.w) * 2 - 1, 0, -1));

                o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                o.worldPos = worldPos;
                o.worldNormal = worldNormal;
                o.normal = v.normal;
                return o;
            }

            float calcSpecularHighlights(float3 viewDir, float3 dirToSun, float3 normal, float smoothnessFactor) {
                float3 halfVector = normalize(dirToSun - viewDir);
                float specularExponent = acos(dot(halfVector, normal));
                specularExponent /= smoothnessFactor;
                return exp(-specularExponent * specularExponent);
            }

            float3 worldToTexPos(float3 worldPos) {
                return (worldPos /(2.0 * 2.0 * radius)) + 0.5;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewVector);

                // Get Water Depth
                float depth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos);
                float distToTerrain = LinearEyeDepth(depth);
                float distToWater = i.screenPos.w;
                float waterDepth = distToTerrain - distToWater;

                // Determine depth color
                float decayfactor = 1 - exp(-waterDepth * depthFactor);
                float3 col = lerp(shallowColor.xyz, depthColor.xyz, decayfactor);

                // Fades shore line
                float fresnel = 1 - min(0.2, pow(saturate(dot(-viewDir, i.worldNormal)), fresnelFactor));
                float shoreFade = 1 - exp(-waterDepth * shoreFadeFactor);
                float alpha = lerp(fresnel, 1, shoreFade);

                float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
                float2 waveOffsetB = float2(_Time.x * waveSpeed * -0.8, _Time.x * waveSpeed * -0.3);
                float3 waveNormal1 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetA, waveNormalA);
                float3 waveNormal2 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetB, waveNormalB);
                float3 waveNormal = triplanarNormal(i.worldPos, waveNormal1, waveNormalScale, waveOffsetB, waveNormalB);
                float3 specWaveNormal = normalize(lerp(i.worldPos, waveNormal, normalFactor));

                float specularTerm = calcSpecularHighlights(viewDir, dirToSun, specWaveNormal, smoothnessFactor);

                col = kd * col + ks * specularTerm * specularColor;

                float val = tex3D(planetMap, worldToTexPos(i.worldPos / scale));
                float waveHeight = tex2D(wavePhysicsMap, float4(i.uv, 0, 0)).x;
                return float4(col.xyz, alpha); //val * float4(1, 1, 1, 0) + float4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}
