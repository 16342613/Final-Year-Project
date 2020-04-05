using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentreOfMassAdjuster : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Rigidbody>().centerOfMass = this.transform.InverseTransformPoint(this.transform.root.transform.position);
    }
}
