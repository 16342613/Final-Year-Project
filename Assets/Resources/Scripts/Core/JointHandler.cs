using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointHandler : MonoBehaviour
{
    void OnJointBreak(float breakForce)
    {
        this.transform.parent = null;
    }
}
