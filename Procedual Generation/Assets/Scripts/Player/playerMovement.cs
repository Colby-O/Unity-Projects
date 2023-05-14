using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
	public Camera mcam;                 // Object to store the camera
	public float walkSpeed = 5.0f;
	public float maxSpeed = 5.0f;
	public float jumpPower = 25.0f;
	public float friction = 5.0f;
	public float gravity = 9.81f;
	private bool inAir = false;
	private Rigidbody mrig;             // Stores the players Ridig Body
	private Vector2 direction;

	void Start()
	{
		mrig = GetComponent<Rigidbody>();
		Cursor.lockState = CursorLockMode.Locked;
	}
	void FixedUpdate()
	{
		Vector3 vel = mrig.velocity;
		vel.y = 0;
		if (direction == Vector2.zero && vel.magnitude >= 1.0f)
		{
			mrig.velocity -= vel.normalized * friction;
		}
		else if (vel.magnitude <= maxSpeed)
		{
			Vector3 dir = Vector3.zero;
			dir += transform.forward * direction.x * walkSpeed;
			dir += transform.right * direction.y * walkSpeed;
			mrig.AddForce(dir);
		}
		else
		{
			vel = vel.normalized * maxSpeed;
			vel.y = mrig.velocity.y;
			mrig.velocity = vel;
		}
	}

	void Update()
	{
		Vector3 dir = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
			direction.x = 1;
		else if (Input.GetKey(KeyCode.S))
			direction.x = -1;
		else direction.x = 0;
		if (Input.GetKey(KeyCode.D))
			direction.y = 1;
		else if (Input.GetKey(KeyCode.A))
			direction.y = -1;
		else direction.y = 0;
		if (!inAir && Input.GetKey(KeyCode.Space))
			mrig.AddForce(0, jumpPower, 0);

		float mdx = Input.GetAxisRaw("Mouse X");
		float mdy = Input.GetAxis("Mouse Y");
		transform.Rotate(0, mdx, 0);
		mcam.transform.Rotate(-mdy, 0, 0);
	}
	void OnCollisionEnter(Collision other)
	{
		inAir = false;
	}
	void OnCollisionExit(Collision other)
	{
		inAir = true;
	}
}
