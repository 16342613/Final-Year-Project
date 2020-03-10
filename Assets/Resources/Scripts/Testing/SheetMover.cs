using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetMover : MonoBehaviour
{
    private Rigidbody rigidBody;

    void Start()
    {
        rigidBody = this.GetComponent<Rigidbody>();
    }

    void Update()
    {
        rigidBody.AddForce(-1, 0, 0);
    }
}
