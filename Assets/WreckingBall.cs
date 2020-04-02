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


    // Start is called before the first frame update
    void Start()
    {
        startLerpPosition = this.transform.position;

        endLerpPosition = startLerpPosition + (movementAxis * movementDistance);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector3.Distance(this.transform.position, startLerpPosition) < 0.1f)
        {
            newPosition = endLerpPosition;
        }
        else if (Vector3.Distance(this.transform.position, endLerpPosition) < 0.1f)
        {
            newPosition = startLerpPosition;
        }

        this.transform.position = Vector3.Lerp(this.transform.position, newPosition, Time.deltaTime * speed);
    }
}
