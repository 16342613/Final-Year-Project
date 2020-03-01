using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Debugger;
using HelperScripts.Methods;
using UnityEngine;

public class TestMeshRayCast : MonoBehaviour
{
    private MeshCollider meshCollider;
    public Vector3 origin;
    private Vector3[] directions = new Vector3[6];
    private Dictionary<Vector3, List<Vector3>> connectedVertices;
    private Mesh mesh;
    public int index = 0;
    List<List<int>> meshTriangles;
    List<List<int>> colliderTriangles;

    public List<List<Vector3>> triangleDetails;
    private List<List<Vector3>> colliderVertices = new List<List<Vector3>>();
    List<BoxCollider> colliders = new List<BoxCollider>();
    public float colliderHeight = 0.01f;
    private Vector3 initialPosition;

    private float fpsTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        mesh = this.GetComponent<MeshFilter>().mesh;
        //mesh.triangles = mesh.triangles.Reverse().ToArray();
        //meshCollider = GetComponent<MeshCollider>();
        //origin = meshCollider.bounds.center; //+ new Vector3(1, 0, 0);

        MeshManager meshManager = new MeshManager(this.GetComponent<MeshFilter>());
        connectedVertices = meshManager.GetConnectedVertices();

        meshTriangles = meshManager.GetMeshTriangles();
        colliderTriangles = meshManager.GetStronglyConnectedTriangles();

