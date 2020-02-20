using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelperScripts.Methods;
using UnityEngine;

public class TestMeshRayCast : MonoBehaviour
{
    private MeshCollider meshCollider;
    public Vector3 origin;
    private Vector3[] directions = new Vector3[6];
    private Dictionary<Vector3, List<Vector3>> connectedVertices;
    private Mesh mesh;
    public int index = 1;
    List<List<int>> meshTriangles;

    // Start is called before the first frame update
    void Start()
    {
        mesh = this.GetComponent<MeshFilter>().mesh;
        //mesh.triangles = mesh.triangles.Reverse().ToArray();
        meshCollider = GetComponent<MeshCollider>();
        origin = meshCollider.bounds.center; //+ new Vector3(1, 0, 0);

        MeshManager meshManager = new MeshManager(this.GetComponent<MeshFilter>());
        connectedVertices = meshManager.GetConnectedVertices();

        DrawBoxColliders();
        //meshTriangles = meshManager.DrawColliders();
    }

    // Update is called once per frame
    void Update()
    {
        DoRayCast();
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
                Debug.Log(hit[i].collider.gameObject.name);
            }
        }
    }

    private void DrawBoxColliders()
    {
        List<Vector3> remainingVertices = mesh.vertices.ToList();

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
        boxCollider.size = new Vector3(1, 1, 0.1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(origin, 0.1f);

        for (int i = 0; i < 6; i++)
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
        }
    }
}
