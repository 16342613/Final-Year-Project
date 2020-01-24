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
    private float[] maxVertexDisplacement;

    public float meshStrength = 1f;
    public float vertexMass = 1f;
    public float springForce = 20f;
    public float damping = 5f;
    public bool returnToRestingForm = false;
    private float uniformScale = 1f;
    private int frameCount = 0;

    // Compute Shader Variables
    private ComputeShader computeShader;
    private ComputeBuffer originalVertexBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer vertexVelocitiesBuffer;
    private Vector3[] verticesGPU;
    private Vector3[] vertexVelocitiesGPU;
    private Vector3[] cpuSide;

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

        verticesGPU = deformedMesh.vertices;
        vertexVelocitiesGPU = new Vector3[deformedVertices.Length];

        cpuSide = new Vector3[deformedMesh.vertices.Length];
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

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            //deformedVertices[i] += vertexVelocitiesGPU[i] * Time.deltaTime;
        }
    }

    public void RespondToForce(Vector3 forceOrigin, float forceAmount)
    {
        TestGPU(forceOrigin, forceAmount);
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            PlasticDeformVertex(i, forceOrigin, forceAmount);
        }

        int test = 3000;
        Debug.Log("GPU: " + vertexVelocitiesGPU[test] + "; CPU: " + cpuSide[test]);
    }

    public void PlasticDeformVertex(int vertexIndex, Vector3 forceOrigin, float force)
    {
        Vector3 vertex = deformedVertices[vertexIndex];

        float distance = Vector3.Distance(vertex, forceOrigin);

        if (distance > 0.5f) return;

        float forceAtVertex = force / (meshStrength + 5 * (distance * distance));
        float vertexAcceleration = forceAtVertex / vertexMass;
        vertexVelocities[vertexIndex] = (vertex - forceOrigin).normalized * vertexAcceleration;

        Vector3 displacement = deformedVertices[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

        //if (vertexIndex == 3000) Debug.Log(reboundVelocity.magnitude - vertexVelocities[3000].magnitude);

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
        //deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;
        cpuSide[vertexIndex] = vertexVelocities[vertexIndex];
    }

    public void TestGPU(Vector3 forceOrigin, float forceAmount)
    {
        int kernelHandle = computeShader.FindKernel("CalculateDistance");

        originalVertexBuffer = new ComputeBuffer(originalVertices.Length, sizeof(float) * 3);
        originalVertexBuffer.SetData(originalVertices);

        verticesGPU = deformedVertices;

        vertexBuffer = new ComputeBuffer(verticesGPU.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verticesGPU);
        vertexVelocitiesBuffer = new ComputeBuffer(vertexVelocitiesGPU.Length, sizeof(float) * 3);
        vertexVelocitiesBuffer.SetData(vertexVelocitiesGPU);

        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("originalVertexBuffer"), vertexBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexBuffer"), vertexBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexVelocitiesBuffer"), vertexVelocitiesBuffer);
        computeShader.SetVector("forceOrigin", new Vector4(forceOrigin.x, forceOrigin.y, forceOrigin.z, 0));
        computeShader.SetInt("vertexCount", verticesGPU.Length);
        computeShader.SetFloat("forceAmount", forceAmount);
        computeShader.SetFloat("meshStrength", meshStrength);
        computeShader.SetFloat("vertexMass", vertexMass);
        computeShader.SetFloat("uniformScale", uniformScale);
        computeShader.SetFloat("springForce", springForce);
        computeShader.SetFloat("damping", damping);
        computeShader.SetFloat("time", Time.deltaTime);

        computeShader.Dispatch(kernelHandle, verticesGPU.Length, 1, 1);

        vertexVelocitiesBuffer.GetData(vertexVelocitiesGPU);
        vertexBuffer.GetData(verticesGPU);

        originalVertexBuffer.Dispose();
        vertexVelocitiesBuffer.Dispose();
        vertexBuffer.Dispose();
    }

    // Gizmos for debug
    /*private void OnDrawGizmos()
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
    }*/
}

