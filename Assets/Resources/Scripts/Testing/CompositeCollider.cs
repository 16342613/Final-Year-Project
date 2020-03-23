using HelperScripts.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompositeCollider : MonoBehaviour
{
    private MeshCollider meshCollider;
    public Vector3 origin;
    private Vector3[] directions = new Vector3[6];
    private Dictionary<Vector3, List<Vector3>> connectedVertices;
    private Mesh mesh;
    public int index = 0;
    public int testIndex = 0;
    public List<List<int>> meshTriangles;
    public List<List<int>> colliderTriangles;
    public List<List<int>> squareVertices;
    public List<List<int>> connectedSquareNodes;
    public List<List<int>> unconnectedSquareNodes;

    public List<List<Vector3>> triangleDetails;
    public List<GameObject> intermediateObjects = new List<GameObject>();
    public List<GameObject> colliderContainers = new List<GameObject>();
    public List<BoxCollider> colliders = new List<BoxCollider>();
    public bool ready = false;
    private float colliderHeight = 0.0001f;
    private MeshManager meshManager;
    public Dictionary<int, List<int>> vertexSquareMapping;
    private BVH_Tree bvhTree;
    public bool finishedRoutine = true;

    #region Compute shader variables

    private ComputeShader computeShader;

    private Vector3[] debugArray;
    private ComputeBuffer debugBuffer;

    private int[,] squareVerticesConverted;
    private ComputeBuffer squareVerticesBuffer;

    private ComputeBuffer meshVerticesBuffer;

    private Matrix4x4[] IM_TRSMs;  // Intermediate Object Translate Rotate Scale Matrices
    private ComputeBuffer IM_TRSM_Buffer;

    private ComputeBuffer meshNormalsBuffer;

    private int[] collidersToUpdate;
    private ComputeBuffer collidersToUpdateBuffer;

    private Matrix4x4[] returnDetails;
    private ComputeBuffer returnDetailsBuffer;

    private Matrix4x4[] CC_TRSMs;  // Collider Container Object Translate Rotate Scale Matrices
    private ComputeBuffer CC_TRSM_Buffer;

    private Matrix4x4[] CC_Local_TRSMs;  // Collider Container Object Translate Rotate Scale Matrices
    private ComputeBuffer CC_Local_TRSM_Buffer;

    private int[,] connectedSquareNodesConverted;
    private ComputeBuffer connectedSquareNodes_Buffer;

    private int[,] unconnectedSquareNodesConverted;
    private ComputeBuffer unconnectedSquareNodes_Buffer;

    private int[,] sidesXConverted;
    private ComputeBuffer sidesX;

    private int[,] sidesYConverted;
    private ComputeBuffer sidesY;

    private float[] returnRotationAngles;
    private ComputeBuffer returnRotationAngles_Buffer;

    private Vector3[] returnColliderSizes;
    private ComputeBuffer returnColliderSizes_Buffer;

    private bool done = false;
    private List<Vector3[]> localColliderTransforms = new List<Vector3[]>();
    private List<object> locals = new List<object>();
    private float totalTime = 0;
    #endregion

    List<_SideOrder> sideOrders = new List<_SideOrder>();

    struct _SideOrder
    {
        public List<int> x;
        public List<int> y;
    }

    // Start is called before the first frame update
    void Start()
    {
        computeShader = Resources.Load<ComputeShader>("Shaders/DeformationShader");

        mesh = this.GetComponent<MeshFilter>().mesh;
        //mesh.triangles = mesh.triangles.Reverse().ToArray();
        //meshCollider = GetComponent<MeshCollider>();
        //origin = meshCollider.bounds.center; //+ new Vector3(1, 0, 0);

        meshManager = new MeshManager(this.GetComponent<MeshFilter>(), this.gameObject.name);
        connectedVertices = meshManager.GetConnectedVertices();

        meshTriangles = meshManager.GetMeshTriangles();
        colliderTriangles = meshManager.GetStronglyConnectedTriangles();

        triangleDetails = meshManager.triangleDetails;
        //InvokeRepeating("DrawColliders", 0f, 2f);
        squareVertices = meshManager.squareVertices;
        connectedSquareNodes = meshManager.connectedSquareNodes;
        unconnectedSquareNodes = meshManager.unconnectedSquareNodes;
        vertexSquareMapping = meshManager.vertexSquareMapping;
        DrawCollidersInitial();

        squareVerticesConverted = new int[squareVertices.Count, 4];
        for (int i = 0; i < squareVertices.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                squareVerticesConverted[i, j] = squareVertices[i][j];
            }
        }

        squareVerticesBuffer = new ComputeBuffer(squareVerticesConverted.Length, sizeof(int));
        squareVerticesBuffer.SetData(squareVerticesConverted);
        computeShader.SetBuffer(computeShader.FindKernel("Main"), Shader.PropertyToID("squareVertices"), squareVerticesBuffer);

        unconnectedSquareNodesConverted = new int[unconnectedSquareNodes.Count, 2];
        for (int i = 0; i < unconnectedSquareNodes.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                unconnectedSquareNodesConverted[i, j] = unconnectedSquareNodes[i][j];
            }
        }

        unconnectedSquareNodes_Buffer = new ComputeBuffer(unconnectedSquareNodesConverted.Length, sizeof(int));
        unconnectedSquareNodes_Buffer.SetData(unconnectedSquareNodesConverted);
        computeShader.SetBuffer(computeShader.FindKernel("Main2"), Shader.PropertyToID("unconnectedSquareNodes"), unconnectedSquareNodes_Buffer);

        connectedSquareNodesConverted = new int[connectedSquareNodes.Count, 2];
        for (int i = 0; i < connectedSquareNodes.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                connectedSquareNodesConverted[i, j] = connectedSquareNodes[i][j];
            }
        }

        connectedSquareNodes_Buffer = new ComputeBuffer(connectedSquareNodesConverted.Length, sizeof(int));
        connectedSquareNodes_Buffer.SetData(connectedSquareNodesConverted);
        computeShader.SetBuffer(computeShader.FindKernel("Main2"), Shader.PropertyToID("connectedSquareNodes"), connectedSquareNodes_Buffer);

        ready = true;
    }

    // Update is called once per frame
    void Update()
    {
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        List<int> ctu = new List<int> { 10 };
        /*for (int i = 0; i < 1; i++)
        {
            ctu.Add(i);
        }*/

        triangleDetails = meshManager.RecalculateTriangleDetails(mesh);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            for (int i = 0; i < ctu.Count; i++)
            {
                UpdateCollider(ctu[i]);
            }
            Debug.Log("CPU : " + totalTime);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            UpdateColliderGPU(ctu);
            sw.Stop();
            Debug.Log("GPU : " + sw.Elapsed.TotalMilliseconds);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            DrawColliders();
        }

        //if (done == false) UpdateColliderGPU(null);
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

    public void DrawCollidersInitial()
    {
        Vector3 objectScale = this.transform.localScale;
        Vector3 objectRotation = this.transform.rotation.eulerAngles;
        GameObject child = new GameObject();
        child.name = "Colliders";
        //child.hideFlags = HideFlags.HideInHierarchy;
        child.transform.position = transform.position;
        child.transform.parent = this.gameObject.transform;

        for (int i = 0; i < colliderTriangles.Count; i++)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);    // Helps with rotation calculations, we revert this when finished
            GameObject intermediateObject = new GameObject();       // Helps to separate parent-child rotations
            intermediateObject.name = "Intermediate - " + i;
            //intermediateObject.hideFlags = HideFlags.HideInHierarchy;
            intermediateObject.transform.parent = child.transform;

            Vector3 boxColliderCentre = Vector3.zero;

            for (int j = 0; j < 4; j++)
            {
                boxColliderCentre += intermediateObject.transform.TransformPoint(mesh.vertices[squareVertices[i][j]]);
            }

            boxColliderCentre = boxColliderCentre / 4;
            locals.Add(boxColliderCentre);
            intermediateObject.transform.localPosition = Vector3.zero;

            GameObject colliderContainer = new GameObject();
            colliderContainer.name = "ColliderContainer";
            //colliderContainer.hideFlags = HideFlags.HideInHierarchy;
            colliderContainer.transform.parent = intermediateObject.transform;
            colliderContainer.transform.position = this.transform.TransformPoint(boxColliderCentre);

            Vector3 averageNormal = (mesh.normals[squareVertices[i][0]] +
                                     mesh.normals[squareVertices[i][1]] +
                                     mesh.normals[squareVertices[i][2]] +
                                     mesh.normals[squareVertices[i][3]]) / 4;

            colliderContainer.transform.rotation = Quaternion.FromToRotation(colliderContainer.transform.forward, averageNormal);
            Vector3[] arr = new Vector3[] { colliderContainer.transform.position, colliderContainer.transform.rotation.eulerAngles, colliderContainer.transform.localScale };
            localColliderTransforms.Add((Vector3[])arr.Clone());

            BoxCollider boxCollider = colliderContainer.AddComponent<BoxCollider>();

            colliderContainer.transform.localRotation = Quaternion.Euler(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);

            Vector3 vec1 = this.transform.TransformDirection(mesh.vertices[unconnectedSquareNodes[i][0]] - mesh.vertices[connectedSquareNodes[i][0]]);
            Vector3 vec2 = this.transform.TransformDirection(mesh.vertices[connectedSquareNodes[i][1]] - mesh.vertices[unconnectedSquareNodes[i][0]]);
            Vector3 vec3 = this.transform.TransformDirection(mesh.vertices[unconnectedSquareNodes[i][1]] - mesh.vertices[connectedSquareNodes[i][1]]);
            Vector3 vec4 = this.transform.TransformDirection(mesh.vertices[connectedSquareNodes[i][0]] - mesh.vertices[unconnectedSquareNodes[i][1]]);

            Vector3 averageSideVector = (vec2 - vec4).normalized;

            if ((vec2 - vec4).magnitude < (vec1 - vec3).magnitude)
            {
                //averageSideVector = (vec1 - vec3).normalized;
            }

            Vector3 correctColliderUp = Vector3.Cross(averageSideVector, this.transform.TransformDirection(colliderContainer.transform.forward));
            Vector3 currentColliderUp = this.transform.TransformDirection(colliderContainer.transform.up);
            float rotationAngle = Vector3.SignedAngle(correctColliderUp, currentColliderUp, this.transform.TransformDirection(boxCollider.transform.forward));
            colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x, colliderContainer.transform.localEulerAngles.y, -rotationAngle);

            List<Vector3> sideVectors = new List<Vector3> { vec1, vec2, vec3, vec4 };
            List<float> slopeDirections = new List<float> { Mathf.Abs(Vector3.Dot(vec1.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec2.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec3.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec4.normalized, this.transform.TransformDirection(colliderContainer.transform.up))) };

            List<float> xSidesLengths = new List<float>();
            List<float> ySidesLengths = new List<float>();

            _SideOrder sideOrder = new _SideOrder();
            sideOrder.x = new List<int>();
            sideOrder.y = new List<int>();
            sideOrders.Add(sideOrder);

            for (int j = 0; j < 4; j++)
            {
                // 1 x 1 x cos(45) = 0.707107
                if (slopeDirections[j] > 0.707107f)
                {
                    ySidesLengths.Add(sideVectors[j].magnitude);
                    sideOrders[i].y.Add(j);
                }
                else if (slopeDirections[j] < 0.707107f)
                {
                    xSidesLengths.Add(sideVectors[j].magnitude);
                    sideOrders[i].x.Add(j);
                }
            }

            boxCollider.size = new Vector3(((xSidesLengths[0] + xSidesLengths[1]) / 2) * objectScale.x, ((ySidesLengths[0] + ySidesLengths[1]) / 2) * objectScale.y, colliderHeight);
            intermediateObjects.Add(intermediateObject);
            colliderContainers.Add(colliderContainer);
            colliders.Add(boxCollider);
            //List<Vector3> boxColliderVertices = new List<Vector3> { transform};


            //intermediateObject.SetActive(false);
        }

        this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);

        sidesXConverted = new int[colliderTriangles.Count, 2];
        sidesYConverted = new int[colliderTriangles.Count, 2];
        for (int i = 0; i < sideOrders.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                sidesXConverted[i, j] = sideOrders[i].x[j];
                sidesYConverted[i, j] = sideOrders[i].y[j];
            }
        }
    }

    /*public void DrawCollidersInitial()
    {
        Vector3 objectScale = Vector3.one;//this.transform.localScale;
        Vector3 objectRotation = this.transform.rotation.eulerAngles;
        GameObject child = new GameObject();
        child.name = "Colliders";
        //child.hideFlags = HideFlags.HideInHierarchy;
        child.transform.position = transform.position;
        child.transform.parent = this.gameObject.transform;

        // Loop through all of the 'squarely connected' triangles
        for (int i = 0; i < colliderTriangles.Count; i++)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);    // Helps with rotation calculations, we revert this when finished
            GameObject intermediateObject = new GameObject();       // Helps to separate parent-child rotations
            intermediateObject.name = "Intermediate - " + i;
            //intermediateObject.hideFlags = HideFlags.HideInHierarchy;
            intermediateObject.transform.parent = child.transform;

            // FROM HERE
            List<Vector3> colliderVertices = new List<Vector3>();

            colliderVertices.AddRange(triangleDetails[colliderTriangles[i][0]]);
            colliderVertices.AddRange(triangleDetails[colliderTriangles[i][1]]);
            List<Vector3> colliderVerticesCopy = colliderVertices.ConvertAll(vertex => new Vector3(vertex.x, vertex.y, vertex.z));
            List<Vector3> connectedNodes = colliderVertices.GroupBy(s => s).SelectMany(grp => grp.Skip(1)).Distinct().ToList();
            List<Vector3> unconnectedNodes = colliderVerticesCopy.Except(connectedNodes).ToList();
            colliderVertices = colliderVertices.Distinct().ToList();
            Vector3 boxColliderCentre = Vector3.zero;

            for (int j = 0; j < 4; j++)
            {
                boxColliderCentre += intermediateObject.transform.TransformPoint(colliderVertices[j]);
            }

            // 81

            boxColliderCentre = boxColliderCentre / 4;
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

            colliderContainer.transform.localRotation = Quaternion.Euler(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);

            Vector3 vec1 = this.transform.TransformDirection(unconnectedNodes[0] - connectedNodes[0]);
            Vector3 vec2 = this.transform.TransformDirection(connectedNodes[1] - unconnectedNodes[0]);
            Vector3 vec3 = this.transform.TransformDirection(unconnectedNodes[1] - connectedNodes[1]);
            Vector3 vec4 = this.transform.TransformDirection(connectedNodes[0] - unconnectedNodes[1]);

            Vector3 averageSideVector = (vec2 - vec4).normalized;

            if ((vec2 - vec4).magnitude < (vec1 - vec3).magnitude)
            {
                //averageSideVector = (vec1 - vec3).normalized;
            }

            Vector3 correctColliderUp = Vector3.Cross(averageSideVector, this.transform.TransformDirection(colliderContainer.transform.forward));
            Vector3 currentColliderUp = this.transform.TransformDirection(colliderContainer.transform.up);
            float rotationAngle = Vector3.SignedAngle(correctColliderUp, currentColliderUp, this.transform.TransformDirection(boxCollider.transform.forward));
            colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x, colliderContainer.transform.localEulerAngles.y, -rotationAngle);

            List<Vector3> sideVectors = new List<Vector3> { vec1, vec2, vec3, vec4 };
            List<float> slopeDirections = new List<float> { Mathf.Abs(Vector3.Dot(vec1.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec2.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec3.normalized, this.transform.TransformDirection(colliderContainer.transform.up))),
                                                            Mathf.Abs(Vector3.Dot(vec4.normalized, this.transform.TransformDirection(colliderContainer.transform.up))) };

            List<float> xSidesLengths = new List<float>();
            List<float> ySidesLengths = new List<float>();

            _SideOrder sideOrder = new _SideOrder();
            sideOrder.x = new List<int>();
            sideOrder.y = new List<int>();
            sideOrders.Add(sideOrder);

            for (int j = 0; j < 4; j++)
            {
                // 1 x 1 x cos(45) = 0.707107
                if (slopeDirections[j] > 0.707107f)
                {
                    ySidesLengths.Add(sideVectors[j].magnitude);
                    sideOrders[i].y.Add(j);
                }
                else if (slopeDirections[j] < 0.707107f)
                {
                    xSidesLengths.Add(sideVectors[j].magnitude);
                    sideOrders[i].x.Add(j);
                }
            }

            boxCollider.size = new Vector3(((xSidesLengths[0] + xSidesLengths[1]) / 2) * objectScale.x, ((ySidesLengths[0] + ySidesLengths[1]) / 2) * objectScale.y, colliderHeight);
            colliders.Add(boxCollider);
            //List<Vector3> boxColliderVertices = new List<Vector3> { transform};


            //intermediateObject.SetActive(false);
        }

        this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);
    }*/

    public void DrawColliders()
    {
        Vector3 objectScale = Vector3.one;//this.transform.localScale;
        Vector3 objectRotation = this.transform.rotation.eulerAngles;
        triangleDetails = meshManager.RecalculateTriangleDetails(mesh);
        colliders.Clear();

        try
        {
            Destroy(this.transform.Find("Colliders").gameObject);
        }
        catch (NullReferenceException ex)
        {

        }

        GameObject child = new GameObject();
        child.name = "Colliders";
        //child.hideFlags = HideFlags.HideInHierarchy;
        child.transform.position = transform.position;
        child.transform.parent = this.gameObject.transform;

        for (int i = 0; i < colliderTriangles.Count; i++)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);

            GameObject intermediateObject = new GameObject();
            intermediateObject.name = "Intermediate - " + i;
            //intermediateObject.hideFlags = HideFlags.HideInHierarchy;
            intermediateObject.transform.parent = child.transform;

            List<Vector3> colliderVertices = new List<Vector3>();
            colliderVertices.AddRange(triangleDetails[colliderTriangles[i][0]]);
            colliderVertices.AddRange(triangleDetails[colliderTriangles[i][1]]);
            List<Vector3> colliderVerticesCopy = colliderVertices.ConvertAll(vertex => new Vector3(vertex.x, vertex.y, vertex.z));
            List<Vector3> connectedNodes = colliderVertices.GroupBy(s => s).SelectMany(grp => grp.Skip(1)).Distinct().ToList();
            List<Vector3> unconnectedNodes = colliderVerticesCopy.Except(connectedNodes).ToList();
            colliderVertices = colliderVertices.Distinct().ToList();
            Vector3 boxColliderCentre = Vector3.zero;

            for (int j = 0; j < 4; j++)
            {
                boxColliderCentre += intermediateObject.transform.TransformPoint(colliderVertices[j]);
            }

            boxColliderCentre = boxColliderCentre / 4;
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

            colliderContainer.transform.localRotation = Quaternion.Euler(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);

            Vector3 yVector = intermediateObject.transform.TransformDirection(unconnectedNodes[0] - connectedNodes[0]).normalized;
            Vector3 xVector = intermediateObject.transform.TransformDirection(connectedNodes[1] - unconnectedNodes[0]).normalized;
            Vector3 colliderUp = colliderContainer.transform.TransformDirection(colliderContainer.transform.up).normalized;
            float value = Vector3.Dot(colliderContainer.transform.TransformDirection(colliderContainer.transform.up).normalized, colliderUp);

            float boxColliderSizeX = (Vector3.Distance(connectedNodes[0], unconnectedNodes[0]) + Vector3.Distance(connectedNodes[1], unconnectedNodes[1])) / 2;
            float boxColliderSizeY = (Vector3.Distance(connectedNodes[1], unconnectedNodes[0]) + Vector3.Distance(connectedNodes[0], unconnectedNodes[1])) / 2;

            Vector3 vec1 = this.transform.TransformDirection(unconnectedNodes[0] - connectedNodes[0]);
            Vector3 vec2 = this.transform.TransformDirection(connectedNodes[1] - unconnectedNodes[0]);
            Vector3 vec3 = this.transform.TransformDirection(unconnectedNodes[1] - connectedNodes[1]);
            Vector3 vec4 = this.transform.TransformDirection(connectedNodes[0] - unconnectedNodes[1]);

            Vector3 averageSideVector = (vec2 - vec4).normalized;

            if ((vec2 - vec4).magnitude < (vec1 - vec3).magnitude)
            {
                //averageSideVector = (vec1 - vec3).normalized;
            }

            Vector3 correctColliderUp = Vector3.Cross(averageSideVector, this.transform.TransformDirection(colliderContainer.transform.forward));
            Vector3 currentColliderUp = this.transform.TransformDirection(colliderContainer.transform.up);
            float rotationAngle = Vector3.SignedAngle(correctColliderUp, currentColliderUp, this.transform.TransformDirection(boxCollider.transform.forward));
            colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x, colliderContainer.transform.localEulerAngles.y, -rotationAngle);

            List<Vector3> sideVectors = new List<Vector3> { vec1, vec2, vec3, vec4 };

            List<float> xSidesLengths = new List<float> { sideVectors[sideOrders[i].x[0]].magnitude, sideVectors[sideOrders[i].x[1]].magnitude };
            List<float> ySidesLengths = new List<float> { sideVectors[sideOrders[i].y[0]].magnitude, sideVectors[sideOrders[i].y[1]].magnitude };

            boxCollider.size = new Vector3(((xSidesLengths[0] + xSidesLengths[1]) / 2) * objectScale.x, ((ySidesLengths[0] + ySidesLengths[1]) / 2) * objectScale.y, colliderHeight);
            colliders.Add(boxCollider);

            //colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x - objectRotation.x, colliderContainer.transform.localEulerAngles.y - objectRotation.y, colliderContainer.transform.localEulerAngles.z - objectRotation.z);

            //intermediateObject.SetActive(false);
            this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);
        }

        //this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);
    }

    public void UpdateCollider(int colliderIndex)
    {
        Vector3 objectScale = this.transform.localScale;
        Vector3 objectRotation = this.transform.rotation.eulerAngles;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        GameObject intermediateObject = intermediateObjects[colliderIndex];
        GameObject colliderContainer = colliderContainers[colliderIndex];
        BoxCollider collider = colliders[colliderIndex];

        this.transform.rotation = Quaternion.Euler(0, 0, 0);
        Vector3 boxColliderCentre = Vector3.zero;
        for (int j = 0; j < 4; j++)
        {
            boxColliderCentre += intermediateObject.transform.TransformPoint(mesh.vertices[squareVertices[colliderIndex][j]]);
        }

        boxColliderCentre = boxColliderCentre / 4;
        //intermediateObject.transform.localPosition = Vector3.zero;
        colliderContainer.transform.position = boxColliderCentre;

        Vector3 averageNormal = (mesh.normals[squareVertices[colliderIndex][0]] +
                                     mesh.normals[squareVertices[colliderIndex][1]] +
                                     mesh.normals[squareVertices[colliderIndex][2]] +
                                     mesh.normals[squareVertices[colliderIndex][3]]) / 4;

        colliderContainer.transform.rotation = Quaternion.LookRotation(intermediateObject.transform.TransformDirection(averageNormal));
        sw.Stop();
        totalTime += (float)sw.Elapsed.TotalMilliseconds;
        //colliderContainer.transform.forward = intermediateObject.transform.TransformDirection(averageNormal);
        colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localRotation.eulerAngles.x, colliderContainer.transform.localRotation.eulerAngles.y, 0);

        Vector3 vec1 = this.transform.TransformDirection(mesh.vertices[unconnectedSquareNodes[colliderIndex][0]] - mesh.vertices[connectedSquareNodes[colliderIndex][0]]);
        Vector3 vec2 = this.transform.TransformDirection(mesh.vertices[connectedSquareNodes[colliderIndex][1]] - mesh.vertices[unconnectedSquareNodes[colliderIndex][0]]);
        Vector3 vec3 = this.transform.TransformDirection(mesh.vertices[unconnectedSquareNodes[colliderIndex][1]] - mesh.vertices[connectedSquareNodes[colliderIndex][1]]);
        Vector3 vec4 = this.transform.TransformDirection(mesh.vertices[connectedSquareNodes[colliderIndex][0]] - mesh.vertices[unconnectedSquareNodes[colliderIndex][1]]);

        Vector3 averageSideVector = (vec2 - vec4).normalized;
        var k = this.transform.TransformDirection(colliderContainer.transform.forward);
        Vector3 correctColliderUp = Vector3.Cross(averageSideVector, this.transform.TransformDirection(colliderContainer.transform.forward));
        Vector3 currentColliderUp = this.transform.TransformDirection(colliderContainer.transform.up);
        float rotationAngle = Vector3.SignedAngle(correctColliderUp, currentColliderUp, this.transform.TransformDirection(collider.transform.forward));
        colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x, colliderContainer.transform.localEulerAngles.y, -rotationAngle);

        List<Vector3> sideVectors = new List<Vector3> { vec1, vec2, vec3, vec4 };

        List<float> xSidesLengths = new List<float> { sideVectors[sideOrders[colliderIndex].x[0]].magnitude, sideVectors[sideOrders[colliderIndex].x[1]].magnitude };
        List<float> ySidesLengths = new List<float> { sideVectors[sideOrders[colliderIndex].y[0]].magnitude, sideVectors[sideOrders[colliderIndex].y[1]].magnitude };

        collider.size = new Vector3(((xSidesLengths[0] + xSidesLengths[1]) / 2) * objectScale.x, ((ySidesLengths[0] + ySidesLengths[1]) / 2) * objectScale.y, colliderHeight);

        intermediateObjects[colliderIndex] = intermediateObject;
        colliderContainers[colliderIndex] = colliderContainer;
        colliders[colliderIndex] = collider;
        this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);
    }

    public void UpdateColliderGroup(List<int> colliderIndexes, int frameSpread = 1)
    {
        finishedRoutine = false;
        var indexInterval = Math.Floor(colliderIndexes.Count * (((double)1) / frameSpread));

        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            UpdateCollider(colliderIndexes[i]);
            index++;

            if (i == 0)
            {
                //continue;
            }

            if (i % indexInterval == 0)
            {
                //yield return null;
            }
        }

        index = 0;
        finishedRoutine = true;
    }

    public void UpdateColliderGPU(List<int> colliderIndexes)
    {
        int kernelHandle = computeShader.FindKernel("Main");
        int kernelHandle2 = computeShader.FindKernel("Main2");
        Vector3 objectRotation = this.transform.rotation.eulerAngles;
        this.transform.rotation = Quaternion.Euler(0, 0, 0);
        collidersToUpdate = colliderIndexes.ToArray();

        IM_TRSMs = new Matrix4x4[colliderIndexes.Count];

        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            IM_TRSMs[i] = intermediateObjects[colliderIndexes[i]].transform.localToWorldMatrix;
        }

        CC_TRSMs = new Matrix4x4[colliderIndexes.Count];
        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            CC_TRSMs[i] = colliderContainers[colliderIndexes[i]].transform.localToWorldMatrix;
        }

        meshVerticesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        meshVerticesBuffer.SetData(mesh.vertices);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("meshVertices"), meshVerticesBuffer);

        IM_TRSM_Buffer = new ComputeBuffer(IM_TRSMs.Length, sizeof(float) * 16);
        IM_TRSM_Buffer.SetData(IM_TRSMs);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("IM_TRSMs"), IM_TRSM_Buffer);

        CC_TRSM_Buffer = new ComputeBuffer(CC_TRSMs.Length, sizeof(float) * 16);
        CC_TRSM_Buffer.SetData(CC_TRSMs);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("CC_TRSMs"), CC_TRSM_Buffer);

        debugArray = new Vector3[colliderIndexes.Count];
        debugBuffer = new ComputeBuffer(debugArray.Length, sizeof(float) * 3);
        debugBuffer.SetData(debugArray);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("debugBuffer"), debugBuffer);

        collidersToUpdateBuffer = new ComputeBuffer(collidersToUpdate.Length, sizeof(int));
        collidersToUpdateBuffer.SetData(collidersToUpdate);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("collidersToUpdate"), collidersToUpdateBuffer);

        returnDetails = new Matrix4x4[CC_TRSMs.Length];
        returnDetailsBuffer = new ComputeBuffer(returnDetails.Length, sizeof(float) * 16);
        returnDetailsBuffer.SetData(returnDetails);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("returnDetails"), returnDetailsBuffer);

        meshNormalsBuffer = new ComputeBuffer(mesh.normals.Length, sizeof(float) * 3);
        meshNormalsBuffer.SetData(mesh.normals);
        computeShader.SetBuffer(kernelHandle, Shader.PropertyToID("meshNormals"), meshNormalsBuffer);

        computeShader.Dispatch(kernelHandle, collidersToUpdate.Length, 1, 1);

        returnDetailsBuffer.GetData(returnDetails);
        returnDetailsBuffer.Dispose();

        debugBuffer.GetData(debugArray);
        debugBuffer.Dispose();

        Debug.Log("DEBUG : " + debugArray[0]);
        Debug.Log("UNITY : " + Quaternion.LookRotation(intermediateObjects[10].transform.TransformDirection(new Vector3(0.5f, 0.2f, -0.3f))));
        Debug.Log("MATHFUNCTIONS : " + MathFunctions.LookRotation(intermediateObjects[0].transform.TransformDirection(new Vector3(0.5932f, -0.7952f, 0.071f)), new Vector3(0, 1, 0)));

        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            int currentIndex = colliderIndexes[i];
            GameObject colliderContainer = colliderContainers[currentIndex];

            List<object> details = DecomposeReturnMatrix(returnDetails[i]);

            colliderContainer.transform.position = (Vector3)details[0];
            Debug.Log("QUAT : " + (Quaternion)details[1]);
            colliderContainer.transform.rotation = (Quaternion)details[1];
            colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.eulerAngles.x, colliderContainer.transform.eulerAngles.y, 0);

            colliderContainers[currentIndex] = colliderContainer;
        }

        CC_TRSMs = new Matrix4x4[colliderIndexes.Count];
        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            CC_TRSMs[i] = colliderContainers[colliderIndexes[i]].transform.localToWorldMatrix;
        }

        // Resend the vertices array to the GPU
        meshVerticesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        meshVerticesBuffer.SetData(mesh.vertices);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("meshVertices"), meshVerticesBuffer);

        /*debugBuffer = new ComputeBuffer(debugArray.Length, sizeof(float) * 3);
        debugBuffer.SetData(debugArray);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("debugBuffer"), debugBuffer);*/

        // Get the local transformation matrices of the colliderContainers
        CC_Local_TRSMs = new Matrix4x4[colliderIndexes.Count];
        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            GameObject CC = colliderContainers[colliderIndexes[i]];
            CC_Local_TRSMs[i] = Matrix4x4.TRS(CC.transform.localPosition, CC.transform.localRotation, CC.transform.localScale);
        }

        // Send the updated collider container TRSMs to the GPU
        CC_TRSM_Buffer = new ComputeBuffer(CC_TRSMs.Length, sizeof(float) * 16);
        CC_TRSM_Buffer.SetData(CC_TRSMs);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("CC_TRSMs"), CC_TRSM_Buffer);

        // Send the local transformation matrices of the colliderContainers to the GPU
        CC_Local_TRSM_Buffer = new ComputeBuffer(CC_Local_TRSMs.Length, sizeof(float) * 16);
        CC_Local_TRSM_Buffer.SetData(CC_Local_TRSMs);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("CC_Local_TRSMs"), CC_Local_TRSM_Buffer);

        // Resend the colliders to update indices to the GPU
        collidersToUpdateBuffer = new ComputeBuffer(collidersToUpdate.Length, sizeof(int));
        collidersToUpdateBuffer.SetData(collidersToUpdate);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("collidersToUpdate"), collidersToUpdateBuffer);

        // Send the X side orderings to the GPU
        sidesX = new ComputeBuffer(sidesXConverted.Length, sizeof(int));
        sidesX.SetData(sidesXConverted);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("sideOrders_X"), sidesX);

        // Send the Y side orderings to the GPU
        sidesY = new ComputeBuffer(sidesYConverted.Length, sizeof(int));
        sidesY.SetData(sidesYConverted);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("sideOrders_Y"), sidesY);

        returnRotationAngles = new float[colliderIndexes.Count];
        returnRotationAngles_Buffer = new ComputeBuffer(returnRotationAngles.Length, sizeof(int));
        returnRotationAngles_Buffer.SetData(returnRotationAngles);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("returnRotationAngles"), returnRotationAngles_Buffer);

        /*returnColliderSizes = new Vector3[colliderIndexes.Count];
        returnColliderSizes_Buffer = new ComputeBuffer(returnColliderSizes.Length, sizeof(int));
        returnColliderSizes_Buffer.SetData(returnColliderSizes);
        computeShader.SetBuffer(kernelHandle2, Shader.PropertyToID("returnColliderSizes"), returnColliderSizes_Buffer);*/

        // Send the main transform to the GPU
        Matrix4x4 thisTransform_TRSM = MathFunctions.Get_TRS_Matrix(this.transform.position, this.transform.rotation.eulerAngles, this.transform.localScale);
        computeShader.SetMatrix(Shader.PropertyToID("thisTransform"), thisTransform_TRSM);

        computeShader.Dispatch(kernelHandle2, collidersToUpdate.Length, 1, 1);

        //debugBuffer.GetData(debugArray);
        //debugBuffer.Dispose();

        returnRotationAngles_Buffer.GetData(returnRotationAngles);
        returnRotationAngles_Buffer.Dispose();

        //returnColliderSizes_Buffer.GetData(returnColliderSizes);
        //returnColliderSizes_Buffer.Dispose();

        for (int i = 0; i < colliderIndexes.Count; i++)
        {
            int currentIndex = colliderIndexes[i];
            GameObject colliderContainer = colliderContainers[currentIndex];
            BoxCollider collider = colliders[currentIndex];

            //colliderContainer.transform.localEulerAngles = new Vector3(colliderContainer.transform.localEulerAngles.x, colliderContainer.transform.localEulerAngles.y, -returnRotationAngles[i]);
            //collider.size = returnColliderSizes[i];

            //colliderContainers[currentIndex] = colliderContainer;
            //colliders[currentIndex] = collider;
        }

        this.transform.rotation = Quaternion.Euler(objectRotation.x, objectRotation.y, objectRotation.z);
    }

    private List<object> DecomposeReturnMatrix(Matrix4x4 returnMatrix)
    {
        Vector4 rawPosition = returnMatrix.GetRow(0);
        Vector3 position = new Vector3(rawPosition[0], rawPosition[1], rawPosition[2]);

        Vector4 quaternionDetails = returnMatrix.GetRow(1);
        Quaternion quaternion = new Quaternion(quaternionDetails[0], quaternionDetails[1], quaternionDetails[2], quaternionDetails[3]);

        //Vector4 rawScale = returnMatrix.GetRow(2);
        //Vector3 scale = new Vector3(rawScale[0], rawScale[1], rawScale[2]);

        return new List<object>() { position, quaternion };
    }

    private void OnDrawGizmos()
    {
        /*Gizmos.color = Color.green;
        Gizmos.DrawLine(colliderVector[0], colliderVector[1]);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(sideVector[0], sideVector[1]);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(extraVector[0], extraVector[1]);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(hypotenuseVector[0], hypotenuseVector[1]);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(colliderUpVector[0], colliderUpVector[1]);*/
        /*Gizmos.color = Color.red;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(mesh.vertices[i]), transform.TransformPoint(mesh.vertices[i] + mesh.normals[i]));
        }*/

        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(origin, 0.01f);
        //Gizmos.DrawSphere(transform.TransformPoint(origin), 05f);

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

        /*Gizmos.color = Color.red;
        List<int> stronglyConnected = colliderTriangles[index];

        for (int i = 0; i < stronglyConnected.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[meshTriangles[stronglyConnected[i]][j]]), 0.01f);
            }
        }*/

        /*Gizmos.color = Color.red;
        for (int j = 0; j < 4; j++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[squareVertices[index][j]]), 0.01f);
        }*/


        //Gizmos.color = Color.green;
        //Gizmos.DrawSphere(transform.TransformPoint(colliderVertices[4][testIndex]), 0.01f);
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
