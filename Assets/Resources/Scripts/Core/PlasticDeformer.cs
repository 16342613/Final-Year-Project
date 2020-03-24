using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using HelperScripts.Methods;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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

    private Vector3[] secondaryVertexArray;
    private Vector3[] test;

    private CompositeCollider compositeCollider;
    private List<List<int>> squareVertices;
    private List<List<int>> connectedSquareNodes;
    private List<List<int>> unconnectedSquareNodes;
    private Dictionary<int, List<int>> vertexSquareMapping = new Dictionary<int, List<int>>();
    private List<int> collidersToUpdate = new List<int>();

    public float range = 0.25f;
    public bool debug = false;

    void Start()
    {
        objectRenderer = this.GetComponent<Renderer>();
        deformedMesh = this.GetComponent<MeshFilter>().mesh;
        originalVertices = deformedMesh.vertices;
        deformedVertices = deformedMesh.vertices;
        secondaryVertexArray = deformedMesh.vertices;
        vertexVelocities = new Vector3[originalVertices.Length];

        maxReboundVelocity = new float[originalVertices.Length];

        //ComputeShaderSetup();

        MeshManager meshManager = new MeshManager(this.GetComponent<MeshFilter>(), this.gameObject.name);
        //test = meshManager.CalculateMeshStrengths();
        //meshManager.DrawColliders();

        compositeCollider = this.gameObject.GetComponent<CompositeCollider>();
        int k = compositeCollider.index;
        WaitUntilReady();

        squareVertices = compositeCollider.squareVertices;
        unconnectedSquareNodes = compositeCollider.unconnectedSquareNodes;
        connectedSquareNodes = compositeCollider.connectedSquareNodes;
        vertexSquareMapping = compositeCollider.vertexSquareMapping;
    }

    private IEnumerator WaitUntilReady()
    {
        while (compositeCollider.ready == false)
        {
            yield return null;
        }
    }

    void FixedUpdate()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        collidersToUpdate.Clear();
        ParseContactPoints();
        frameCount = 0;

        uniformScale = this.transform.localScale.x;

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
                PlasticDeformVertexColliders(i, contactPoints, forces);
            }
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();

        collidersToUpdate = collidersToUpdate.Distinct().ToList();

        if (collidersToUpdate.Count > 0)
        {
            compositeCollider.UpdateColliderGPU(new List<int> { collidersToUpdate[0] });
            stopWatch.Stop();

            Debug.Log("TOTAL TIME : " + stopWatch.Elapsed.Milliseconds);
        }
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

    public void PlasticDeformVertexColliders(int vertexIndex, Vector3[] forceOrigins, float[] forces)
    {
        Vector3 vertVel = Vector3.zero;
        int noForceCount = 0;

        for (int i = 0; i < forceOrigins.Length; i++)
        {
            float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigins[i]);

            if (distance > range)
            {
                noForceCount++;

                if (noForceCount == (forceOrigins.Length))
                {
                    return;
                }

                continue;
            }

            float forceAtVertex = forces[i] / (meshStrength + 5 * (distance * distance));
            float vertexAcceleration = forceAtVertex / vertexMass;
            vertVel += (deformedVertices[vertexIndex] - forceOrigins[i]).normalized * vertexAcceleration;
        }

        vertexVelocities[vertexIndex] = vertVel;

        Vector3 displacement = secondaryVertexArray[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

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

        if (vertexSquareMapping.ContainsKey(vertexIndex) == false)
        {
            return;
        }

        for (int i = 0; i < vertexSquareMapping[vertexIndex].Count; i++)
        {
            collidersToUpdate.Add(vertexSquareMapping[vertexIndex][i]);
        }
    }

    public void PlasticDeformVertexPrototype(int vertexIndex, Vector3[] forceOrigins, float[] forces)
    {
        Vector3 vertVel = Vector3.zero;

        for (int i = 0; i < forceOrigins.Length; i++)
        {
            float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigins[i]);

            if (distance > range) continue;

            float forceAtVertex = forces[i] / (meshStrength + 5 * (distance * distance));
            float vertexAcceleration = forceAtVertex / vertexMass;
            vertVel += (deformedVertices[vertexIndex] - forceOrigins[i]).normalized * vertexAcceleration;
        }

        vertexVelocities[vertexIndex] = vertVel;

        Vector3 displacement = secondaryVertexArray[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

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

    // Gizmos for debug
    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Gizmos.DrawSphere(transform.TransformPoint(collisionPoint), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(offsetCollision), 0.01f);

        Gizmos.color = Color.green;
        for(int i=0; i<vertices.Length; i++)
        {
            //Gizmos.DrawLine(transform.TransformPoint(deformedVertices[i]), transform.TransformPoint(deformedVertices[i] + test[i]));
            //Gizmos.DrawLine(transform.TransformPoint(deformedVertices[i]), transform.TransformPoint(deformedVertices[i] - test[i] * 0.01f));
            Gizmos.DrawSphere(transform.TransformPoint(test[i]), 0.01f);
        }

        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        //Gizmos.DrawLine(transform.TransformPoint(offsetCollision), transform.TransformPoint(offsetPoint));
        //Gizmos.DrawSphere(transform.TransformPoint(deformedVertices[5500]), 0.01f);
    }*/
}

