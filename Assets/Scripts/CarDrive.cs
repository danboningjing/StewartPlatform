using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarDrive : MonoBehaviour
{  
    public float speed = 4f;
    public float a_speed = 15f;
    Vector3 movement;  
    // Update is called once per frame
    void FixedUpdate()
    {     
            float h = Input.GetAxisRaw("Horizontal"); //A D 左右
            float v = Input.GetAxisRaw("Vertical"); //W S 前后
            Move(h, v);           
    }

    void Move(float h, float v)
    {      
        movement.Set(0f, 0f, v);      
        movement = movement* speed * Time.deltaTime;
        transform.Translate(movement);

        float rotationX = h* a_speed * Time.deltaTime;
        //水平旋转
        transform.Rotate(0, rotationX, 0);

    }

}
