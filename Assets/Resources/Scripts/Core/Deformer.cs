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

    [HideInInspector]
    public Dictionary<GameObject, ContactPoint[]> contactInfo = new Dictionary<GameObject, ContactPoint[]>();

    protected void ParseContactPoints()
    {
        if (contactInfo.Count != 0)
        {
            contactPoints.Clear();

            for (int i = 0; i < contactInfo.Count; i++)
            {
                ContactPoint[] collisionArray = contactInfo.ElementAt(i).Value;

                for (int j = 0; j < collisionArray.Length; j++)
                {
                    contactPoints.Add(this.transform.InverseTransformPoint(collisionArray[i].point) + this.transform.InverseTransformDirection(collisionArray[i].normal) * 0.05f);
                }
            }
        }
    }
}
