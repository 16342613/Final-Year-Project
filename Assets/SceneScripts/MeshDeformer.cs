using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour
{

    public float springForce = 20f;
    public float damping = 5f;
    public float test = 5f;
    public float localForce = 10f;
    public float mass = 1f;
    public float maxAreaOfEffect = 1f;

    Mesh deformingMesh;
    Vector3[] originalVertices, displacedVertices;
    Vector3[] vertexVelocities;
    List<Vector3> collisionPoints = new List<Vector3>();

    float uniformScale = 1f;

    Vector3 contactPoint;
    float forceOffset;
    //public int testVertexIndex = 3000;

    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }

        vertexVelocities = new Vector3[originalVertices.Length];

        forceOffset = GameObject.Find("Main Camera").GetComponent<MeshDeformerInput>().forceOffset;

        //GetComponent<Rigidbody>().AddTorque(0, 0, -100f);
    }

    void FixedUpdate()
    {
        uniformScale = transform.localScale.x;
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            UpdateVertex(i);
        }
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();

        for (int i = 0; i < collisionPoints.Count; i++)
        {
            AddDeformingForce(collisionPoints[i], localForce / collisionPoints.Count);
        }

        //GetComponent<MeshCollider>().sharedMesh = deformingMesh;
    }

    void UpdateVertex(int i)
    {
        Vector3 velocity = vertexVelocities[i];
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        displacement *= uniformScale;
        velocity -= displacement * springForce * Time.deltaTime;
        velocity *= 1f - damping * Time.deltaTime;
        vertexVelocities[i] = velocity;
        displacedVertices[i] += velocity * (Time.deltaTime / uniformScale);
    }

    public void AddDeformingForce(Vector3 point, float force)
    {
        point = transform.InverseTransformPoint(point);
        contactPoint = point; // recent addition

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, point, force);
        }
    }

    void AddForceToVertex(int i, Vector3 point, float force)
    {
        float distance = Vector3.Distance(displacedVertices[i], point);


        if (distance < maxAreaOfEffect)
        {
            Vector3 pointToVertex = displacedVertices[i] - point;
          //  Debug.DrawLine(point, displacedVertices[i]);
            pointToVertex *= uniformScale;
            float attenuatedForce = force / (mass + (pointToVertex * test).sqrMagnitude);
            float velocity = attenuatedForce * Time.deltaTime;
            vertexVelocities[i] += pointToVertex.normalized * velocity;


            if (i == 3000)
            {
                //Debug.Log("Vertex: " + displacedVertices[i] + "; Hit: " + point);
                Debug.Log(displacedVertices[i]);
            }

        }

    }

    private void OnCollisionStay(Collision collision)
    {
        collisionPoints.Clear();
        for (int i = 0; i < collision.contacts.Length; i++)
        {
            //AddDeformingForce(collision.contacts[i].point, localForce / collision.contacts.Length);
            Vector3 point = collision.contacts[i].point;
            point += collision.contacts[i].normal * -0.1f;
            collisionPoints.Add(point);
        }
    }

    /*void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Handles.color = Color.blue;

        Gizmos.DrawSphere(transform.TransformPoint(contactPoint), 0.01f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(displacedVertices[3000]), 0.01f);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.TransformPoint(displacedVertices[5500]), 0.01f);
    }*/
}

