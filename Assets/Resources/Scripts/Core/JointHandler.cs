using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointHandler : MonoBehaviour
{
    void OnJointBreak(float breakForce)
    {
        this.transform.parent = null;
        GameObject.Destroy(this.GetComponent<PlasticDeformer>());

        this.GetComponent<CompositeCollider>().KillColliders();
        GameObject.Destroy(this.GetComponent<CompositeCollider>());
        this.gameObject.AddComponent<BoxCollider>();

        Debug.Log(this.transform.name + " is broken off!");
    }
}
