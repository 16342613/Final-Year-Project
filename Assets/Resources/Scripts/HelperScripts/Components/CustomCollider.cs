using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CustomCollider
{
    public BoxCollider collider;
    public List<Vector3> colliderVertices = new List<Vector3>();

    public CustomCollider(BoxCollider collider)
    {
        this.collider = collider;
    }

    public List<Vector3> GetColliderVertices()
    {
        // If it has already been computed
        if (colliderVertices.Count == 4)
        {
            return colliderVertices;
        }
        // We need to compute it first
        else
        {
            Vector3 colliderCentre = collider.center;
            Vector3 colliderExtents = collider.size;

            for (int i = 0; i < 4; i++)
            {
                Vector3 extents = colliderExtents;
                extents.Scale(new Vector3((i & 1) == 0 ? 1 : -1, (i & 2) == 0 ? 1 : -1, (i & 4) == 0 ? 1 : -1));
                Vector3 localVertexPosition = colliderCentre + extents;
                colliderVertices.Add(localVertexPosition);
            }

            return colliderVertices;
        }
    }
}
