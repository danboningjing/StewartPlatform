using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToStart_Trigger : MonoBehaviour {
    public Transform start;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "CarBody")
        {
            other.transform.parent.position = start.position;
        }
    }
}
