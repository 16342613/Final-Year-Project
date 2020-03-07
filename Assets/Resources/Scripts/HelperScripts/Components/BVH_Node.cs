using System;
using System.Collections.Generic;

public class BVH_Node
{
    public BoundingBox item;
    public BVH_Node parent;
    public List<BVH_Node> children = new List<BVH_Node>();
    public int level;
    public bool root;

    public BVH_Node(BoundingBox item, BVH_Node parent, bool root = false)
    {
        this.item = item;
        this.parent = parent;
        this.root = root;

        if (root == true)
        {
            level = 0;
        }
        else
        {
            level = parent.level + 1;
        }
    }

    public void AddChildNode(BVH_Node child)
    {
        children.Add(child);
    }
}