        triangleDetails = meshManager.triangleDetails;
        DrawColliders();
    }

    // Update is called once per frame
    void Update()
    {
        //DoRayCast();
    }

    private void DoRayCast()
    {
        RaycastHit[] hit = new RaycastHit[6];

        for (int i = 0; i < 6; i++)
        {
            Vector3 direction = Vector3.zero;

            switch (i)
            {
                case 0:
                    direction = transform.forward;
                    break;

                case 1:
                    direction = transform.up;
                    break;

                case 2:
                    direction = -transform.forward;
                    break;

                case 3:
                    direction = -transform.up;
                    break;

                case 4:
                    direction = transform.right;
                    break;

                case 5:
                    direction = -transform.right;
                    break;
            }

            directions[i] = direction;

            if (Physics.Raycast(origin, direction, out hit[i]))
            {
                //Debug.Log(hit[i].collider.gameObject.name);
            }
        }
    }

    public void DrawColliders()
    {
        GameObject child = new GameObject();
        //child.hideFlags = HideFlags.HideInHierarchy;
        initialPosition = this.gameObject.transform.position;
        child.transform.parent = this.gameObject.transform;

        for (int i = 0; i < 30; i++)
        {
            colliderVertices.Add(new List<Vector3>());
            colliderVertices[i].AddRange(triangleDetails[colliderTriangles[i][0]]);
            colliderVertices[i].AddRange(triangleDetails[colliderTriangles[i][1]]);
            colliderVertices[i] = colliderVertices[i].Distinct().ToList();
            Vector3 boxColliderCentre = Vector3.zero;

            for (int j = 0; j < 4; j++)
            {
                boxColliderCentre += colliderVertices[i][j];
            }

            boxColliderCentre = boxColliderCentre / 4;
            origin = boxColliderCentre;

            GameObject colliderContainer = new GameObject();
            //colliderContainer.hideFlags = HideFlags.HideInHierarchy;
            colliderContainer.transform.parent = child.transform;
            colliderContainer.transform.position = this.transform.position;

            BoxCollider boxCollider = colliderContainer.AddComponent<BoxCollider>();
            //boxCollider.hideFlags = HideFlags.HideInInspector;
            boxCollider.center = boxColliderCentre;

            float boxColliderSizeX = (Vector3.Distance(colliderVertices[i][0], colliderVertices[i][1]) + Vector3.Distance(colliderVertices[i][2], colliderVertices[i][3])) / 2;
            float boxColliderSizeY = (Vector3.Distance(colliderVertices[i][1], colliderVertices[i][2]) + Vector3.Distance(colliderVertices[i][3], colliderVertices[i][0])) / 2;
            boxCollider.size = new Vector3(boxColliderSizeX, boxColliderSizeY, colliderHeight);

            Vector3 x = colliderVertices[i][0] - colliderVertices[i][1];
            Vector3 y = colliderVertices[i][0] - colliderVertices[i][3];
            float colliderDirection = 90 - Vector3.Angle(x, y);

            Debug.Log(colliderDirection);
            boxCollider.transform.RotateAround(transform.TransformPoint(boxColliderCentre), Vector3.forward, colliderDirection);
            colliders.Add(boxCollider);

            //colliderContainer.SetActive(false);
        }
    }

    private void DrawBoxColliders()
    {
        /*List<Vector3> remainingVertices = mesh.vertices.ToList();

        //while (remainingVertices.Count > 0) {

        Vector3 currentVertex = remainingVertices[0];
        List<Vector3> verticesConnectedToCurrent = connectedVertices[currentVertex];
        List<Vector3> boxColliderVertices = new List<Vector3>();

        boxColliderVertices.Add(currentVertex);

        string toPrint = "";
        for (int i = 0; i < verticesConnectedToCurrent.Count; i++)
        {
            toPrint += "Index: " + i + " Point: " + verticesConnectedToCurrent[i] + "; ";
            boxColliderVertices.Add(verticesConnectedToCurrent[i]);
        }

        Vector3 boxColliderCentre = Vector3.zero;


        for (int i = 0; i < boxColliderVertices.Count - 1; i++)
        {
            boxColliderCentre += boxColliderVertices[i];
            remainingVertices.Remove(boxColliderVertices[i]);
        }

        boxColliderCentre = boxColliderCentre / 4;

        BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
        boxCollider.hideFlags = HideFlags.HideInInspector;
        boxCollider.center = boxColliderCentre;
        boxCollider.size = new Vector3(1, 1, 0.1f);*/
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        /*for (int i = 0; i < 100; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(mesh.vertices[i]), transform.TransformPoint(mesh.vertices[i] + mesh.normals[i]));
        }*/

        //Gizmos.DrawSphere(transform.TransformPoint(origin), 0.05f);

        /*for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(origin, origin + directions[i]);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[index]), 0.05f);

        Gizmos.color = Color.blue;
        List<Vector3> verticesConnectedToQueryPoint = connectedVertices[mesh.vertices[index]];

        for (int i = 0; i < connectedVertices[mesh.vertices[index]].Count; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(verticesConnectedToQueryPoint[i]), 0.05f);
        }*/

        /*List<int> stronglyConnected = colliderTriangles[index];

         for (int i = 0; i < stronglyConnected.Count; i++)
         {
             for (int j = 0; j < 3; j++)
             {
                 Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[meshTriangles[stronglyConnected[i]][j]]), 0.05f);
             }
         }*/

        //Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[312]), 0.05f);
        //Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[311]), 0.05f);
        /*List<Vector3> notDrawn = mesh.vertices.ToList();
        List<int> notDrawnInts = new List<int>();

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            notDrawnInts.Add(i);
        }

        for (int i = 0; i < colliderTriangles.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Gizmos.DrawCube(transform.TransformPoint(mesh.vertices[meshTriangles[colliderTriangles[i][j]][k]]), new Vector3(0.05f, 0.05f, 0.05f));
                    notDrawn.Remove(mesh.vertices[meshTriangles[colliderTriangles[i][j]][k]]);
                    notDrawnInts.Remove(meshTriangles[colliderTriangles[i][j]][k]);
                }

            }
        }

        Gizmos.color = Color.blue;
        for (int i = 0; i < notDrawn.Count; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(notDrawn[i]), 0.03f);
        }

        Debug.Log(notDrawn.Count);
        DebugHelper.PrintList(notDrawnInts)*/;
    }
}
