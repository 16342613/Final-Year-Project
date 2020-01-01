using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using HelperScripts.Tree;
using HelperScripts.Components;
using HelperScripts.Methods;
using Debugger;

public class BoundingVolumeHierarchy : MonoBehaviour
{
    GameObject attachedGameObject;
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    List<BoxCollider> colliders = new List<BoxCollider>();
    TreeStructure tree;
    MeshCollider objectMeshCollider;
    int objectMeshColliderLayer;
    Dictionary<Vector3, List<Vector3>> connectVertices;

    // In Development
    int maxCollidersRendered = 100000;
    int maxIterations = 2;

    // Debug Only
    public int vertexToCheck = 0;

    void Start()
    {
        attachedGameObject = this.gameObject;
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
        objectMeshColliderLayer = LayerMask.NameToLayer("ObjectMeshCollider");

        //ClearOldBVH();
        //ApplyBVH();
        //ApplyVertexColliders();

        //DebugHelper.PrintArray(vertices.ToList().Cast<object>().ToArray(), true);
        //DebugHelper.PrintArray(triangles.ToList().Cast<object>().ToArray());

        MeshManager meshManager = new MeshManager(mesh);
        connectVertices = meshManager.GetConnectedVertices();
        //DebugHelper.PrintListList((List<List<object>>) vertexDetails["trianglesInfo"]);
        //DebugHelper.PrintConnectedVertices(connectVertices, true);
    }

    void FixedUpdate()
    {
        //UpdateBVHPosition();

    }

    void ClearOldBVH()
    {
        List<BoxCollider> oldColliders = GetComponents<BoxCollider>().ToList();

        for (int i = 0; i < oldColliders.Count; i++)
        {
            Destroy(oldColliders[i]);
        }
    }

    public void ApplyVertexColliders()
    {
        if (maxCollidersRendered > vertices.Length)
        {
            maxCollidersRendered = vertices.Length;
        }

        for (int i = 0; i < maxCollidersRendered; i++)
        {
            BoxCollider collider = (BoxCollider)gameObject.AddComponent<BoxCollider>();
            collider.hideFlags = HideFlags.HideInInspector;
            collider.center = vertices[i];
            collider.size = new Vector3(0.04f, 0.04f, 0.04f);

            colliders.Add(collider);
        }
    }

    public void ApplyBVH()
    {
        if (maxCollidersRendered > vertices.Length)
        {
            maxCollidersRendered = vertices.Length;
        }

        BoxCollider rootCollider = (BoxCollider)gameObject.AddComponent<BoxCollider>();
        rootCollider.hideFlags = HideFlags.HideInInspector;
        rootCollider.center = mesh.bounds.center;
        rootCollider.size = mesh.bounds.size;
        BoxColliderID rootControllerItem = new BoxColliderID(rootCollider, 1);

        tree = new TreeStructure(new Node(null, rootCollider));  // Create the BVH tree

        for (int i = 0; i < maxCollidersRendered; i++)
        {
            BoxCollider collider = (BoxCollider)gameObject.AddComponent<BoxCollider>();
            collider.hideFlags = HideFlags.HideInInspector;
            collider.center = vertices[i];
            collider.size = new Vector3(0.005f, 0.005f, 0.005f);
            colliders.Add(collider);
        }

        GameObject meshChild = new GameObject();
        meshChild.transform.parent = gameObject.transform;
        meshChild.name = "Mesh Collider Container";
        meshChild.layer = objectMeshColliderLayer;
        objectMeshCollider = meshChild.AddComponent<MeshCollider>();
        meshChild.AddComponent<UnderlyingMeshCollision>();
        objectMeshCollider.sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        objectMeshCollider.transform.position = transform.position;
        objectMeshCollider.transform.rotation = transform.rotation;
        objectMeshCollider.transform.localScale = new Vector3(1, 1, 1);

        RecursivelySplitBVH(rootCollider, 0);
    }

    void RecursivelySplitBVH(BoxCollider parentCollider, int currentDepth)
    {
        if (currentDepth <= maxIterations)
        {
            Vector3 parentCenter = parentCollider.center;
            Vector3 parentSize = parentCollider.size;

            BoxCollider childCollider = (BoxCollider)gameObject.AddComponent<BoxCollider>();
            childCollider.hideFlags = HideFlags.HideInInspector;
            childCollider.center = new Vector3(parentCenter.x + 0.25f * parentSize.x, parentCenter.y + 0.25f * parentSize.y, parentCenter.z);
            childCollider.size = new Vector3(0.5f * parentSize.x, 0.5f * parentSize.y, parentSize.z);

            RecursivelySplitBVH(childCollider, currentDepth + 1);
        }
    }

    void UpdateBVHPosition()
    {
        colliders = GetComponents<BoxCollider>().ToList();
        vertices = mesh.vertices;
        triangles = mesh.triangles;

        for (int i = 0; i < colliders.Count; i++)
        {
            colliders[i].center = vertices[i];
        }
    }

    #region Getters

    public Dictionary<Vector3, List<Vector3>> GetConnectedVertices()
    {
        return connectVertices;
    }

    #endregion

    private void OnCollisionEnter(Collision collision)
    {

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Handles.color = Color.blue;

        /*for (int i = 0; i < colliders.Count; i++)
        {
            Vector3 vertex = transform.TransformPoint(colliders[i].center);

            Gizmos.DrawSphere(transform.TransformPoint(colliders[i].center), 0.005f);
            Handles.Label(new Vector3(vertex.x + 0.05f, vertex.y + 0.05f, vertex.z + 0.05f), i.ToString());
        }*/

        /*
        Gizmos.DrawSphere(transform.TransformPoint(connectVertices.ElementAt(vertexToCheck).Key), 0.005f);

        Gizmos.color = Color.green;
        for (int i = 0; i < connectVertices.ElementAt(vertexToCheck).Value.Count; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(connectVertices.ElementAt(vertexToCheck).Value[i]), 0.005f);
        }*/
    }
}
