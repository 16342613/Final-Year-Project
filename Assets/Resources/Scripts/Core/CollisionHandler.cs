using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HelperScripts.Components;

public class CollisionHandler : MonoBehaviour
{
    private GameObject thisObject;
    private Vector3[] collisionPoints;
    private Dictionary<GameObject, Deformer> collisionObjectToScript = new Dictionary<GameObject, Deformer>();
    private float objectMass;

    private void Start()
    {
        objectMass = this.GetComponent<Rigidbody>().mass;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlasticDeformer>() != null)
        {
            collisionObjectToScript.Add(collision.gameObject, collision.gameObject.GetComponent<PlasticDeformer>());
        }

        collisionObjectToScript[collision.gameObject].contactInfo.Add(this.gameObject, collision.contacts);
        collisionObjectToScript[collision.gameObject].collisionInfo.Add(this.gameObject, collision.relativeVelocity.magnitude * objectMass);
    }

    private void OnCollisionStay(Collision collision)
    {
        collisionObjectToScript[collision.gameObject].contactInfo[this.gameObject] = collision.contacts;
        collisionObjectToScript[collision.gameObject].collisionInfo[this.gameObject] = collision.relativeVelocity.magnitude * objectMass;
    }

    private void OnCollisionExit(Collision collision)
    {
        collisionObjectToScript[collision.gameObject].contactInfo.Remove(this.gameObject);
        collisionObjectToScript[collision.gameObject].collisionInfo.Remove(this.gameObject);
    }
}
