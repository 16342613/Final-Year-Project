using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Debugger;

namespace HelperScripts.Methods
{
    public class MeshManager
    {
        public Mesh queryMesh;
        public Vector3[] normals;
        public Vector3[] vertices;
        public int[] triangles;
        public List<List<Vector3>> triangleDetails;
        public GameObject meshParentObject;
        public Dictionary<Vector3, List<Vector3>> connectedVertices;
        public List<List<int>> meshTriangles = new List<List<int>>();   // The vertex indexes of each vertex in a triangle
        public List<List<int>> stronglyConnectedTriangles = new List<List<int>>();
        public List<List<int>> colliderTriangles = new List<List<int>>();

        public List<List<int>> squareVertices = new List<List<int>>();
        public List<List<int>> connectedSquareNodes = new List<List<int>>();
        public List<List<int>> unconnectedSquareNodes = new List<List<int>>();
        public string objectName;

        public Dictionary<int, List<int>> vertexSquareMapping = new Dictionary<int, List<int>>();

        public MeshManager(Mesh queryMesh, string objectName)
        {
            this.queryMesh = queryMesh;
            this.vertices = queryMesh.vertices;
            this.triangles = queryMesh.triangles;
            this.normals = queryMesh.normals;
            this.objectName = objectName;
        }

        public MeshManager(MeshFilter meshObject, string objectName)
        {
            this.queryMesh = meshObject.mesh;
            this.meshParentObject = meshObject.gameObject;
            this.vertices = queryMesh.vertices;
            this.triangles = queryMesh.triangles;
            this.normals = queryMesh.normals;
            this.objectName = objectName;
        }

