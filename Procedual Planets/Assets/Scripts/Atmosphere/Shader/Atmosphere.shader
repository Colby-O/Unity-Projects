Shader "Hidden/Atmosphere_p"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            // Atmosphere Parameters
            static const float maxFloat = 3.402823466e+38F;
            float3 planetCenter;
            float3 dirToSun;
            float3 scatteringCoefficients;
            float atmosphereRadius;
            float oceanRadius;
            float planetRadius;
            float fallOffFactor;
            int numOpticalDepthPoints;
            int numInScatterPoints;
            float intensity;
            float ditherStrength;

            // Cloud Parameters
            Texture3D<float> DisplayTexture;
            SamplerState samplerDisplayTexture;
            float innerCloudRadius;
            float outerCloudRadius;
            int numCloudSamplePoints;
            float cloudScale;
            float cloudOffset;
            float lightAbsorptionTowardsSunFactor;
            float lightAbsorptionThroughCloudFactor;
            float lightEnergyFactor;
            float darknessThreshold;
            float cloudThreshold;
            float cloudSpeed;
            float outlookingAtmosphereStrength;
            float inlookingAtmosphereStrength;
            float atmosphereStrengthOnSurface;
            float4 cloudColor;
            bool atmosphereEnabled;
            bool cloudsEnabled;
            float planetUnscaledRadius;

            bool isInsideSphere(float3 center, float radius, float3 rayOrigin, float3 rayDir) {
                // Check if the ray's origin lies within the sphere with the following
                // sqrt((O.x - C.x)^2 + (O.y - C.y)^2 + (O.z - C.z)^2) <= R (lies within or on sphere)
                return sqrt(pow(rayOrigin.x - center.x, 2.0f) + pow(rayOrigin.y - center.y, 2.0f) + pow(rayOrigin.z - center.z, 2.0f)) <= radius;
            }

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
                        return float2(distToSphereNear, distToSphereFar - distToSphereNear);
                    }

                }

                return float2(maxFloat, 0);
            }

            float3 worldToTexPos(float3 worldPos) {
                return (worldPos / (2.0 * 2.0 * planetUnscaledRadius)) + 0.5; // planetUnscaledRadius or planetRadius
            }

            float sampleCloudDensity(float3 pt) {
                float3 planetPt = pt - planetCenter;
                float3 pos = planetPt * cloudScale * 0.001 + cloudOffset + _Time * cloudSpeed;
                float3 uvw = worldToTexPos(pos);
                float density = DisplayTexture.SampleLevel(samplerDisplayTexture, uvw, 0);
                return density;
            }

            float calculateCloudLight(float3 pos) {
                //float3 dirToLight = _WorldSpaceLightPos0.xyz;

                float2 hitOutter = raySphereIntersect(planetCenter, outerCloudRadius, pos, normalize(dirToSun));
                float2 hitInner = raySphereIntersect(planetCenter, innerCloudRadius, pos, normalize(dirToSun));

                float dist;

                if (hitInner.x != maxFloat && hitOutter.x != maxFloat) {
                    dist = hitInner.x - hitOutter.x;
                }
                else {
                    dist = hitOutter.y;
                }

                float ds = dist / numCloudSamplePoints;
                float totalDensity = 0;

                for (int i = 0; i < numCloudSamplePoints; ++i) {
                    pos += normalize(dirToSun) * ds;
                    totalDensity += max(0, sampleCloudDensity(pos) * ds);
                }

                float transmittance = exp(-totalDensity * lightAbsorptionTowardsSunFactor);

                return darknessThreshold + transmittance * (1 - darknessThreshold);
            }

            float3 calculateCloudLightEnergy(float enterDist, float distToTravel, float3 rayOrigin, float3 rayDir, float startingCloudDensity) {
                float distTraveled = 0;
                float lightEnergy = 0;
                float cloudDensity = startingCloudDensity;
                float transmittance = 1;
                float ds = distToTravel / numCloudSamplePoints;

                while (distTraveled < distToTravel) {
                    float3 pos = rayOrigin + (enterDist + distTraveled) * rayDir;
                    float3 tex = worldToTexPos(pos);
                    cloudDensity += sampleCloudDensity(pos);
                    if (cloudDensity > cloudThreshold) {
                        float lightTransmittance = calculateCloudLight(pos);
                        lightEnergy += cloudDensity * ds * transmittance * lightTransmittance * lightEnergyFactor;
                        transmittance *= exp(-cloudDensity * ds * lightAbsorptionThroughCloudFactor);

                        if (transmittance < 0.01) {
                            break;
                        }
                    }
                    distTraveled += ds;
                }

                return float3(lightEnergy, transmittance, cloudDensity);
            }

            float sampleAtmosphereDensity(float3 pt) {
                float height = length(pt - planetCenter) - planetRadius;
                float scaledHeight = height / (atmosphereRadius - planetRadius);
                float density = exp(-scaledHeight * fallOffFactor) * (1 - scaledHeight);
                return density;
            }

            float getOpticalDepth(float3 rayOrigin, float3 rayDir, float rayLength) {
                float3 samplePoint = rayOrigin;
                float ds = rayLength / (numOpticalDepthPoints - 1);
                float opticalDepth = 0;

                for (int i = 0; i < numOpticalDepthPoints; ++i) {
                    opticalDepth += sampleAtmosphereDensity(samplePoint) * ds;
                    samplePoint += rayDir * ds;
                }

                return opticalDepth;
            }

            float3 calculateAtmosphereLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalColor, float distToSurface) {
                float3 inScatterPoint = rayOrigin;
                float ds = rayLength / (numInScatterPoints - 1);
                float3 inScatterLight = float3(0, 0, 0);
                float viewerOpticalDepth = 0;

                for (int i = 0; i < numInScatterPoints; ++i) {
                    float2 hit = raySphereIntersect(planetCenter, atmosphereRadius, inScatterPoint, normalize(dirToSun));
                    float sunRayLength = hit.y;
                    // TODO: Pre-bake opticalDepth to save resources
                    float opticalDepth = getOpticalDepth(inScatterPoint, normalize(dirToSun), sunRayLength);
                    viewerOpticalDepth = getOpticalDepth(inScatterPoint, -rayDir, ds * i);
                    float3 transmittance = exp(-(opticalDepth + viewerOpticalDepth) * scatteringCoefficients);
                    float density = sampleAtmosphereDensity(inScatterPoint);
                    inScatterLight += density * transmittance * scatteringCoefficients * ds * intensity;
                    inScatterPoint += rayDir * ds;
                }

                // TODO: Find A Better Method To Attenuate Brightness of Original Color
                // Method below is crakced
                float atmosphereStrength = (isInsideSphere(planetCenter, atmosphereRadius, _WorldSpaceCameraPos, rayDir)) ? outlookingAtmosphereStrength : inlookingAtmosphereStrength;
                float originalColorTransmittance = saturate(distToSurface / atmosphereStrength);//exp(-viewerOpticalDepth);

                float3 finalCol = (originalColor + inScatterLight * atmosphereStrengthOnSurface) * (1 - originalColorTransmittance) + originalColorTransmittance * inScatterLight;

                return finalCol;//originalColor* originalColorTransmittance + inScatterLight;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float distToTerrain = LinearEyeDepth(depth) * length(i.viewVector);

                float3 rayDir = normalize(i.viewVector);
                float3 rayOrigin = _WorldSpaceCameraPos;

                float distToOcean = raySphereIntersect(planetCenter, oceanRadius, rayOrigin, rayDir).x;
                float distToSurface = min(distToTerrain, distToOcean);

                // Clouds
                float2 hitOutter = raySphereIntersect(planetCenter, outerCloudRadius, rayOrigin, rayDir);
                float2 hitInner = raySphereIntersect(planetCenter, innerCloudRadius, rayOrigin, rayDir);

                if (isInsideSphere(planetCenter, innerCloudRadius, rayOrigin, rayDir)) {
                    float2 tempO = hitOutter.x;
                    float2 tempI = hitInner.x;
                    hitOutter.x = hitOutter.y + tempO;
                    hitInner.x = hitInner.y + tempI;
                }

                bool hasHit = hitOutter.x != maxFloat && abs(hitOutter.x) < distToSurface; //&& hitOutter.x < hitInner.x;

                float lightEnergy = 0;
                float transmittance = 1;

                if (hasHit && cloudsEnabled) {
                    float insideFactor = (isInsideSphere(planetCenter, innerCloudRadius, rayOrigin, rayDir)) ? -1 : 1;
                    bool isInOuterSphere = isInsideSphere(planetCenter, outerCloudRadius, rayOrigin, rayDir);
                    float distToTravel;
                    if (abs(hitOutter.y + hitOutter.x) > distToSurface) {
                        if (hitInner.x != maxFloat && abs(hitInner.x) < distToSurface) {
                            distToTravel = insideFactor * (hitInner.x - hitOutter.x);
                        }
                        else {
                            distToTravel = hitOutter.y;
                        }

                        float3 cloudInfo = calculateCloudLightEnergy(hitOutter.x, distToTravel, rayOrigin, rayDir, 0);
                        lightEnergy += cloudInfo.x;
                        transmittance *= cloudInfo.y;
                    }
                    else {
                        if (hitInner.x != maxFloat && abs(hitInner.y + hitInner.x) < distToSurface && !isInsideSphere(planetCenter, innerCloudRadius, rayOrigin, rayDir)) {
                            distToTravel = insideFactor * (hitInner.x - hitOutter.x);

                            float3 cloudInfoNearLayer = calculateCloudLightEnergy((isInOuterSphere) ? 0 : hitOutter.x, distToTravel, rayOrigin, rayDir, 0);
                            //lightEnergy += cloudInfoNearLayer.x;
                            //transmittance *= cloudInfoNearLayer.y;

                            distToTravel = insideFactor * (hitOutter.y + hitOutter.x - hitInner.y - hitInner.x);

                            float3 cloudInfoFarLayer = calculateCloudLightEnergy(hitOutter.y + hitOutter.x, distToTravel, rayOrigin, rayDir, 0);

                            // Sus? Surely Therew Is A Better Method To Fade Out Clouds In The Back Of The Planet
                            float farLayerFade = hitOutter.x / (hitOutter.y + hitOutter.x);
                            float insideOuterSphere = isInsideSphere(planetCenter, outerCloudRadius, rayOrigin, rayDir) ? 1 : 1;

                            lightEnergy += cloudInfoNearLayer.x;
                            transmittance *= cloudInfoNearLayer.y;

                            // TODO: FIx Dark outline in front cloud over back clouds!!!
                            lightEnergy +=  lerp(0, cloudInfoFarLayer.x, exp(-lightEnergy * 10));
                            transmittance *= lerp(1, cloudInfoFarLayer.y, exp(-lightEnergy * 0.01));

                        }
                        else if (hitInner.x != maxFloat && abs(hitInner.x) < distToSurface) {
                            distToTravel = insideFactor * (hitInner.x - hitOutter.x);
                            float3 cloudInfo = calculateCloudLightEnergy(hitOutter.x, distToTravel, rayOrigin, rayDir, 0);
                            lightEnergy += cloudInfo.x;
                            transmittance *= cloudInfo.y;
                        }
                        else {
                            distToTravel = hitOutter.y + hitOutter.x;
                            float3 cloudInfo = calculateCloudLightEnergy((isInOuterSphere) ? 0 : hitOutter.x, distToTravel, rayOrigin, rayDir, 0);
                            lightEnergy += cloudInfo.x;
                            transmittance *= cloudInfo.y;
                        }
                    }
                }

                // Atmosphere
                float2 atmosphereHit = raySphereIntersect(planetCenter, atmosphereRadius, rayOrigin, rayDir);
                float distToAtomsphere = atmosphereHit.x;
                float distThroughAtomsphere = min(atmosphereHit.y, distToSurface - distToAtomsphere);

                if (distThroughAtomsphere > 0 && atmosphereEnabled) {
                    float3 fristPointInAtmosphere = rayOrigin + rayDir * distToAtomsphere;
                    float3 AtmosphereLight = calculateAtmosphereLight(fristPointInAtmosphere, rayDir, distThroughAtomsphere, col.xyz, distToSurface);
                    float3 AtmosphereAndCloudLight = AtmosphereLight * transmittance + (lightEnergy * cloudColor);
                    return float4(AtmosphereAndCloudLight, 1);
                }

                return  col * transmittance + (lightEnergy * cloudColor);
            }
            ENDCG
        }
    }
}