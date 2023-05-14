using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
    public event System.Action OnValueUpdated;
    public bool autoUpdate;

    #if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NoiftOfUpdatedValues;
        }
    }

    public void NoiftOfUpdatedValues()
    {
        UnityEditor.EditorApplication.update -= NoiftOfUpdatedValues;
        OnValueUpdated?.Invoke();
    }
    #endif
}
