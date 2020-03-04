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
    public int index = 13;
    public int testIndex = 0;
    List<List<int>> meshTriangles;
    List<List<int>> colliderTriangles;

    public List<List<Vector3>> triangleDetails;
    private List<List<Vector3>> colliderVertices = new List<List<Vector3>>();
    List<BoxCollider> colliders = new List<BoxCollider>();
    public float colliderHeight = 0.01f;
    private MeshManager meshManager;

    private List<Vector3> colliderVector;
    private List<Vector3> sideVector;
    private List<Vector3> extraVector;

    // Start is called before the first frame update
    void Start()
    {
        mesh = this.GetComponent<MeshFilter>().mesh;
        //mesh.triangles = mesh.triangles.Reverse().ToArray();
        //meshCollider = GetComponent<MeshCollider>();
        //origin = meshCollider.bounds.center; //+ new Vector3(1, 0, 0);

        meshManager = new MeshManager(this.GetComponent<MeshFilter>());
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
        //Debug.Log(p.transform.localPosition);
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
        child.name = "Colliders";
        //child.hideFlags = HideFlags.HideInHierarchy;
        child.transform.position = transform.position;
        child.transform.parent = this.gameObject.transform;

        for (int i = 0; i < colliderTriangles.Count; i++)
        {
            GameObject intermediateObject = new GameObject();
            intermediateObject.name = "Intermediate - " + i;
            //intermediateObject.hideFlags = HideFlags.HideInHierarchy;
            intermediateObject.transform.parent = child.transform;

            colliderVertices.Add(new List<Vector3>());
            colliderVertices[i].AddRange(triangleDetails[colliderTriangles[i][0]]);
            colliderVertices[i].AddRange(triangleDetails[colliderTriangles[i][1]]);
            List<Vector3> colliderVerticesCopy = colliderVertices[i].ConvertAll(vertex => new Vector3(vertex.x, vertex.y, vertex.z));
            List<Vector3> connectedNodes = colliderVertices[i].GroupBy(s => s).SelectMany(grp => grp.Skip(1)).Distinct().ToList();
            List<Vector3> unconnectedNodes = colliderVerticesCopy.Except(connectedNodes).ToList();
            colliderVertices[i] = colliderVertices[i].Distinct().ToList();
            Vector3 boxColliderCentre = Vector3.zero;

            for (int j = 0; j < 4; j++)
            {
                boxColliderCentre += intermediateObject.transform.TransformPoint(colliderVertices[i][j]);
            }

            boxColliderCentre = boxColliderCentre / 4;

            if (i == 281) origin = intermediateObject.transform.TransformPoint((colliderVertices[i][1] + colliderVertices[i][2]) / 2);
            Vector3 midpoint = intermediateObject.transform.TransformPoint((colliderVertices[i][1] + colliderVertices[i][2]) / 2);
            intermediateObject.transform.localPosition = Vector3.zero;

            GameObject colliderContainer = new GameObject();
            colliderContainer.name = "ColliderContainer";
            //colliderContainer.hideFlags = HideFlags.HideInHierarchy;
            colliderContainer.transform.parent = intermediateObject.transform;
            colliderContainer.transform.position = transform.TransformPoint(boxColliderCentre);

            Vector3 averageNormal = (mesh.normals[meshTriangles[colliderTriangles[i][0]][0]] +
                                     mesh.normals[meshTriangles[colliderTriangles[i][0]][1]] +
                                     mesh.normals[meshTriangles[colliderTriangles[i][1]][0]] +
                                     mesh.normals[meshTriangles[colliderTriangles[i][1]][1]]) / 4;
            colliderContainer.transform.rotation = Quaternion.FromToRotation(colliderContainer.transform.forward, averageNormal);

            BoxCollider boxCollider = colliderContainer.AddComponent<BoxCollider>();
            //boxCollider.hideFlags = HideFlags.HideInInspector;
            colliderContainer.transform.localRotation = Quaternion.Euler(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);

            if (i == 34)
            {

            }

            Vector3 yVector = intermediateObject.transform.TransformDirection(unconnectedNodes[0] - connectedNodes[0]).normalized;
            Vector3 xVector = intermediateObject.transform.TransformDirection(connectedNodes[1] - unconnectedNodes[0]).normalized;
            Vector3 colliderUp = colliderContainer.transform.TransformDirection(colliderContainer.transform.up).normalized;
            //colliderVector = new List<Vector3> { colliderContainer.transform.position, colliderUp };
            //sideVector = new List<Vector3> { unconnectedNodes[0], xVector };
            float value = Vector3.Dot(colliderContainer.transform.TransformDirection(colliderContainer.transform.up).normalized, colliderUp);

            //float boxColliderSizeX = (Vector3.Distance(colliderVertices[i][0], colliderVertices[i][1]) + Vector3.Distance(colliderVertices[i][2], colliderVertices[i][3])) / 2;
            //float boxColliderSizeY = (Vector3.Distance(colliderVertices[i][1], colliderVertices[i][2]) + Vector3.Distance(colliderVertices[i][3], colliderVertices[i][0])) / 2;
            float boxColliderSizeX = (Vector3.Distance(connectedNodes[0], unconnectedNodes[0]) + Vector3.Distance(connectedNodes[1], unconnectedNodes[1])) / 2;
            float boxColliderSizeY = (Vector3.Distance(connectedNodes[1], unconnectedNodes[0]) + Vector3.Distance(connectedNodes[0], unconnectedNodes[1])) / 2;

            Vector3 vec1 = intermediateObject.transform.TransformDirection(unconnectedNodes[0] - connectedNodes[0]);
            Vector3 vec2 = intermediateObject.transform.TransformDirection(connectedNodes[1] - unconnectedNodes[0]);
            Vector3 vec3 = intermediateObject.transform.TransformDirection(unconnectedNodes[1] - connectedNodes[1]);
            Vector3 vec4 = intermediateObject.transform.TransformDirection(connectedNodes[0] - unconnectedNodes[1]);

            if (i == 34)
            {
                // green
                colliderVector = new List<Vector3> { Vector3.zero, this.transform.TransformDirection(boxCollider.transform.up) };
                // blue
                sideVector = new List<Vector3> { Vector3.zero, this.transform.TransformDirection(boxCollider.transform.forward) };
                // red
                extraVector = new List<Vector3> { Vector3.zero, this.transform.TransformDirection(boxCollider.transform.right) };
                Debug.Log(Vector3.Dot(colliderVector[1], sideVector[1]));
            }

            List<Vector3> sideVectors = new List<Vector3> { vec1, vec2, vec3, vec4 };
            List<float> slopeDirections = new List<float> { Mathf.Abs(Vector3.Dot(vec1.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec2.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec3.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec4.normalized, this.transform.TransformDirection(colliderContainer.transform.up))) };

            List<float> xSidesLengths = new List<float>();
            List<float> ySidesLengths = new List<float>();

            for (int j = 0; j < 4; j++)
            {
                if (slopeDirections[j] > 0.5f)
                {
                    ySidesLengths.Add(sideVectors[j].magnitude);
                }
                else if (slopeDirections[j] < 0.5f)
                {
                    xSidesLengths.Add(sideVectors[j].magnitude);
                }
            }

            //boxCollider.size = new Vector3(boxColliderSizeX, boxColliderSizeY, colliderHeight);
            boxCollider.size = new Vector3((xSidesLengths[0] + xSidesLengths[1]) / 2, (ySidesLengths[0] + ySidesLengths[1]) / 2, colliderHeight);

            Vector3 x1 = colliderVertices[i][0] - colliderVertices[i][1];
            Vector3 y1 = colliderVertices[i][0] - colliderVertices[i][3];
            Vector3 x2 = colliderVertices[i][3] - colliderVertices[i][2];
            Vector3 y2 = colliderVertices[i][1] - colliderVertices[i][2];
            float colliderDirection = 90 - Vector3.Angle(x1, y1);
            colliderDirection += 90 - Vector3.Angle(x2, y2);
            colliderDirection = colliderDirection / 2;

            colliderContainer.transform.localRotation = Quaternion.Euler(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);
            //colliderContainer.transform.RotateAround(transform.TransformPoint(boxColliderCentre), Vector3.forward, colliderDirection);

            //float angle = Vector3.Angle(transform.forward, target.position);
            //olliderContainer.transform.rotation = Quaternion.FromToRotation(colliderContainer.transform.right, transform.TransformDirection(x1));
            colliders.Add(boxCollider);

            //intermediateObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(colliderVector[0], colliderVector[1]);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(sideVector[0], sideVector[1]);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(extraVector[0], extraVector[1]);
        /*for (int i = 0; i < mesh.vertexCount; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(mesh.vertices[i]), transform.TransformPoint(mesh.vertices[i] + mesh.normals[i]));
        }*/

        //Gizmos.DrawSphere(transform.TransformPoint(origin), 0.03f);

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

        Gizmos.color = Color.red;
        List<int> stronglyConnected = colliderTriangles[index];

        for (int i = 0; i < stronglyConnected.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[meshTriangles[stronglyConnected[i]][j]]), 0.01f);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(colliderVertices[13][testIndex]), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(colliderVertices[testIndex][1]), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(colliderVertices[testIndex][2]), 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(colliderVertices[testIndex][3]), 0.01f);

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
                    //Gizmos.DrawCube(transform.TransformPoint(mesh.vertices[meshTriangles[colliderTriangles[i][j]][k]]), new Vector3(0.05f, 0.05f, 0.05f));
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
        DebugHelper.PrintList(notDrawnInts);*/

        /*Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[0]), 0.05f);
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[1]), 0.05f);
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[2]), 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[3]), 0.05f);
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[4]), 0.05f);
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[5]), 0.05f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(meshManager.testVertices[testIndex]), 0.05f);*/
    }
}
