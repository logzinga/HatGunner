using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public Rigidbody rb;

    public float forwardForce = 1000f;

    public float sidewaysForce = 1000f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey("w")) {
            rb.AddForce(forwardForce * Time.deltaTime, 0, 0);
        }

        if (Input.GetKey("s")) {
            rb.AddForce(-forwardForce * Time.deltaTime, 0, 0);
        }

        if (Input.GetKey("a")) {
            rb.AddForce(0, 0, sidewaysForce * Time.deltaTime);
        }
    }
}
