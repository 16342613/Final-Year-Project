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
        private Vector3[] vertices;
        private int[] triangles;
        private List<List<Vector3>> triangleDetails;

        public MeshManager(Mesh queryMesh)
        {
            this.queryMesh = queryMesh;
            this.vertices = queryMesh.vertices;
            this.triangles = queryMesh.triangles;
        }

        public Dictionary<Vector3, List<Vector3>> GetConnectedVertices()
        {
            Dictionary<Vector3, List<Vector3>> connectedVertices = new Dictionary<Vector3, List<Vector3>>();
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

        /*public List<Vector3> GetMeshCorners()
        {
            Vector3 maxHeight = queryMesh.bounds.center;  // 0
            Vector3 maxDepth = queryMesh.bounds.center;   // 1
            Vector3 maxNorth = queryMesh.bounds.center;   // 2
            Vector3 maxSouth = queryMesh.bounds.center;   // 3
            Vector3 maxEast = queryMesh.bounds.center;    // 4
            Vector3 maxWest = queryMesh.bounds.center;    // 5

            for (int i = 0; i < vertices.Length; i++)
            {
                
            }
        }*/

        public List<Vector3> GetVertexPath(Vector3 startVertex, Vector3 targetVertex)
        {
            List<Vector3> path = new List<Vector3>();

            // TODO

            return path;
        }
    }
}
