using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonnetAssemblyScript : MonoBehaviour
{
    private Rigidbody rigidBody;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = this.GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;
    }

    // Update is called once per frame
    void Update()
    {
        //rigidBody.AddForce(1, 0, 0);
    }
}