        public List<List<Vector3>> RecalculateTriangleDetails(Mesh mesh)
        {
            this.queryMesh = mesh;
            this.vertices = queryMesh.vertices;

            triangleDetails.Clear();

            for (int i = 0; i < (triangles.Length / 3); i++)
            {
                triangleDetails.Add(new List<Vector3>());

                for (int j = 0; j < 3; j++)
                {
                    triangleDetails[i].Add(vertices[triangles[j + (i * 3)]]);
                }
            }

            return triangleDetails;
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

        /// <summary>
        /// A method that gets the cleans up Mesh.triangles into a nice 2D list
        /// </summary>
        /// <returns></returns>
        /// REFERENCE POINT 1
        public List<List<int>> GetMeshTriangles()
        {
            // Loop through all of the Mesh.triangles array
            for (int i = 0; i < triangles.Length; i++)
            {
                if (i % 3 == 0)
                {
                    // Add a new entry to the 2D list, since this is a new triangle
                    meshTriangles.Add(new List<int>());
                }

                meshTriangles.Last().Add(triangles[i]);
            }

            return meshTriangles;
        }

        public List<List<int>> GetStronglyConnectedTriangles()
        {
            if (meshTriangles.Count == 0)
            {
                GetMeshTriangles();
            }
            
            /// REFERENCE POINT 2
            for (int i = 0; i < meshTriangles.Count; i++)
            {
                // Obtain a reference triangle
                List<int> currentTriangle = meshTriangles[i];
                List<int> connectionNodes = new List<int>();

                // Loop through all the other mesh triangles
                for (int j = 0; j < meshTriangles.Count; j++)
                {
                    if (j == i) continue;   // A traingle cannot be strongly connected to itself
                    int nodesShared = 0;

                    // Loop through each vertex in the reference triangle
                    for (int k = 0; k < 3; k++)
                    {
                        if (meshTriangles[j].Contains(currentTriangle[k]))
                        {
                            nodesShared++;
                        }
                    }

                    // If the triangles share two vertices
                    if (nodesShared == 2)
                    {
                        // This index pair is a strongly connected triangle
                        stronglyConnectedTriangles.Add(new List<int> { i, j });
                    }
                }
            }

            /// REFERENCE POINT 2
            for (int i = 0; i < stronglyConnectedTriangles.Count; i++)
            {
                // Triangle vertices as Vector3
                List<Vector3> triangle1Vertices = triangleDetails[stronglyConnectedTriangles[i][0]];
                List<Vector3> triangle2Vertices = triangleDetails[stronglyConnectedTriangles[i][1]];

                // Triangle vertices as indices of mesh.vertices
                List<int> triangle1Indexes = meshTriangles[stronglyConnectedTriangles[i][0]];
                List<int> triangle2Indexes = meshTriangles[stronglyConnectedTriangles[i][1]];
                List<int> polygonIndexes = new List<int>();

                // Find the vertex indices of the corners of the polygon formed by the strongly connected triangle pair
                for (int j = 0; j < 6; j++)
                {
                    if (j <= 2)
                    {
                        polygonIndexes.Add(triangle1Indexes[j]);
                    }
                    else
                    {
                        polygonIndexes.Add(triangle2Indexes[j - 3]);
                    }
                }

                // Get the distinct vertex indices to remove duplicates
                List<int> connectedNodesIndexes = polygonIndexes.GroupBy(s => s).SelectMany(grp => grp.Skip(1)).Distinct().ToList();
                polygonIndexes = polygonIndexes.Distinct().ToList();

                // individual vertices of the strongly connected triangle pair
                Vector3 a1 = vertices[connectedNodesIndexes[0]];
                Vector3 b1 = vertices[triangle1Indexes.Except(connectedNodesIndexes).ToList()[0]];
                Vector3 c1 = vertices[connectedNodesIndexes[1]];
                Vector3 a2 = vertices[connectedNodesIndexes[0]];
                Vector3 b2 = vertices[triangle2Indexes.Except(connectedNodesIndexes).ToList()[0]];
                Vector3 c2 = vertices[connectedNodesIndexes[1]];

                // Get the internal angles of the first triangle
                float abcAngle1 = MathFunctions.GetVectorInternalAngle(b1, c1, a1);
                float bcaAngle1 = MathFunctions.GetVectorInternalAngle(c1, a1, b1);
                float cabAngle1 = MathFunctions.GetVectorInternalAngle(a1, b1, c1);

                // Get the internal angles of the second triangle
                float abcAngle2 = MathFunctions.GetVectorInternalAngle(b2, c2, a2);
                float bcaAngle2 = MathFunctions.GetVectorInternalAngle(c2, a2, b2);
                float cabAngle2 = MathFunctions.GetVectorInternalAngle(a2, b2, c2);

                // The internal angles of the polygon formed by the strongly connected triangle pair
                float polygonABC = abcAngle1;
                float polygonBCD = bcaAngle1 + bcaAngle2;
                float polygonCDA = abcAngle2;
                float polygonDAB = cabAngle1 + cabAngle2;

                List<float> polygonInternalAngles = new List<float> { polygonABC, polygonBCD, polygonCDA, polygonDAB };
                bool approximatelySquare = true;

                // Loop through each of the polygon internal angles
                for (int j = 0; j < 4; j++)
                {
                    // If the the difference between the internal angle and the ideal internal angle (90 degrees to form a 
                    // perfect square) is greater than the threshold angle (20 degrees is optimal, but can be tuned if 
                    // necessary), this polygon is not roughly square.
                    if (Mathf.Abs(90 - polygonInternalAngles[j]) > 20)
                    {
                        approximatelySquare = false;
                        break;
                    }
                }

                // If the polygon is roughly square, then add it to the next stage
                if (approximatelySquare == true)
                {
                    colliderTriangles.Add(stronglyConnectedTriangles[i]);
                }
            }

            DebugHelper.PrintListList(colliderTriangles, true, true);

            /// REFERENCE POINT 5
            // We still need to process the results to remove duplicates
            List<List<int>> processedColliderTriangles = new List<List<int>>();
            List<int> duplicatedIndexes = new List<int>();

            for (int i = 0; i < colliderTriangles.Count; i++)
            {
                int triangle1 = colliderTriangles[i][0];
                int triangle2 = colliderTriangles[i][1];

                for (int j = i; j < colliderTriangles.Count; j++)
                {
                    int triangle3 = colliderTriangles[j][0];
                    int triangle4 = colliderTriangles[j][1];

                    if (triangle1 == triangle4 && triangle2 == triangle3)
                    {
                        duplicatedIndexes.Add(j);
                    }
                }
            }

            for (int i = 0; i < colliderTriangles.Count; i++)
            {
                if (duplicatedIndexes.Contains(i) == false)
                {
                    processedColliderTriangles.Add(colliderTriangles[i]);

                    List<int> colliderVertices = new List<int>();
                    colliderVertices.AddRange(meshTriangles[processedColliderTriangles.Last()[0]]);
                    colliderVertices.AddRange(meshTriangles[processedColliderTriangles.Last()[1]]);
                    List<int> colliderVerticesCopy = new List<int>(colliderVertices);
                    connectedSquareNodes.Add(colliderVertices.GroupBy(s => s).SelectMany(grp => grp.Skip(1)).Distinct().ToList());
                    unconnectedSquareNodes.Add(colliderVerticesCopy.Except(connectedSquareNodes.Last()).ToList());

                    squareVertices.Add(new List<int>());
                    squareVertices.Last().AddRange(unconnectedSquareNodes.Last());
                    squareVertices.Last().AddRange(connectedSquareNodes.Last());
                }
            }

            colliderTriangles = processedColliderTriangles;
            CalculateVertexSquareMapping();

            return processedColliderTriangles;
        }

        private void CalculateVertexSquareMapping()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexSquareMapping.Add(i, new List<int>());

                for (int j = 0; j < squareVertices.Count; j++)
                {
                    if (squareVertices[j].Contains(i))
                    {
                        vertexSquareMapping[i].Add(j);
                    }
                }
            }
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
