using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Debugger;

namespace HelperScripts.Methods
{
    public class MeshManager
    {
        private Mesh queryMesh;
        private Vector3[] normals;
        private Vector3[] vertices;
        private int[] triangles;
        private List<List<Vector3>> triangleDetails;
        private GameObject meshParentObject;
        private Dictionary<Vector3, List<Vector3>> connectedVertices;

        public MeshManager(Mesh queryMesh)
        {
            this.queryMesh = queryMesh;
            this.vertices = queryMesh.vertices;
            this.triangles = queryMesh.triangles;
            this.normals = queryMesh.normals;
        }

        public MeshManager(MeshFilter meshObject)
        {
            this.queryMesh = meshObject.mesh;
            this.meshParentObject = meshObject.gameObject;
            this.vertices = queryMesh.vertices;
            this.triangles = queryMesh.triangles;
            this.normals = queryMesh.normals;
        }

        /// <summary>
        /// Get the vertices which are connected to each other via edges
        /// </summary>
        /// <returns></returns>
        public Dictionary<Vector3, List<Vector3>> GetConnectedVertices()
        {
            connectedVertices = new Dictionary<Vector3, List<Vector3>>();
            List<List<Vector3>> trianglesInfo = new List<List<Vector3>>();
            List<List<int>> triangleVertexIndexes = new List<List<int>>();

            for (int i = 0; i < (triangles.Length / 3); i++)
            {
                trianglesInfo.Add(new List<Vector3>());
                triangleVertexIndexes.Add(new List<int>());

                for (int j = 0; j < 3; j++)
                {
                    trianglesInfo[i].Add(vertices[triangles[j + (i * 3)]]);
                    triangleVertexIndexes[i].Add(triangles[j + (i * 3)]);
                }
            }

            triangleDetails = trianglesInfo;
            DebugHelper.PrintListList(triangleDetails, false, true);
            GetVertexPath(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f));

            // Loop through each triangle
            for (int i = 0; i < triangleDetails.Count; i++)
            {
                // Loop through each vertex in the triangle
                for (int j = 0; j < 3; j++)
                {
                    // Add a key for this vertex into the connected vertices dictionary if it is not in the dictionary already
                    if (connectedVertices.ContainsKey(triangleDetails[i][j]) == false)
                    {
                        connectedVertices.Add(triangleDetails[i][j], new List<Vector3>());
                    }

                    // Loop through each vertex in the triangle again to add the appropriate vertices to the connected list
                    for (int k = 0; k < 3; k++)
                    {
                        if ((triangleDetails[i][j] != triangleDetails[i][k]) && (connectedVertices[triangleDetails[i][j]].Contains(triangleDetails[i][k]) == false))
                        {
                            connectedVertices[triangleDetails[i][j]].Add(triangleDetails[i][k]);
                        }
                    }
                }
            }

            return connectedVertices;
        }

        public List<List<int>> DrawColliders()
        {
            List<List<int>> meshTriangles = new List<List<int>>();
            List<List<int>> stronglyConnectedTriangles = new List<List<int>>(); // Contains the indexes of meshTriangles which share 2 nodes

            for (int i = 0; i < triangles.Length; i++)
            {
                if (i % 3 == 0)
                {
                    meshTriangles.Add(new List<int>());
                }

                meshTriangles.Last().Add(triangles[i]);
            }

            DebugHelper.PrintListList(meshTriangles, false, true);

            for (int i = 0; i < meshTriangles.Count; i++)
            {
                List<int> currentTriangle = meshTriangles[i];

                for (int j = 0; j < meshTriangles.Count; j++)
                {
                    if (j == i) continue;

                    int nodesShared = 0;

                    for (int k = 0; k < 3; k++)
                    {
                        if (meshTriangles[j].Contains(currentTriangle[k]))
                        {
                            nodesShared++;
                        }
                    }

                    if (nodesShared == 2)
                    {
                        stronglyConnectedTriangles.Add(new List<int> { i, j });
                        break;
                    }
                }
            }

            for (int i = 0; i < stronglyConnectedTriangles.Count; i++)
            {
                int triangleOne = stronglyConnectedTriangles[i][0];
                int triangleTwo = stronglyConnectedTriangles[i][1];

                for (int j = 0; j < stronglyConnectedTriangles.Count; j++)
                {
                    int triangleThree = stronglyConnectedTriangles[j][0];
                    int triangleFour = stronglyConnectedTriangles[j][1];

                    if (triangleOne == triangleFour && triangleTwo == triangleThree)
                    {
                        stronglyConnectedTriangles.RemoveAt(j);
                        break;
                    }
                }
            }

            DebugHelper.PrintListList(stronglyConnectedTriangles, false, true, false);

            return meshTriangles;
        }

        public Vector3 GetClosestVertexToPoint(Vector3 queryPoint, bool convertToLocalSpace = false, Transform objectTransform = null)
        {
            float shortestDistance = 99;
            Vector3 closestVertex = vertices[0];

            if (convertToLocalSpace == true)
            {
                if (objectTransform == null) throw new System.Exception("ERROR: Transform can't be null!");

                closestVertex = objectTransform.InverseTransformPoint(queryPoint);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(queryPoint, vertices[i]);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestVertex = vertices[i];
                }
            }

            return closestVertex;
        }

        /// <summary>
        /// Gets vertices of the top layer of the mesh.
        /// This is ideal when you want to use a plane as a mesh,
        /// but backface culling prevents the plane from being rendered
        /// so you have to use a box instead. We only need to consider
        /// the vertices on the top of the mesh, and we can copy their
        /// movements for the bottom of the mesh.
        /// </summary>
        /// <returns></returns>
        public List<int> GetTopVertices()
        {
            List<int> topVertexIndexes = new List<int>();

            return topVertexIndexes;
        }

        public List<int> GetMeshEdgeVertices()
        {
            return null;
        }

        public List<Vector3> GetVertexPath(Vector3 startVertex, Vector3 targetVertex)
        {
            List<Vector3> path = new List<Vector3>();

            // TODO

            return path;
        }

        public Vector3[] CalculateMeshStrengths()
        {
            float[] meshStrengths = new float[vertices.Length];
            Vector3[] testOutput = new Vector3[vertices.Length];
            Vector3[] offsetVertices = new Vector3[vertices.Length];
            if (connectedVertices == null) connectedVertices = GetConnectedVertices();

            for (int i = 0; i < vertices.Length; i++)
            {
                offsetVertices[i] = vertices[i] - normals[i] * 0.01f;
            }

            MeshCollider meshCollider = meshParentObject.AddComponent<MeshCollider>();

            for (int i = 0; i < vertices.Length; i++)
            {
                testOutput[i] = offsetVertices[i];
            }

            return testOutput;
        }
    }
}
