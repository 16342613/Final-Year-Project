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

        // Find out what vertex is mapped to what SCTP
        compositeCollider = this.gameObject.GetComponent<CompositeCollider>();
        vertexSquareMapping = compositeCollider.vertexSquareMapping;
        // The number of SCTPs that have been changed to trigger an update
        updateThreshold = Mathf.FloorToInt((float)compositeCollider.colliderTriangles.Count *
                                            ((float)updateThresholdPercentage / (float)100));
    }

    /// REFERENCE POINT 15
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

        // Overwrite the old vertex positions with the new vertex positions
        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();

        // If there are colliders that need to be updated
        if (collidersToUpdate.Count > 0)
        {
            if (collidersToUpdate.Count > updateThreshold)
            {
                // Prep sending the task to the GPU
                GPU_Handover();
            }

            // The number of frames since the colliders were last updated
            framesSinceGPUCall++;
        }

        // If the number of colliders to be updated is below the threshold, but they 
        // have been 'in the queue' for greater than the max number of idle frames, 
        // send this to the GPU
        if ((framesSinceGPUCall > maxIdleFrames) && (collidersToUpdate.Count > 0))
        {
            GPU_Handover();
        }
    }

    private void GPU_Handover()
    {
        collidersToUpdate = collidersToUpdate.Distinct().ToList();

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

        // Get the resultant force on the vertex
        for (int i = 0; i < forces.Length; i++)
        {
            totalForce += forces[i];
        }

        // The area of effect threshold
        localRange = (totalForce / (float)forces.Length) * 0.01f;

        // Find out which forces are within the area of effect
        for (int i = 0; i < forceOrigins.Length; i++)
        {
            // The distance from the force to the vertex
            float distance = Vector3.Distance(deformedVertices[vertexIndex], forceOrigins[i]);

            // If the force is too far away
            if (distance > localRange)
            {
                noForceCount++;

                // If all forces are too far away, skip this vertex
                if (noForceCount == (forceOrigins.Length))
                {
                    return;
                }

                continue;
            }

            // This is the actual force on the vertex (Inverese square law)
            float forceAtVertex = forces[i] / (meshStrength + 5 * (distance * distance));
            // This is the acceleration of the vertex (Newton's laws of motion)
            float vertexAcceleration = forceAtVertex / vertexMass;
            // The updated velocity of the vertex after applying acceleration (Hooke's law still needs to be applied)
            vertVel += (deformedVertices[vertexIndex] - forceOrigins[i]).normalized * vertexAcceleration;
        }

        vertexVelocities[vertexIndex] = vertVel;

        // Take the scale of the object into account!
        Vector3 displacement = deformedVertices[vertexIndex] - originalVertices[vertexIndex];
        displacement *= uniformScale;
        // The velocity acting against the direction of movement (Hooke's law)
        Vector3 reboundVelocity = displacement * springForce;
        vertexVelocities[vertexIndex] -= reboundVelocity;
        // Apply damping
        vertexVelocities[vertexIndex] *= 1f - damping * Time.deltaTime;
        deformedVertices[vertexIndex] += vertexVelocities[vertexIndex] * Time.deltaTime;

        // If this rebound velocity is greater than the previous greatest rebound velocity
        if (reboundVelocity.magnitude > maxReboundVelocity[vertexIndex])
        {
            maxReboundVelocity[vertexIndex] = reboundVelocity.magnitude;
        }
        // If this rebound velocity is less than the previous greatest rebound velocity
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
}

