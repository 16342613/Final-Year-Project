using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HelperScripts.Components;
using System.Linq;
using System;

public class CollisionHandler : MonoBehaviour
{
    private GameObject thisObject;
    private Vector3[] collisionPoints;
    private Dictionary<GameObject, Deformer> collisionObjectToScript = new Dictionary<GameObject, Deformer>();
    private float objectMass;

    private void Start()
    {
        try
        {
            objectMass = this.GetComponent<Rigidbody>().mass;
        }
        catch (MissingComponentException)
        {
            objectMass = 10;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlasticDeformer>() != null)
        {
            collisionObjectToScript.Add(collision.gameObject, collision.gameObject.GetComponent<PlasticDeformer>());
        }
        else if (collision.gameObject.GetComponentInParent<PlasticDeformer>() != null)
        {
            collisionObjectToScript.Add(collision.gameObject, collision.gameObject.GetComponentInParent<PlasticDeformer>());
        }

        //Debug.Log(collision.relativeVelocity.magnitude * objectMass);

        try
        {
            collisionObjectToScript[collision.gameObject].contactInfo.Add(this.gameObject, collision.contacts);
            collisionObjectToScript[collision.gameObject].collisionInfo.Add(this.gameObject, collision.relativeVelocity.magnitude * objectMass);
        }
        catch (KeyNotFoundException)
        {

        }
    }

    private void OnCollisionStay(Collision collision)
    {
        try
        {
            collisionObjectToScript[collision.gameObject].contactInfo[this.gameObject] = collision.contacts;
            collisionObjectToScript[collision.gameObject].collisionInfo[this.gameObject] = collision.relativeVelocity.magnitude * objectMass;
        }
        catch (KeyNotFoundException)
        {

        }
    }

    private void OnCollisionExit(Collision collision)
    {
        try
        {
            collisionObjectToScript[collision.gameObject].contactInfo.Remove(this.gameObject);
            collisionObjectToScript[collision.gameObject].collisionInfo.Remove(this.gameObject);
            collisionObjectToScript.Remove(collision.gameObject);
        }
        catch (KeyNotFoundException)
        {

        }
    }
}
