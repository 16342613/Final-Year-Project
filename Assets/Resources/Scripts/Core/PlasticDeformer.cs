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

        MeshManager meshManager = new MeshManager(this.GetComponent<MeshFilter>(), this.gameObject.name);

        compositeCollider = this.gameObject.GetComponent<CompositeCollider>();

        squareVertices = compositeCollider.squareVertices;
        unconnectedSquareNodes = compositeCollider.unconnectedSquareNodes;
        connectedSquareNodes = compositeCollider.connectedSquareNodes;
        vertexSquareMapping = compositeCollider.vertexSquareMapping;
    }

    void Update()
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

        if (collidersToUpdate.Count > 0)
        {
            GPU_Handover(collidersToUpdate);
            compositeCollider.UpdateColliderGPU(collidersToUpdate);
            //compositeCollider.UpdateColliderNaive(collidersToUpdate);
            stopWatch.Stop();

            Debug.Log("TOTAL FRAME TIME : " + stopWatch.Elapsed.Milliseconds + " ; FPS : " + (1.0f / Time.deltaTime));
        }
    }

    private void GPU_Handover(List<int> newCollidersToUpdate)
    {
        collidersToUpdate.AddRange(newCollidersToUpdate);
        collidersToUpdate = collidersToUpdate.Distinct().ToList();

        
    }

    public void PlasticDeformVertexColliders(int vertexIndex, Vector3[] forceOrigins, float[] forces)
    {
        Vector3 vertVel = Vector3.zero;
        int noForceCount = 0;
        float localRange = 0;
        float totalForce = 0;

        for(int i=0; i<forces.Length; i++)
        {
            totalForce += forces[i];
        }

        localRange = (totalForce / (float) forces.Length) * 0.1f;

        for (int i = 0; i < forceOrigins.Length; i++)
        {
            float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigins[i]);

            if (distance > localRange)
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

