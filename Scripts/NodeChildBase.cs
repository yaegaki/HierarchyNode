using UnityEngine;

namespace HierarchyNode.Base
{
    public class NodeChildrenBase
    {
        protected Transform transform;
        public static T Create<T>(Transform transform)
            where T : NodeChildrenBase, new()
        {
            return new T() { transform = transform };
        }
    }
}
