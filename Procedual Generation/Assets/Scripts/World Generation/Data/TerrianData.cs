using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrianData : UpdateableData
{
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public bool useFallOff;
    public bool useFlatShading;
    public float uniformScale;

    public float minHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }
}
