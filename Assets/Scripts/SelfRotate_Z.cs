using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfRotate_Z : MonoBehaviour {
    public float speed = 5;
    private const float maxAngle=30f;

    // Use this for initialization
    void Update()
    {
        if(this.transform.eulerAngles.z>maxAngle && this.transform.eulerAngles.z < (360-maxAngle))
        {
            speed = -speed;
            //Debug.Log(this.transform.eulerAngles.z);
        }
    }
    // Update is called once per frame
    void FixedUpdate () {
        this.transform.Rotate(0f,0f,speed * Time.deltaTime);
	}
}
