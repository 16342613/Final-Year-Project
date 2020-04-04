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
    private float[] maxReboundVelocity;

    public float meshStrength = 1f;
    public float vertexMass = 1f;
    public float springForce = 20f;
    public float damping = 5f;
    public bool returnToRestingForm = false;
    private float uniformScale = 1f;

    private CompositeCollider compositeCollider;
    private Dictionary<int, List<int>> vertexSquareMapping = new Dictionary<int, List<int>>();
    private List<int> collidersToUpdate = new List<int>();

    public float range = 0.25f;
    public bool debug = false;

    public int maxIdleFrames = 250;
    public int updateThresholdPercentage = 10;
    private int framesSinceGPUCall = 0;
    private int updateThreshold;

    /// REFERENCE POINT 13
    void Start()
    {
        deformedMesh = this.GetComponent<MeshFilter>().mesh;
        // Store the original vertex positions of the mesh
        originalVertices = deformedMesh.vertices;
        // This is the deformed vertex positions of the mesh, used during runtime
        deformedVertices = deformedMesh.vertices;
        // The velocities of each vertex
        vertexVelocities = new Vector3[originalVertices.Length];
        // This stores the maximum deformation of each vertex so far
        maxReboundVelocity = new float[originalVertices.Length];

        // Find out the indices of every vertex in the SCTPs
        compositeCollider = this.gameObject.GetComponent<CompositeCollider>();
        vertexSquareMapping = compositeCollider.vertexSquareMapping;
        // The number of SCTPs that have been changed to trigger an update
        updateThreshold = Mathf.FloorToInt((float)compositeCollider.colliderTriangles.Count * 
                                            ((float)updateThresholdPercentage / (float)100));
    }

    void FixedUpdate()
    {
        // Convert the raw Unity colliision data into a more presentable format
        ParseContactPoints();
        uniformScale = this.transform.localScale.x;

        Vector3[] contactPoints = contactDetails.Values.ToArray();
        float[] forces = contactDetails.Keys.ToArray();

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
           //GPU_Handover(collidersToUpdate);  // Update every frame no matter what

            if (collidersToUpdate.Count > updateThreshold)
            {
                GPU_Handover(collidersToUpdate);
            }

            framesSinceGPUCall++;
        }

        if ((framesSinceGPUCall > maxIdleFrames) && (collidersToUpdate.Count > 0))
        {
            GPU_Handover(null, true);
        }
    }

    private void GPU_Handover(List<int> newCollidersToUpdate, bool forceUpdate = false)
    {
        if (forceUpdate == true)
        {
            collidersToUpdate = collidersToUpdate.Distinct().ToList();
        }
        else
        {
            collidersToUpdate.AddRange(newCollidersToUpdate);
            collidersToUpdate = collidersToUpdate.Distinct().ToList();
        }

        compositeCollider.UpdateColliderGPU(collidersToUpdate);
        collidersToUpdate.Clear();
        framesSinceGPUCall = 0;
    }

    /// REFERENCE POINT 14
    public void PlasticDeformVertexColliders(int vertexIndex, Vector3[] forceOrigins, float[] forces)
    {
        Vector3 vertVel = Vector3.zero;
        int noForceCount = 0;
        float localRange = 0;
        float totalForce = 0;

        for (int i = 0; i < forces.Length; i++)
        {
            totalForce += forces[i];
        }

        localRange = (totalForce / (float)forces.Length) * 0.01f;

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

        Vector3 displacement = deformedVertices[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;

        Vector3 reboundVelocity = displacement * springForce;

        vertexVelocities[vertexIndex] -= reboundVelocity;
        vertexVelocities[vertexIndex] *= 1f - damping * Time.deltaTime;
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;

        if (reboundVelocity.magnitude > maxReboundVelocity[vertexIndex])
        {
            maxReboundVelocity[vertexIndex] = reboundVelocity.magnitude;
        }
        else if (reboundVelocity.magnitude < maxReboundVelocity[vertexIndex] && returnToRestingForm == false)
        {
            return;
        }

        if (vertexSquareMapping.ContainsKey(vertexIndex) == false)
        {
            return;
        }

        for (int i = 0; i < vertexSquareMapping[vertexIndex].Count; i++)
        {
            if (collidersToUpdate.Contains(vertexSquareMapping[vertexIndex][i]) == false)
            {
            collidersToUpdate.Add(vertexSquareMapping[vertexIndex][i]);
            }
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

