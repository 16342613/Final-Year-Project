using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class PlasticDeformer : Deformer
{
    private ObjectProperties objectProperties;

    private Renderer objectRenderer;
    private ComputeShader computeShader;
    private float[] maxVertexDisplacement;

    public float meshStrength = 1f;
    public float vertexMass = 1f;
    public float springForce = 20f;
    public float damping = 5f;
    public bool returnToRestingForm = false;
    private float uniformScale = 1f;
    private int frameCount = 0;

    void Start()
    {
        objectRenderer = this.GetComponent<Renderer>();
        deformedMesh = this.GetComponent<MeshFilter>().mesh;
        originalVertices = deformedMesh.vertices;
        deformedVertices = deformedMesh.vertices;
        vertexVelocities = new Vector3[originalVertices.Length];

        computeShader = (ComputeShader)Resources.Load("Shaders/DeformationShader");
        //TestComputeShader();
        //objectRenderer.material.SetFloat("_Test", 1.0f);

        maxVertexDisplacement = new float[originalVertices.Length];
    }

    void FixedUpdate()
    {
        frameCount++;

        if (frameCount % 3 == 0)
        {
            ParseContactPoints();
            frameCount = 0;
        }

        uniformScale = this.transform.localScale.x;

        for (int i = 0; i < contactDetails.Count; i++)
        {
            RespondToForce(contactDetails.ElementAt(i).Value, 2);//contactDetails.ElementAt(i).Key);
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
    }

    public void RespondToForce(Vector3 forceOrigin, float force)
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            PlasticDeformVertex(i, forceOrigin, force);
        }
    }

    public void PlasticDeformVertex(int vertexIndex, Vector3 forceOrigin, float force)
    {
        Vector3 vertex = deformedVertices[vertexIndex];

        float distance = Vector3.Distance(vertex, forceOrigin);

        if (distance > 0.5f) return;

        float forceAtVertex = force / (meshStrength + 5 * (distance * distance));
        float vertexAcceleration = forceAtVertex / vertexMass;
        vertexVelocities[vertexIndex] = (vertex - forceOrigin).normalized * (vertexAcceleration);

        Vector3 displacement = deformedVertices[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

        if (vertexIndex == 3000) Debug.Log(reboundVelocity.magnitude - vertexVelocities[3000].magnitude);

        /*if (displacement.magnitude > maxVertexDisplacement[vertexIndex])
        {
            maxVertexDisplacement[vertexIndex] = displacement.magnitude;
        }
        else if (displacement.magnitude < maxVertexDisplacement[vertexIndex] && returnToRestingForm == false)
        {
            return;
        }*/

        vertexVelocities[vertexIndex] -= reboundVelocity;
        vertexVelocities[vertexIndex] *= 1f - damping * Time.deltaTime;
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;
    }

    // Gizmos for debug
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < contactPoints.Count; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(contactPoints[i]), 0.01f);
        }

        //Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(offsetCollision), 0.01f);

        Gizmos.color = Color.green;
        //Gizmos.DrawSphere(transform.TransformPoint(offsetPoint), 0.01f);
        Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        //Gizmos.DrawLine(transform.TransformPoint(offsetCollision), transform.TransformPoint(offsetPoint));
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }
}

