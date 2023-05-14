using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LandscapeGenerator))]
public class Terrian3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LandscapeGenerator mapGen = (LandscapeGenerator)target;

        if (DrawDefaultInspector() && mapGen.caveData.autoUpdate)
        {
            mapGen.Generate();
        }

        if (GUILayout.Button("Generate"))
        {
            mapGen.Generate(); 
        }

        if (GUILayout.Button("Denerate"))
        {
            mapGen.ClearMeshData();
        }
    }
}
