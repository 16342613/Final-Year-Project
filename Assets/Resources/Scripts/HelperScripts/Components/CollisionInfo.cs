using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelperScripts.Components
{
    public class CollisionInfo
    {
        public GameObject collisionObject;
        public Vector3[] collisionPoints;

        public CollisionInfo(GameObject collisionObject, Vector3[] collisionPoints)
        {
            this.collisionObject = collisionObject;
            this.collisionPoints = collisionPoints;
        }

    }
}
