using System;
using System.Collections.Generic;
using UnityEngine;
public class BoundingBox
{
    BoxCollider collider;

    public BoundingBox(BoxCollider collider)
    {
        this.collider = collider;
    }
}
