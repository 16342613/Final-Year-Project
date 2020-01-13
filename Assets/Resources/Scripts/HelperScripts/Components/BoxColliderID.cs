using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelperScripts.Components
{
    public class BoxColliderID
    {
        public BoxCollider collider;
        public int ID;

        public BoxColliderID(BoxCollider collider, int ID)
        {
            this.collider = collider;
            this.ID = ID;
        }
    }
}
