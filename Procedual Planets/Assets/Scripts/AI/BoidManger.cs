using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManger : MonoBehaviour
{
    public int numPoints;
    public float visionRadius = 1.0f;
    public float sphereCastRadius = 0.1f;
    public int collisionMask = 0;
    List<Vector3> rayDir = new List<Vector3>();
    void GetCollisionRays()
    {
        for (int i = 0; i < numPoints; ++i)
        {
            float dist = i / (numPoints - 1.0f);
            float goldenRatio = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
            float theta = Mathf.Acos(1 - 2 * dist);
            float phi = 2 * Mathf.PI * goldenRatio * i;

            float x = Mathf.Sin(theta) * Mathf.Cos(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = Mathf.Cos(theta);

            //Debug.Log(new Vector3(x, y, z));
            //Debug.DrawLine(new Vector3(x, y, z), new Vector3(x + 0.01f, y + 0.01f, z + 0.001f), Color.black, 10000000);
            rayDir.Add(new Vector3(x, y, z));
        }
    }

    Vector3 FindBestAvailableDriection()
    {
        Vector3 bestDir = transform.forward;
        float furtherestDist = 0;
        RaycastHit hit;

        for (int i = 0; i < rayDir.Count; ++i)
        {
            Vector3 dir = transform.TransformDirection(rayDir[i]);
            bool hasHit = Physics.SphereCast(transform.position, sphereCastRadius, dir, out hit, visionRadius, collisionMask);

            if(hasHit)
            {
                if (hit.distance > furtherestDist)
                {
                    furtherestDist = hit.distance;
                }
            } else
            {
                return dir;
            }
        }

        return bestDir;
    }

    void Start()
    {
        GetCollisionRays();
    }

    void Update()
    {

    }
}
