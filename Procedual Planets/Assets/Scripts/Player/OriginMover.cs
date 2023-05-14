using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OriginMover : MonoBehaviour
{
	public int boundMax = 10000;
	public Transform originTransform;

	void MoveWorldInBounds()
	{
		Vector3 newOrigin = originTransform.position;
		GameObject[] transforms = SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject obj in transforms) {
			Transform t = obj.transform;
			t.position -= newOrigin;
		}
	}

	void FixedUpdate()
	{
		if (originTransform.position.magnitude > boundMax) {
			MoveWorldInBounds();
			return;
		}
	}
}
