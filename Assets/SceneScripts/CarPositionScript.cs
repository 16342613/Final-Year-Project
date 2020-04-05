using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPositionScript : MonoBehaviour
{
    Transform chassisTransform;

    // Start is called before the first frame update
    void Start()
    {
        chassisTransform = GameObject.FindWithTag("Main Car").transform;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = chassisTransform.position;
    }
}
