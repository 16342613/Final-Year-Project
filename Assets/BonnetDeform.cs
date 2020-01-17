using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using HelperScripts.Methods;
using UnityEditor;

public class BonnetDeform : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private MeshManager meshManager;
    private Dictionary<Vector3, List<Vector3>> connectedVertices;
    private Rigidbody rigidBody;
    private GameObject parent;
    private List<Joint> connectedJoints = new List<Joint>();
    private Vector3 weakestVertex = Vector3.zero;
    public bool sheetMetal = true;
    private Vector3 objectDirection;
    private Vector3 previousPosition;
    private Vector3 newPosition;
    private bool collided = false;

    // Debug Only
    public int vertexToCheck = 0;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = this.GetComponent<Rigidbody>();
        parent = this.transform.parent.gameObject;
        Joint[] allJointsInChildren = parent.GetComponentsInChildren<Joint>();

        for (int i = 0; i < allJointsInChildren.Length; i++)
        {
            if (allJointsInChildren[i].connectedBody.Equals(rigidBody))
            {
                connectedJoints.Add(allJointsInChildren[i]);
            }
        }

        mesh = this.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

        meshManager = new MeshManager(mesh);
        connectedVertices = meshManager.GetConnectedVertices();

        weakestVertex = GetWeakestVertex();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        objectDirection = rigidBody.velocity;

        if(collided == false)
        {
            //Debug.Log(objectDirection);
        }
    }

    private Vector3 GetWeakestVertex()
    {
        // TODO: Incorporate joints into consideration

        return meshManager.GetClosestVertexToPoint(mesh.bounds.center, true, this.transform);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Handles.color = Color.blue;

        //Gizmos.DrawSphere(transform.TransformPoint(connectedVertices.ElementAt(vertexToCheck).Key), 0.005f);

        Gizmos.color = Color.green;
        //Gizmos.DrawSphere(transform.TransformPoint(weakestVertex), 0.01f);
        //for (int i = 0; i < connectedVertices.ElementAt(vertexToCheck).Value.Count; i++)
        //{
            //Gizmos.DrawSphere(transform.TransformPoint(connectedVertices.ElementAt(vertexToCheck).Value[i]), 0.005f);
        //}
    }

    private void OnCollisionEnter(Collision collision)
    {
        collided = true;
    }
}
