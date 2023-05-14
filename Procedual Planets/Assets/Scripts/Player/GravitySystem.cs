using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GravitySystem : MonoBehaviour
{
	public int boundMax = 10000;
	public float gravityConstant = 10.0f;
	public Transform[] objects;

    private float GetObjectMass(Transform t)
    {
        if (t.gameObject.tag == "Planet")
        {
            return t.GetComponent<PlanetMovement>().mass;
        }
        else
        {
            return t.GetComponent<Rigidbody>().mass;
        }
    }

	public List<Transform> GetObjects()
    {
        return new List<Transform>(objects);
    }

	public List<Transform> GetPlanets()
	{
		List<Transform> planets = new List<Transform>();
		foreach (Transform t in objects)
        {
			if (t.gameObject.tag == "Planet")
            {
				planets.Add(t);
			}
		}
		return planets;
	}

    public Transform GetStrongestGravitationalFieldFrom(Transform t)
    {
        Transform strongest = null;
        float currentForce = -1;
		foreach (Transform planet in objects) {
            float force = CalculateForceBetween(t, planet);
			if (force > currentForce) {
				strongest = planet;
				currentForce = force;
			}
		}
        if (strongest == null)
        {
            Debug.LogError("NULL?????");
        }
        return strongest;
    }

    public float CalculateForceBetween(Transform t1, Transform t2)
    {
        float distance = Vector3.Distance(t1.position, t2.position);
        return (gravityConstant * GetObjectMass(t1) * GetObjectMass(t2)) / (Mathf.Pow(distance, 2.0f) + 1.0f);
    }

	void Update()
	{
		foreach (Transform t1 in objects)
        {
            Vector3 force = Vector3.zero;
			foreach (Transform t2 in objects)
            {
				float distance = Vector3.Distance(t1.position, t2.position);
				float f = (gravityConstant * GetObjectMass(t2)) / (Mathf.Pow(distance, 2.0f) + 1.0f);

				Vector3 direction = (t2.position - t1.position).normalized;
                force += f * direction * Time.deltaTime;
			}
			if (t1.gameObject.tag == "Planet")
            {
                PlanetMovement pm = t1.GetComponent<PlanetMovement>();
                pm.AddForce(force);
				t1.Translate(pm.velocity);
			}
            else
            {
                Rigidbody rig1 = t1.GetComponent<Rigidbody>();
				rig1.AddForce(force, ForceMode.Acceleration);
            }
		}
	}
}
