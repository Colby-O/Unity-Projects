using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Atmosphere : MonoBehaviour
{
    public Material atmosphereMaterial;
    public Material underwaterMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Vector3 wavelengths = FindObjectOfType<PlanetManger>().wavelengths;
        Vector3 scatteringCoefficients = new Vector3(Mathf.Pow(400.0f / wavelengths.x, 4.0f), Mathf.Pow(400.0f / wavelengths.y, 4.0f), Mathf.Pow(400.0f / wavelengths.z, 4.0f));
        scatteringCoefficients *= FindObjectOfType<PlanetManger>().scatteringStrength;

        atmosphereMaterial.SetInt("atmosphereEnabled", (FindObjectOfType<PlanetManger>().enableAtmosphere) ? 1 : 0);
        atmosphereMaterial.SetVector("scatteringCoefficients", scatteringCoefficients);
        atmosphereMaterial.SetVector("planetCenter", FindObjectOfType<PlanetManger>().transform.position);
        atmosphereMaterial.SetVector("dirToSun", FindObjectOfType<PlanetManger>().sunDir); //new Vector3(0, 13.0f, -273f)
        atmosphereMaterial.SetFloat("scatteringStrength", FindObjectOfType<PlanetManger>().scatteringStrength);
        atmosphereMaterial.SetFloat("atmosphereStrengthOnSurface", FindObjectOfType<PlanetManger>().atmosphereStrengthOnSurface);
        atmosphereMaterial.SetFloat("intensity", FindObjectOfType<PlanetManger>().intensity);
        atmosphereMaterial.SetFloat("planetUnscaledRadius", FindObjectOfType<PlanetManger>().radius);
        atmosphereMaterial.SetFloat("planetRadius", FindObjectOfType<PlanetManger>().radius * FindObjectOfType<PlanetManger>().transform.localScale.x * 0.5f);
        atmosphereMaterial.SetFloat("atmosphereRadius", FindObjectOfType<PlanetManger>().atmosphereRadius);
        atmosphereMaterial.SetFloat("oceanRadius", FindObjectOfType<PlanetManger>().radius * FindObjectOfType<PlanetManger>().transform.localScale.x * FindObjectOfType<PlanetManger>().oceanTransfrom.localScale.x * 0.5f);
        atmosphereMaterial.SetFloat("fallOffFactor", FindObjectOfType<PlanetManger>().fallOffFactor);
        atmosphereMaterial.SetFloat("outlookingAtmosphereStrength", FindObjectOfType<PlanetManger>().outlookingAtmosphereStrength * FindObjectOfType<PlanetManger>().transform.localScale.x);
        atmosphereMaterial.SetFloat("inlookingAtmosphereStrength", FindObjectOfType<PlanetManger>().inlookingAtmosphereStrength * FindObjectOfType<PlanetManger>().transform.localScale.x);
        atmosphereMaterial.SetInt("numOpticalDepthPoints", FindObjectOfType<PlanetManger>().numOpticalDepthPoints);
        atmosphereMaterial.SetInt("numInScatterPoints", FindObjectOfType<PlanetManger>().numInScatterPoints);

        // Cloud Shader Parameters
        atmosphereMaterial.SetInt("cloudsEnabled", (FindObjectOfType<PlanetManger>().enableClouds) ? 1 : 0);
        atmosphereMaterial.SetTexture("DisplayTexture", FindObjectOfType<CloudManger>().noiseMap);
        atmosphereMaterial.SetFloat("innerCloudRadius", FindObjectOfType<CloudManger>().innerCloudRadius);
        atmosphereMaterial.SetFloat("outerCloudRadius", FindObjectOfType<CloudManger>().outerCloudRadius);
        atmosphereMaterial.SetFloat("cloudOffset", FindObjectOfType<CloudManger>().cloudOffset);
        atmosphereMaterial.SetFloat("cloudScale", FindObjectOfType<CloudManger>().cloudScale);
        atmosphereMaterial.SetFloat("lightAbsorptionTowardsSunFactor", FindObjectOfType<CloudManger>().lightAbsorptionTowardsSunFactor);
        atmosphereMaterial.SetFloat("lightAbsorptionThroughCloudFactor", FindObjectOfType<CloudManger>().lightAbsorptionThroughCloudFactor);
        atmosphereMaterial.SetFloat("lightEnergyFactor", FindObjectOfType<CloudManger>().lightEnergyFactor);
        atmosphereMaterial.SetFloat("darknessThreshold", FindObjectOfType<CloudManger>().darknessThreshold);
        atmosphereMaterial.SetFloat("cloudThreshold", FindObjectOfType<CloudManger>().cloudThreshold);
        atmosphereMaterial.SetFloat("cloudSpeed", FindObjectOfType<CloudManger>().cloudSpeed);
        atmosphereMaterial.SetInt("numCloudSamplePoints", FindObjectOfType<CloudManger>().numSamplePoints);
        atmosphereMaterial.SetVector("cloudColor", FindObjectOfType<CloudManger>().cloudColor);

        underwaterMaterial.SetInt("isUnderwater", transform.GetComponent<WaterManager>().isUnderwater ? 1 : 0);
        underwaterMaterial.SetColor("waterColor", FindObjectOfType<PlanetManger>().underwaterColor);
        underwaterMaterial.SetFloat("viewDistance", FindObjectOfType<PlanetManger>().underwaterViewDistance);

        RenderTexture temp = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);
        Graphics.Blit(src, temp, atmosphereMaterial);
        Graphics.Blit(temp, dst, underwaterMaterial);
        temp.Release();
    }
}
