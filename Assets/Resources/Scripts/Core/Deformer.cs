using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Deformer : MonoBehaviour
{
    protected Mesh deformedMesh;
    protected Vector3[] deformedVertices;
    protected Vector3[] originalVertices;
    protected Vector3[] vertexVelocities;
    protected List<Vector3> contactPoints = new List<Vector3>();
    protected Dictionary<float, Vector3> contactDetails = new Dictionary<float, Vector3>();

    [HideInInspector]
    public Dictionary<GameObject, ContactPoint[]> contactInfo = new Dictionary<GameObject, ContactPoint[]>();
    [HideInInspector]
    public Dictionary<GameObject, float> collisionInfo = new Dictionary<GameObject, float>();

    private float collisionProximitySimilarity = 1f;

    protected void ParseContactPoints()
    {
        List<Vector3> consideredContactPoints = new List<Vector3>();

        if (contactInfo.Count != 0)
        {
            contactPoints.Clear();
            contactDetails.Clear();

            for (int i = 0; i < contactInfo.Count; i++)
            {
                bool addAllPoints = false;
                ContactPoint[] collisionArray = contactInfo.ElementAt(i).Value;
                Vector3 averageContactPoint = Vector3.zero;

                for (int j = 0; j < collisionArray.Length; j++)
                {
                    Vector3 offsetContactPoint = this.transform.InverseTransformPoint(collisionArray[j].point) + this.transform.InverseTransformDirection(collisionArray[j].normal) * 0.05f;

                    consideredContactPoints.Add(offsetContactPoint);
                    averageContactPoint += offsetContactPoint;
                }

                averageContactPoint = averageContactPoint / collisionArray.Length;

                for (int j = 0; j < collisionArray.Length; j++)
                {
                    if (Vector3.Distance(averageContactPoint, this.transform.InverseTransformPoint(collisionArray[j].point)) > collisionProximitySimilarity)
                    {
                        addAllPoints = true;
                        break;
                    }

                    if (j == collisionArray.Length - 1)
                    {
                        if (i == 0)
                        {
                            contactDetails.Add(collisionInfo[contactInfo.ElementAt(i).Key], averageContactPoint);
                            contactPoints.Add(averageContactPoint);
                        }
                        else
                        {
                            contactDetails.Add(collisionInfo[contactInfo.ElementAt(i).Key], averageContactPoint);
                            contactPoints.Add(averageContactPoint);
                        }
                    }
                }

                if(addAllPoints == true)
                {
                    for (int j = 0; j < collisionArray.Length; j++)
                    {
                        contactDetails.Add(collisionInfo[contactInfo.ElementAt(i).Key], this.transform.InverseTransformPoint(collisionArray[j].point) + this.transform.InverseTransformDirection(collisionArray[j].normal) * 0.05f);
                        contactPoints.Add(this.transform.InverseTransformPoint(collisionArray[j].point) + this.transform.InverseTransformDirection(collisionArray[j].normal) * 0.05f);
                    }
                }
            }
        }
    }
}
