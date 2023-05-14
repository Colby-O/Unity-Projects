using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManger : MonoBehaviour
{

    public float speed;

    Rigidbody rig;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
    }


    void Update()
    {
        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(verticalAxis, 0, horizontalAxis) * speed * Time.deltaTime;

        rig.MovePosition(transform.position + movement);
    }
}
