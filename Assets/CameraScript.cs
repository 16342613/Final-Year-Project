using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    public float smoothSpeed = 0.3f;
    public Vector3 offset;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 desiredPosition = target.transform.position + offset;
        Vector3 smoothedPosition = Vector3.SmoothDamp(this.transform.position, desiredPosition, ref velocity, smoothSpeed);
        //Vector3 smoothedRotation = Vector3.SmoothDamp(this.transform.rotation.eulerAngles, target.transform.rotation.eulerAngles, ref velocity, smoothSpeed);

        this.transform.position = smoothedPosition;
        //this.transform.rotation = Quaternion.Euler(smoothedRotation);
        transform.LookAt(target.transform);
    }
}
