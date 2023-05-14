using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
	public Transform camera;

	public float movementSpeed = 2.0f;
	public float jumpPower = 10.0f;
	public float xMouseSpeed = 100.0f;
	public float yMouseSpeed = 100.0f;
	public float maxVelocity = 10.0f;

	public GravitySystem gravitySystem;

	private Transform mClosestPlanet;
	private Rigidbody mRig;

	private float mYRot = 0;
	private Vector3 mMovementDirection = Vector3.zero;

	void SetClosestPlanet()
	{
        mClosestPlanet = gravitySystem.GetStrongestGravitationalFieldFrom(transform);
	}

	void Start()
	{
		Debug.Log(gravitySystem.GetPlanets()[0]);
		mClosestPlanet = gravitySystem.GetPlanets()[0];
		SetClosestPlanet();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		mRig = GetComponent<Rigidbody>();
	}


	// Update is called once per frame
	void Update()
	{
		Vector3 dir = (transform.position - mClosestPlanet.position).normalized;
		Quaternion target = Quaternion.FromToRotation(Vector3.up, dir);
		Quaternion twoards = Quaternion.RotateTowards(transform.rotation, target, float.PositiveInfinity);

		transform.rotation = twoards * Quaternion.Euler(0, mYRot, 0);

		if (Input.GetKey(KeyCode.W)) {
			mMovementDirection.z = 1;
		} else if (Input.GetKey(KeyCode.S)) {
			mMovementDirection.z = -1;
		} else {
			mMovementDirection.z = 0;
		}

		if (Input.GetKey(KeyCode.D)) {
			mMovementDirection.x = 1;
		} else if (Input.GetKey(KeyCode.A)) {
			mMovementDirection.x = -1;
		} else {
			mMovementDirection.x = 0;
		}

		if (Input.GetKey(KeyCode.Space)) {
			mRig.AddRelativeForce(Vector3.up * jumpPower, ForceMode.Impulse);
		}
		if (Input.GetKey(KeyCode.LeftShift)) {
			mRig.AddRelativeForce(Vector3.up * jumpPower * -1, ForceMode.Impulse);
		}
		float xrotation = Input.GetAxis("Mouse X");
		float yrotation = Input.GetAxis("Mouse Y");
		camera.Rotate(-yrotation * yMouseSpeed, 0, 0);
		mYRot += xrotation * xMouseSpeed;
	}

	void FixedUpdate()
	{
		mRig.AddRelativeForce(mMovementDirection * movementSpeed);
		SetClosestPlanet();
		if (mRig.velocity.magnitude > maxVelocity) {
			mRig.velocity = mRig.velocity.normalized * maxVelocity;
		}
	}
}
