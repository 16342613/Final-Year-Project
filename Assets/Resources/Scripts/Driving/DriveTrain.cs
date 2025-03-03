﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveTrain : MonoBehaviour
{
    public GameObject associatedWheel;
    private WheelCollider wheelCol;
    public bool frontWheel = false;

    // Start is called before the first frame update
    void Start()
    {
        //wheelRB = this.GetComponent<Rigidbody>();
        wheelCol = this.GetComponent<WheelCollider>();
        //wheelRB.maxAngularVelocity = 50;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetKey(KeyCode.W) == true || Input.GetKey(KeyCode.A) == true || Input.GetKey(KeyCode.S) == true || Input.GetKey(KeyCode.D) == true)
        {
            wheelCol.brakeTorque = 0;
        }
        else
        {
            wheelCol.brakeTorque = 50;
        }

        if (frontWheel == true)
        {
            if (Input.GetKey(KeyCode.D) && wheelCol.steerAngle < 25)
            {
                wheelCol.steerAngle += 5;
                //associatedWheel.transform.localEulerAngles = new Vector3(associatedWheel.transform.localEulerAngles.x, -wheelCol.steerAngle, associatedWheel.transform.localEulerAngles.z);
            }
            else if (Input.GetKey(KeyCode.A) && wheelCol.steerAngle > -25)
            {
                wheelCol.steerAngle -= 5;
            }
            else
            {
                if (wheelCol.steerAngle > 1)
                {
                    wheelCol.steerAngle = wheelCol.steerAngle - 1;
                }
                else if (wheelCol.steerAngle < 1)
                {
                    wheelCol.steerAngle = wheelCol.steerAngle + 1;
                }
            }
        }

        if (Input.GetKey(KeyCode.W))
        {
            wheelCol.motorTorque = -20;
        }
        if (Input.GetKey(KeyCode.S))
        {
            wheelCol.motorTorque = 10;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            wheelCol.brakeTorque = 50;
        }

        if (Input.GetKey(KeyCode.W) == false && Input.GetKey(KeyCode.S) == false)
        {
            wheelCol.motorTorque = 0;
        }

        associatedWheel.transform.Rotate(wheelCol.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        associatedWheel.transform.localEulerAngles = new Vector3(associatedWheel.transform.localEulerAngles.x, -wheelCol.steerAngle, associatedWheel.transform.localEulerAngles.z);
    }
}
