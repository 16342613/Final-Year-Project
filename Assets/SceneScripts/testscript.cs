using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testscript : MonoBehaviour
{
    HingeJoint hinge;
    // Start is called before the first frame update
    void Start()
    {
        hinge = this.GetComponent<HingeJoint>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(hinge.currentForce.magnitude);
    }
}
