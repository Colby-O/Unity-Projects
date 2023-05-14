using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ForestData : UpdateableData
{
    public int spacing = 3;
   // [Range(0, 1)]
    public float offsetAmount;
    [Range(0, 360)]
    public float maxRotation;
    public float maxScalingFactor;
    public float minScalingFactor;
    [Range(0, 1)]
    public float minHeight;
    [Range(0, 1)]
    public float maxHeight;
    [Min(0)]
    public float forestRenderRange;

    public Element[] elements; // frist element highest priority and so on
}

[System.Serializable]
public class Element
{
    public string name;
    [Range(1, 100)]
    public int density;

    public GameObject[] prefabs;

    public bool CanPlace()
    {
        return (Random.Range(0, 100) < density) ? true : false;
    }

    public GameObject GetRandom()
    {
        return prefabs[Random.Range(0, prefabs.Length)];
    }


}
