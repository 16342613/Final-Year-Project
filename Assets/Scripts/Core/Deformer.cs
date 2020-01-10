using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Deformer : MonoBehaviour
{
    private Renderer objectRenderer;
    private Mesh deformedMesh;
    private Vector3[] deformedVertices;
    private Vector3[] originalVertices;
    private Vector3[] vertexVelocities;

    public float meshStrength = 1f;
    public float vertexMass = 1f;

    // Currently for debug only
    private Vector3 collisionPoint;

    void Start()
    {
        objectRenderer = this.GetComponent<Renderer>();
        deformedMesh = this.GetComponent<MeshFilter>().mesh;
        originalVertices = deformedMesh.vertices;
        deformedVertices = deformedMesh.vertices;
        vertexVelocities = new Vector3[originalVertices.Length];
    }

    void FixedUpdate()
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            UpdateVertex(i);
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();

        Debug.Log(objectRenderer.material.GetVector("_ForceOrigin"));
    }

    public void RespondToForce(Vector3 forceOrigin, float force)
    {
        collisionPoint = forceOrigin;
        objectRenderer.material.SetVector("_ForceOrigin", forceOrigin);

        Vector3 forceOrigin_to_localSpace = this.transform.InverseTransformPoint(forceOrigin);
        collisionPoint = forceOrigin_to_localSpace;

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            CalculateVertexVelocity(i, forceOrigin_to_localSpace, force);
        }
    }

    public void CalculateVertexVelocity(int vertexIndex, Vector3 forceOrigin, float force)
    {
        Vector3 vertex = deformedVertices[vertexIndex];

        float distance = Vector3.Distance(this.transform.InverseTransformPoint(vertex), this.transform.InverseTransformPoint(forceOrigin));                 // The distance from the force origin to the vertex
        float forceAtVertex = force / (meshStrength + (distance * distance));   // The force at that vertex according to inverse square law
        Vector3 originToVertex_Vector = vertex - forceOrigin;                   // Get the vector we are going to be moving the vertex along

        float vertexAcceleration = forceAtVertex / vertexMass;                              // From F = ma
        float vertexVelocity = vertexAcceleration * Time.deltaTime;                         // Get the velocity magnitude of the vertex according to the time
        vertexVelocities[vertexIndex] = originToVertex_Vector.normalized * vertexVelocity;  // The velocity of the vertex along the vertex path vector
    }

    public void UpdateVertex(int vertexIndex)
    {
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;
    }

    // Gizmos for debug
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }
}
