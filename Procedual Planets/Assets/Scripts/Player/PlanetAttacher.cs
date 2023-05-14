using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetAttacher : MonoBehaviour
{
    private GravitySystem mGravitySystem;
    private Transform mCurrentParent = null;
    private Rigidbody mRig;

    private void Start()
    {
        mGravitySystem = FindObjectOfType<GravitySystem>();
        mRig = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Transform stronest = mGravitySystem.GetStrongestGravitationalFieldFrom(transform);
        if (stronest != mCurrentParent)
        {
            transform.SetParent(stronest);
            mCurrentParent = stronest;
        }
    }
}
