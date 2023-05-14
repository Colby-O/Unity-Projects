Shader "Hidden/Clouds"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float3 viewVector : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                return o;
            }

            static const float maxFloat = pow(2,32) - 1;
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            Texture3D<float> DisplayTexture;
            SamplerState samplerDisplayTexture;

            float3 planetCenter;
            float atmosphereRadius;
            float oceanRadius;
            float innerCloudRadius;
            float outerCloudRadius;
            float planetRadius;
            int numSamplePoints;
            float cloudScale;
            float cloudOffset;

            float lightAbsorptionTowardsSunFactor;
            float lightAbsorptionThroughCloudFactor;
            float lightEnergyFactor;
            float darknessThreshold;
            float cloudThreshold;
            float cloudSpeed;
            float4 cloudColor;

            float2 raySphereIntersect(float3 center, float radius, float3 rayOrigin, float3 rayDir) {
                float3 diff = rayOrigin - center;
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(rayDir, diff);
                float c = dot(diff, diff) - radius * radius;
                float discriminant = b * b - 4.0 * a * c;
                float epsilon = 0.0001;

                if (discriminant > epsilon) {
                    float distToSphereFar = (-b + sqrt(discriminant)) / (2.0 * a);
                    float distToSphereNear = (-b - sqrt(discriminant)) / (2.0 * a);

                    if (distToSphereFar >= epsilon) {
                        return float2(distToSphereNear, distToSphereFar);
                    }
                }

                return float2(maxFloat, maxFloat);
            }

            float3 worldToTexPos(float3 worldPos) {
                return (worldPos / (2.0 * 2.0 * planetRadius)) + 0.5;
            }

            float sampleDensity(float3 pt) {
                float3 pos = pt * cloudScale * 0.001 + (cloudOffset + _Time) * cloudSpeed;
                float3 uvw = worldToTexPos(pos);
                float density = DisplayTexture.SampleLevel(samplerDisplayTexture, uvw, 0);
                return density;
            }

            float calculateLight(float3 pos) {
                float3 dirToLight = _WorldSpaceLightPos0.xyz;

                float2 hitOutter = raySphereIntersect(planetCenter, outerCloudRadius, pos, 1 / dirToLight);
                float2 hitInner = raySphereIntersect(planetCenter, innerCloudRadius, pos, 1 / dirToLight);

                float dist;

                if (hitInner.x != maxFloat && hitInner.y != maxFloat) {
                    dist = hitInner.x - hitOutter.x;
                }
                else {
                    dist = hitOutter.y - hitOutter.x;
                }

                float stepSize = dist / numSamplePoints;
                float totalDensity = 0;

                for (int i = 0; i < numSamplePoints; ++i) {
                    pos += dirToLight * stepSize;
                    totalDensity += max(0, sampleDensity(pos) * stepSize);
                }

                float transmittance = exp(-totalDensity * lightAbsorptionTowardsSunFactor);

                return darknessThreshold + transmittance * (1 - darknessThreshold);
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float distToTerrain = LinearEyeDepth(depth) * length(i.viewVector);
  
                float3 rayDir = normalize(i.viewVector);
                float3 rayOrigin =  _WorldSpaceCameraPos;

                float distToOcean = raySphereIntersect(planetCenter, oceanRadius, rayOrigin, rayDir).x;

                float distToSurface = min(distToTerrain, distToOcean);

                float2 hitOutter = raySphereIntersect(planetCenter, outerCloudRadius, rayOrigin, rayDir);
                float2 hitInner = raySphereIntersect(planetCenter, innerCloudRadius, rayOrigin, rayDir);
                bool hasHit = hitOutter.x != maxFloat && hitOutter.y != maxFloat && abs(hitOutter.x) < distToSurface && hitOutter.x < hitInner.x;
                if (hasHit) {
                    float cloudDensity = 0;
                    float distTraveled = 0;
                    float lightEnergy = 0;
                    float transmittance = 1;
                    float stepSize;
                    float distLimit;
                    if (hitInner.x != maxFloat && hitInner.y != maxFloat && abs(hitInner.x) < distToSurface) {
                        stepSize = (hitInner.x - hitOutter.x) / numSamplePoints;
                        distLimit = (hitInner.x - hitOutter.x);
                    }
                    else {
                        stepSize = (hitOutter.y - hitOutter.x) / numSamplePoints;
                        distLimit = (hitOutter.y - hitOutter.x);
                    }
                    while (distTraveled < distLimit) {
                        float3 pos = rayOrigin + (hitOutter.x + distTraveled) * rayDir;
                        float3 tex = worldToTexPos(pos);
                        cloudDensity += sampleDensity(pos);
                        if (cloudDensity > cloudThreshold) {
                            float lightTransmittance = calculateLight(pos);
                            lightEnergy += cloudDensity * stepSize * transmittance * lightTransmittance * lightEnergyFactor;
                            transmittance *= exp(-cloudDensity * stepSize * lightAbsorptionThroughCloudFactor);

                            if (transmittance < 0.01) {
                                break;
                            }
                        }

                        distTraveled += stepSize;
                    }

                    return col * transmittance + (lightEnergy * cloudColor);
                }

                return col;
            }
            ENDCG
        }
    }
}
