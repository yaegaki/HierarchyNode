using UnityEngine;

namespace HierarchyNode.Base
{
    public class EmptyChildrenNodeBase
    {
        protected Transform transform;
        public EmptyChildrenNodeBase(Transform transform)
        {
            this.transform = transform;
        }

        public Transform Transform { get { return transform; } }
        public GameObject GameObject { get { return transform.gameObject; } }

        public U AddComponent<U>()
            where U : Component
        {
            return this.GameObject.AddComponent<U>();
        }

        public U GetComponent<U>()
            where U : Component
        {
            return this.GameObject.GetComponent<U>();
        }

        public U[] GetComponents<U>()
            where U : Component
        {
            return this.GameObject.GetComponents<U>();
        }
    }

    public class NodeBase<T> : EmptyChildrenNodeBase
        where T : NodeChildrenBase, new()
    {
        public NodeBase(Transform transform) : base(transform)
        {
        }

        private T _Children;
        public T Children
        {
            get
            {
                return _Children ?? (_Children = NodeChildrenBase.Create<T>(transform));
            }
        }

        public T c { get { return Children; } }
    }
}
