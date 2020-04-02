using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCOM : MonoBehaviour
{
    Vector3 CoM = Vector3.zero;
    float c = 0f;

    void Start()
    {
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rbs)
        {
            rb.centerOfMass = this.transform.InverseTransformPoint(this.transform.root.transform.position);

            CoM += rb.worldCenterOfMass * rb.mass;
            c += rb.mass;
        }

        CoM /= c;

        Debug.Log(this.transform.root.transform.name);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CoM, 0.1f);
    }
}
