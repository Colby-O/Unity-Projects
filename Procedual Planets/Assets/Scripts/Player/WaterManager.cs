using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterManager : MonoBehaviour
{
    public bool isUnderwater = false;

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("OMG");
        if (collision.gameObject.tag == "Water")
        {
            isUnderwater = true;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.tag == "Water")
        {
            isUnderwater = false;
        }
    }
}
