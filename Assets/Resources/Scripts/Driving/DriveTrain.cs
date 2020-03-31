using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveTrain : MonoBehaviour
{
    Rigidbody wheelRB;

    // Start is called before the first frame update
    void Start()
    {
        wheelRB = this.GetComponent<Rigidbody>();
        wheelRB.maxAngularVelocity = 50;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W)) 
        {
            wheelRB.AddRelativeTorque(5, 0, 0);
        }
        if (Input.GetKey(KeyCode.S))
        {
            wheelRB.AddRelativeTorque(-5, 0, 0);
        }
    }
}
