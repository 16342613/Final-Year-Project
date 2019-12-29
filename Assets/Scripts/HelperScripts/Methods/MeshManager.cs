using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace HelperScripts.Methods
{
    public class MeshManager
    {
        public MeshManager()
        {
            // Empty constructor for now
        }

        public static Dictionary<string, object> GetConnectedVertices(Mesh queryMesh)
        {
            Dictionary<Vector3, List<Vector3>> connectedVertices = new Dictionary<Vector3, List<Vector3>>();
            Vector3[] vertices = queryMesh.vertices;
            int[] triangles = queryMesh.triangles;
            List<List<Vector3>> trianglesInfo = new List<List<Vector3>>();
            
            for (int i = 0; i < (triangles.Length / 3); i++)
            {
                trianglesInfo.Add(new List<Vector3>());
                
                for (int j = 0; j < 3; j++)
                {
                    trianglesInfo[i].Add(vertices[triangles[j + (i * 3)]]);
                }
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                // Avoid duplicate vertices
                if (connectedVertices.ContainsKey(vertices[i]) == false)
                {

                }
            }

            Dictionary<string, object> toReturn = new Dictionary<string, object>();
            toReturn.Add("trianglesInfo", trianglesInfo);


            return toReturn;
        }
    }
}
