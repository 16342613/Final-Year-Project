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
    private Vector3[] originalVerticesGPU;
    private Vector3[] verticesGPU;
    private Vector3[] vertexVelocitiesGPU;
    private Vector3[] cpuSide;

    private ComputeBuffer debugBuffer;
    private Vector3[] debugArray;
    private List<int> wrongPoints = new List<int>();
    private ComputeBuffer vertexDataBuffer;
    int test = 1;
    private VertexData[] vertices;

    struct VertexData
    {
        public Vector3 originalVertexPosition;
        public Vector3 vertexPosition;
        public Vector3 vertexVelocity;

        private Vector3 dummy;
        private Vector3 dummy2;
        private float dummy3;

        public VertexData(Vector3 originalVertexPosition, Vector3 vertexPosition, Vector3 vertexVelocity) : this()
        {
            this.originalVertexPosition = originalVertexPosition;
            this.vertexPosition = vertexPosition;
            this.vertexVelocity = vertexVelocity;
        }
    }

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

        originalVerticesGPU = (Vector3[])originalVertices.Clone();
        verticesGPU = (Vector3[])deformedVertices.Clone();
        vertexVelocitiesGPU = new Vector3[deformedVertices.Length];

        cpuSide = new Vector3[deformedMesh.vertices.Length];
        debugArray = new Vector3[deformedMesh.vertices.Length];

        vertices = new VertexData[deformedMesh.vertices.Length];

        ComputeShaderSetup();
    }

    private void ComputeShaderSetup()
    {
        int kernelHandle = computeShader.FindKernel("AssignThreads");

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new VertexData(deformedMesh.vertices[i], deformedMesh.vertices[i], Vector3.zero);
        }

        vertexDataBuffer = new ComputeBuffer(vertices.Length, 64);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexData"), vertexDataBuffer);
        computeShader.SetInt("vertexCount", vertices.Length);

        computeShader.Dispatch(computeShader.FindKernel("AssignThreads"), 1, 1, 1);

        vertexDataBuffer.Dispose();
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

    public void RespondToForce(Vector3 forceOrigin, float forceAmount)
    {
        bool noProblem = false;

        wrongPoints.Clear();

        //TestGPU(forceOrigin, forceAmount);
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            PlasticDeformVertex(i, forceOrigin, forceAmount);
        }

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            if (debugArray[i] != cpuSide[i])
            {
                wrongPoints.Add(i);
            }
        }

        //Debug.Log("GPU Debug: " + debugArray[test] + "; \tCPU Debug: " + cpuSide[test] + "; \tGPU Vertex Position:" + verticesGPU[test] + "; \tCPU Vertex Position:" + deformedVertices[test] + "; \tGPU Vertex Velocity:" + vertexVelocitiesGPU[test] + "; \tCPU Vertex Velocity:" + vertexVelocities[test] + "; \tIndex:" + test + "; \tNo Problem:" + noProblem);
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
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;
        cpuSide[vertexIndex] = vertexVelocities[vertexIndex];
    }

    public void TestGPU(Vector3 forceOrigin, float forceAmount)
    {
        int kernelHandle = computeShader.FindKernel("DeformVertex");

        originalVertexBuffer = new ComputeBuffer(originalVertices.Length, sizeof(float) * 3);
        originalVertexBuffer.SetData(originalVerticesGPU);

        verticesGPU = (Vector3[])deformedVertices.Clone();

        vertexBuffer = new ComputeBuffer(verticesGPU.Length, sizeof(float) * 3);
        vertexBuffer.SetData(verticesGPU);
        vertexVelocitiesBuffer = new ComputeBuffer(vertexVelocitiesGPU.Length, sizeof(float) * 3);
        vertexVelocitiesBuffer.SetData(vertexVelocitiesGPU);

        debugBuffer = new ComputeBuffer(debugArray.Length, sizeof(float) * 3);
        debugBuffer.SetData(debugArray);

        //VertexData[] vertexData = EncodeVertexThreadIDs(verticesGPU);
        //vertexThreadIDBuffer = new ComputeBuffer(vertexData.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData)));
        //vertexThreadIDBuffer.SetData(vertexData);
        //computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexData"), vertexThreadIDBuffer);

        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("originalVertexBuffer"), originalVertexBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexBuffer"), vertexBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexVelocitiesBuffer"), vertexVelocitiesBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("debugBuffer"), debugBuffer);
        computeShader.SetVector("forceOrigin", new Vector4(forceOrigin.x, forceOrigin.y, forceOrigin.z, 0));
        computeShader.SetInt("vertexCount", verticesGPU.Length);
        computeShader.SetFloat("forceAmount", forceAmount);
        computeShader.SetFloat("meshStrength", meshStrength);
        computeShader.SetFloat("vertexMass", vertexMass);
        computeShader.SetFloat("uniformScale", uniformScale);
        computeShader.SetFloat("springForce", springForce);
        computeShader.SetFloat("damping", damping);
        computeShader.SetFloat("time", Time.deltaTime);

        int size = (int)Math.Ceiling(Math.Sqrt(verticesGPU.Length));
        computeShader.SetInt(Shader.PropertyToID("sideCount"), size);

        computeShader.Dispatch(kernelHandle, size, size, 1);

        vertexVelocitiesBuffer.GetData(vertexVelocitiesGPU);
        vertexBuffer.GetData(verticesGPU);
        debugBuffer.GetData(debugArray);

        originalVertexBuffer.Dispose();
        vertexVelocitiesBuffer.Dispose();
        vertexBuffer.Dispose();
        debugBuffer.Dispose();
        //vertexThreadIDBuffer.Dispose();

        deformedVertices = (Vector3[])verticesGPU.Clone();
    }

    private VertexData[] EncodeVertexThreadIDs(Vector3[] input)
    {
        /*int currentIndex = 0;
        int size = (int)Math.Ceiling(Math.Sqrt(input.Length));
        VertexData[] vertexData = new VertexData[size * size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (currentIndex < input.Length)
                {
                    vertexData[currentIndex] = new VertexData(input[currentIndex], i, j);
                    currentIndex++;
                }
                else
                {
                    vertexData[currentIndex] = new VertexData(Vector3.zero, -1, -1);
                    currentIndex++;
                }
            }
        }

        Debug.LogError(vertexData[test].threadID_X + ", " + vertexData[test].threadID_Y);
        */
        return null;
    }

    // Gizmos for debug
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (wrongPoints.Count > 0)
        {
            //Gizmos.DrawSphere(transform.TransformPoint(verticesGPU[wrongPoints[index]]), 0.02f);
        }

        //Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(offsetCollision), 0.01f);

        Gizmos.color = Color.green;
        if (wrongPoints.Count > 0)
        {
            //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[wrongPoints[index]]), 0.02f);
        }

        //Gizmos.DrawSphere(transform.TransformPoint(offsetPoint), 0.01f);

        Gizmos.color = Color.white;
        //Gizmos.DrawLine(transform.TransformPoint(offsetCollision), transform.TransformPoint(offsetPoint));
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }
}

