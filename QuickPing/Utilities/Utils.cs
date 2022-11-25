using UnityEngine;

namespace QuickPing.Utilities
{
    internal static class GO_Ext
    {
        public static IDestructible GetRecursiveComponentInParents<IDestructible>(Transform root)

        {
            if (root.parent == null) return default;

            var res = root.GetComponentInParent<IDestructible>();
            if (res != null)
                return res;
            else
                return GetRecursiveComponentInParents<IDestructible>(root.parent);
        }


        public static GameObject GetRecursiveParentWithComponent<T>(Transform root)
        {
            if (!root.parent) return null;
            if (root.parent.gameObject.TryGetComponent(out T _))
            {
                return root.parent.gameObject;
            }
            else
                return GetRecursiveParentWithComponent<T>(root.parent);
        }
    }
}
