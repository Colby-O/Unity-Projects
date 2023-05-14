using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(worldGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        worldGenerator mapGen = (worldGenerator)target;

        if(DrawDefaultInspector() && mapGen.autoUpdate)
        {
            mapGen.DrawMapInEditor();
        }

        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
