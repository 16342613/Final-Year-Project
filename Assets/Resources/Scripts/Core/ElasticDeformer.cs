using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class ElasticDeformer : MonoBehaviour
{
    private ObjectProperties objectProperties;

    private Renderer objectRenderer;
    private ComputeShader computeShader;
    private Mesh deformedMesh;
    private Vector3[] deformedVertices;
    private Vector3[] originalVertices;
    private Vector3[] vertexVelocities;
    private float[] maxVertexDisplacement;
    private bool newCollisionsDetected;
    private List<ContactPoint> contactPoints = new List<ContactPoint>();

    public float meshStrength = 1f;
    public float vertexMass = 1f;
    public float springForce = 20f;
    public float damping = 5f;
    public bool returnToRestingForm = false;
    private float uniformScale = 1f;

    // Currently for debug only
    private Vector3 collisionPoint = Vector3.zero;
    private int computeInt = 0;
    public RenderTexture result;
    public Texture2D texture;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer vertexVelocitiesBuffer;
    private ComputeBuffer testBuffer;
    private Vector3[] test = new Vector3[] { Vector3.one };
    private Vector3[] shaderVertices;
    private Vector3[] shaderVertexVelocities;

    private Vector3 offsetPoint;
    private Vector3 offsetCollision;

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
        uniformScale = this.transform.localScale.x;

        for (int i = 0; i < contactPoints.Count; i++)
        {
            RespondToForce(contactPoints[i].point - contactPoints[i].normal * 0.05f, 2);
        }

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            UpdateVertex(i);
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();

        //Debug.Log(contactPoints.Count);
    }

    int TestComputeShader()
    {
        int kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetInt("numberToSquare", 2);
        result = new RenderTexture(100, 1, 24);

        result.enableRandomWrite = true;
        result.Create();

        vertexBuffer = new ComputeBuffer(deformedVertices.Length, sizeof(float) * 3);
        vertexBuffer.SetData(deformedVertices);
        computeShader.SetBuffer(kernelHandle, "vertices", vertexBuffer);

        computeShader.SetTexture(kernelHandle, "Result", result);
        computeShader.Dispatch(kernelHandle, 100 / 4, 1 / 1, 1);

        RenderTexture.active = result;
        texture = new Texture2D(100, 1, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
        texture.Apply();

        vertexBuffer.GetData(deformedVertices);

        return kernelHandle;
    }

    public void RespondToForce(Vector3 forceOrigin, float force)
    {
        #region todo
        /*int kernelHandle = computeShader.FindKernel("CSMain");
        shaderVertices = (Vector3[]) deformedVertices.Clone();
        shaderVertexVelocities = vertexVelocities;

        vertexBuffer = new ComputeBuffer(shaderVertices.Length, sizeof(float) * 3);
        vertexBuffer.SetData(shaderVertices);
        computeShader.SetBuffer(kernelHandle, "vertices", vertexBuffer);

        vertexVelocitiesBuffer = new ComputeBuffer(shaderVertices.Length, sizeof(float) * 3);
        vertexVelocitiesBuffer.SetData(shaderVertexVelocities);
        computeShader.SetBuffer(kernelHandle, "velocities", vertexVelocitiesBuffer);

        testBuffer = new ComputeBuffer(1, sizeof(float) * 3);
        testBuffer.SetData(test);
        computeShader.SetBuffer(kernelHandle, "testVector", testBuffer);

        computeShader.SetVector(Shader.PropertyToID("forceOrigin"), forceOrigin);
        computeShader.SetFloat(Shader.PropertyToID("forceAmount"), force);
        computeShader.SetFloat(Shader.PropertyToID("meshStrength"), meshStrength);
        computeShader.SetFloat(Shader.PropertyToID("time"), Time.deltaTime);
        computeShader.SetFloat(Shader.PropertyToID("vertexMass"), vertexMass);
        computeShader.SetInt(Shader.PropertyToID("numberOfVertices"), deformedVertices.Length);

        computeShader.Dispatch(kernelHandle, 3, 3, 1);

        vertexVelocitiesBuffer.GetData(shaderVertexVelocities);
        testBuffer.GetData(test);

        vertexVelocitiesBuffer.Dispose();
        vertexBuffer.Dispose();
        testBuffer.Dispose();
        //vertexVelocities = shaderVertexVelocities;
        */
        #endregion

        collisionPoint = forceOrigin;

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

        float distance = Vector3.Distance(vertex, forceOrigin);                 // The distance from the force origin to the vertex

        /*if (vertexIndex == 0)
        {
            offsetPoint = deformedVertices[0] + ((vertex - forceOrigin) * 0.01f);
            offsetCollision = forceOrigin - (vertex - forceOrigin) * 0.1f;
        }*/

        //if (Physics.Linecast(Camera.main.transform.TransformPoint(Camera.main.transform.position), transform.TransformPoint(vertex + ((vertex - forceOrigin) * 0.01f))) == true)
        {
            //return;
        }

        if (distance > 0.5f) return;

        float forceAtVertex = force / (meshStrength + 5 * (distance * distance));   // The force at that vertex according to inverse square law


        float vertexAcceleration = forceAtVertex / vertexMass;                              // From F = ma
        vertexVelocities[vertexIndex] = (vertex - forceOrigin).normalized * (vertexAcceleration * Time.deltaTime);  // The velocity of the vertex along the vertex path vector
    }

    public void UpdateVertex(int vertexIndex)
    {
        Vector3 velocity = vertexVelocities[vertexIndex];

        if (returnToRestingForm == true && velocity == Vector3.zero)
        {
            deformedVertices[vertexIndex] = originalVertices[vertexIndex];
            return;
        }

        if (velocity.magnitude < 0.00001f)
        {
            vertexVelocities[vertexIndex] = Vector3.zero;
            return;
        }

        Vector3 displacement = deformedVertices[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        if (displacement.magnitude > maxVertexDisplacement[vertexIndex])
        {
            maxVertexDisplacement[vertexIndex] = displacement.magnitude;
        }
        else if (displacement.magnitude < maxVertexDisplacement[vertexIndex] && returnToRestingForm == false)
        {
            return;
        }

        Vector3 reboundVelocity = displacement * springForce * Time.deltaTime;

        if(vertexIndex == 3000)
        {
            Debug.Log("Vel: " + velocity.magnitude + " ; Reb: " + reboundVelocity.magnitude);
        }
        velocity = velocity - reboundVelocity;
        velocity *= 1f - damping * Time.deltaTime;

        vertexVelocities[vertexIndex] = velocity;

        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;
    }

    // Gizmos for debug
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < contactPoints.Count; i++)
        {
            Gizmos.DrawSphere((contactPoints[i].point), 0.01f);
        }
        //Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(offsetCollision), 0.01f);

        Gizmos.color = Color.green;
        //Gizmos.DrawSphere(transform.TransformPoint(offsetPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        //Gizmos.DrawLine(transform.TransformPoint(offsetCollision), transform.TransformPoint(offsetPoint));
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (contactPoints.Contains(collision.contacts[i]) == false)
            {
                contactPoints.Add(collision.contacts[i]);
            }
        }

        contactPoints = collision.contacts.ToList();
    }
}
