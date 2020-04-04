using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WreckingBall : MonoBehaviour
{
    private Vector3 newPosition;
    private Vector3 endLerpPosition;
    private Vector3 startLerpPosition;

    public Vector3 movementAxis;
    public float movementDistance;
    public float speed;
    private float switchDirectionThreshold;


    // Start is called before the first frame update
    void Start()
    {
        startLerpPosition = this.transform.position;
        endLerpPosition = startLerpPosition + (movementAxis * movementDistance);
        switchDirectionThreshold = movementDistance * 0.1f * speed;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector3.Distance(this.transform.position, startLerpPosition) < switchDirectionThreshold)
        {
            newPosition = endLerpPosition;
        }
        else if (Vector3.Distance(this.transform.position, endLerpPosition) < switchDirectionThreshold)
        {
            newPosition = startLerpPosition;
        }

        this.transform.position = Vector3.Lerp(this.transform.position, newPosition, Time.deltaTime * speed);
    }
}
