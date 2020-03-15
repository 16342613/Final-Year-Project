using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetMover : MonoBehaviour
{
    private Rigidbody rigidBody;
    public int force = -5;

    void Start()
    {
        rigidBody = this.GetComponent<Rigidbody>();
    }

    void Update()
    {
        rigidBody.AddForce(force, 0, 0);
    }
}
