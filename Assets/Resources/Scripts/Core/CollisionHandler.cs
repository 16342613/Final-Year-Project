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
            try
            {
                collisionObjectToScript.Add(collision.gameObject, collision.gameObject.GetComponent<PlasticDeformer>());
            }
            catch (ArgumentException)
            {

            }
        }
        else if (collision.gameObject.GetComponentInParent<PlasticDeformer>() != null)
        {
            try
            {
                collisionObjectToScript.Add(collision.gameObject, collision.gameObject.GetComponentInParent<PlasticDeformer>());
            }
            catch (ArgumentException)
            {

            }
        }

        //Debug.Log(collision.relativeVelocity.magnitude * objectMass);

        try
        {
            try
            {
                collisionObjectToScript[collision.gameObject].contactInfo.Add(this.gameObject, collision.contacts);
            }
            catch (ArgumentException)
            {
                collisionObjectToScript[collision.gameObject].contactInfo[this.gameObject] = collision.contacts;
            }

            try
            {
                collisionObjectToScript[collision.gameObject].collisionInfo.Add(this.gameObject, collision.relativeVelocity.magnitude * objectMass);
            }
            catch (ArgumentException)
            {
                collisionObjectToScript[collision.gameObject].collisionInfo[this.gameObject] =  collision.relativeVelocity.magnitude * objectMass;
            }
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
