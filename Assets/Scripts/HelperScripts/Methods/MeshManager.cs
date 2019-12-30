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

            for (int i = 0; i < vertices.Length; i++)
            {
                // Avoid duplicate vertices
                if (connectedVertices.ContainsKey(vertices[i]) == false)
                {

                }
            }

            return connectedVertices;
        }

        public List<Vector3> GetVertexPath(Vector3 startVertex, Vector3 targetVertex)
        {
            List<Vector3> path = new List<Vector3>();

            return path;
        }
    }
}
