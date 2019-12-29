using System.Collections;
using System.Collections.Generic;

namespace HelperScripts.Tree
{
    public class Node
    {
        public Node parent;
        public object item;
        public int depth;
        public List<Node> children = new List<Node>();

        public Node(Node parent, object item)
        {
            this.parent = parent;
            this.item = item;
        }

        public void AddChild(Node child)
        {
            children.Add(child);
        }
    }
}
