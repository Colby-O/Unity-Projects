using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetMovement : MonoBehaviour
{
    public Vector3 velocity = Vector3.zero;
    public float mass = 1.0f;
    public void AddForce(Vector3 force)
    {
        velocity += force;
    }
}
