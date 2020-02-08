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
    private float[] maxReboundVelocity;

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
    private ComputeBuffer vertexDataBuffer;
    private VertexData[] vertices;

    private Vector3[] secondaryVertexArray;

    struct VertexData
    {
        public Vector3 originalVertexPosition;
        public Vector3 vertexPosition;
        public Vector3 vertexVelocity;
        public int threadID_X;
        public int threadID_Y;

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
        secondaryVertexArray = deformedMesh.vertices;
        vertexVelocities = new Vector3[originalVertices.Length];

        computeShader = (Resources.Load("Shaders/DeformationShader") as ComputeShader);

        maxReboundVelocity = new float[originalVertices.Length];

        originalVerticesGPU = (Vector3[])originalVertices.Clone();
        verticesGPU = (Vector3[])deformedVertices.Clone();
        vertexVelocitiesGPU = new Vector3[deformedVertices.Length];

        cpuSide = new Vector3[deformedMesh.vertices.Length];
        debugArray = new Vector3[deformedMesh.vertices.Length];

        vertices = new VertexData[deformedMesh.vertices.Length];

        //ComputeShaderSetup();
    }

    private void ComputeShaderSetup()
    {
        int kernelHandle = computeShader.FindKernel("AssignThreads");

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new VertexData(deformedMesh.vertices[i], deformedMesh.vertices[i], Vector3.zero);
        }

        int threadPoolDimension = (int)Math.Ceiling(Mathf.Sqrt(vertices.Length));
        int index = 0;

        for (int i = 0; i < threadPoolDimension; i++)
        {
            for (int j = 0; j < threadPoolDimension; j++)
            {
                if (index < vertices.Length)
                {
                    vertices[index].threadID_X = i;
                    vertices[index].threadID_Y = j;

                    index++;
                }
            }
        }

        vertexDataBuffer = new ComputeBuffer(vertices.Length, (sizeof(float) * 3 * 3) + (sizeof(int) * 2));
        debugBuffer = new ComputeBuffer(debugArray.Length, sizeof(float) * 3);
        debugBuffer.SetData(debugArray);

        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("debugBuffer"), debugBuffer);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("vertexData"), vertexDataBuffer);
        computeShader.SetInt("vertexCount", vertices.Length);
        computeShader.SetInt("threadPoolDimension", threadPoolDimension);

        computeShader.Dispatch(computeShader.FindKernel("AssignThreads"), 1, 1, 1);

        debugBuffer.GetData(debugArray);

        debugBuffer.Dispose();
        vertexDataBuffer.Dispose();
    }

    void FixedUpdate()
    {

        ParseContactPoints();
        frameCount = 0;

        uniformScale = this.transform.localScale.x;

        /*for (int i = 0; i < contactDetails.Count; i++)
        {
            RespondToForce(contactDetails.ElementAt(i).Value, 2);//contactDetails.ElementAt(i).Key);
        }*/

        Vector3[] contactPoints = contactDetails.Values.ToArray();
        float[] forces = contactDetails.Keys.ToArray();

        for (int i = 0; i < forces.Length; i++)
        {
            forces[i] = 2;
        }

        if (contactPoints.Length > 0)
        {
            for (int i = 0; i < deformedVertices.Length; i++)
            {
                PlasticDeformVertexPrototype(i, contactPoints, forces);
            }
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();

        string k = "";

        for (int i=0; i < forces.Length; i++)
        {
            k += forces[i] + " ; "; 
        }

        //Debug.Log(forces.Length);
    }

    public void RespondToForce(Vector3 forceOrigin, float forceAmount)
    {
        //TestGPU(forceOrigin, forceAmount);
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            PlasticDeformVertex(i, forceOrigin, forceAmount);
        }

        //Debug.Log("GPU Debug: " + debugArray[test] + "; \tCPU Debug: " + cpuSide[test] + "; \tGPU Vertex Position:" + verticesGPU[test] + "; \tCPU Vertex Position:" + deformedVertices[test] + "; \tGPU Vertex Velocity:" + vertexVelocitiesGPU[test] + "; \tCPU Vertex Velocity:" + vertexVelocities[test] + "; \tIndex:" + test + "; \tNo Problem:" + noProblem);
    }

    public void PlasticDeformVertex(int vertexIndex, Vector3 forceOrigin, float force)
    {
        float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigin);

        if (distance > force) return;

        Vector3 forceOriginToVertex = deformedVertices[vertexIndex] - forceOrigin;
        float forceAtVertex = force / (meshStrength + 5 * (distance * distance));
        float vertexAcceleration = forceAtVertex / vertexMass;
        vertexVelocities[vertexIndex] = forceOriginToVertex.normalized * vertexAcceleration;

        Vector3 displacement = secondaryVertexArray[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

        vertexVelocities[vertexIndex] -= reboundVelocity;
        vertexVelocities[vertexIndex] *= 1f - damping * 0.02f;
        secondaryVertexArray[vertexIndex] += vertexVelocities[vertexIndex] * 0.02f;

        if (vertexIndex == 3000) Debug.Log("Max displacement: " + maxReboundVelocity[3000] + "; current: " + displacement.magnitude);

        if (displacement.magnitude > maxReboundVelocity[vertexIndex])
        {
            maxReboundVelocity[vertexIndex] = displacement.magnitude;
        }
        else if (displacement.magnitude < maxReboundVelocity[vertexIndex] && returnToRestingForm == false)
        {
            return;
        }

        //vertexVelocities[vertexIndex] -= reboundVelocity;
        //vertexVelocities[vertexIndex] *= 1f - damping * Time.deltaTime;
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * 0.02f;
        //cpuSide[vertexIndex] = vertexVelocities[vertexIndex];
    }

    public void PlasticDeformVertexPrototype(int vertexIndex, Vector3[] forceOrigins, float[] forces)
    {
        Vector3 vertVel = Vector3.zero;

        for (int i = 0; i < forceOrigins.Length; i++)
        {
            float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigins[i]);

            if (distance > 0.5f) continue;

            float forceAtVertex = forces[i] / (meshStrength + 5 * (distance * distance));
            float vertexAcceleration = forceAtVertex / vertexMass;
            vertVel += (deformedVertices[vertexIndex] - forceOrigins[i]).normalized* vertexAcceleration;
        }

        vertexVelocities[vertexIndex] = vertVel;

        Vector3 displacement = secondaryVertexArray[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

        /*if(vertexIndex == 3000)
        {
            Debug.Log(reboundVelocity.magnitude);
        }*/

        vertexVelocities[vertexIndex] -= reboundVelocity;
        vertexVelocities[vertexIndex] *= 1f - damping * Time.deltaTime;
        secondaryVertexArray[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;

        if (reboundVelocity.magnitude > maxReboundVelocity[vertexIndex])
        {
            maxReboundVelocity[vertexIndex] = reboundVelocity.magnitude;
        }
        else if (reboundVelocity.magnitude < maxReboundVelocity[vertexIndex] && returnToRestingForm == false)
        {
            return;
        }

        deformedVertices[vertexIndex] = secondaryVertexArray[vertexIndex];
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

        computeShader.Dispatch(kernelHandle, verticesGPU.Length, 1, 1);

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
        //Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(offsetCollision), 0.01f);

        Gizmos.color = Color.green;

        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        //Gizmos.DrawLine(transform.TransformPoint(offsetCollision), transform.TransformPoint(offsetPoint));
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }
}

