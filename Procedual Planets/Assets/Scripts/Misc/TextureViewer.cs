using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureViewer : MonoBehaviour
{
    [Range(0,1)]
    public float depth = 0;
    public bool showBoundary = false;
    public bool showExactValue = true;
    Material material;

    void Start()
    {
        material = GetComponentInChildren<MeshRenderer>().material;
    }

    void Update()
    {
        material.SetFloat("depth", depth);
        //material.SetTexture("DisplayTexture", FindObjectOfType<GenerateNoise>().renderTexture);
        material.SetTexture("DisplayTexture", FindObjectOfType<MoonManger>().planetMap);
        material.SetInt("showBoundary", (showBoundary == true) ? 1 : 0);
        material.SetInt("showExactValue", (showExactValue == true) ? 1 : 0);
    }
}
