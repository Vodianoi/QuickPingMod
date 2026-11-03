using UnityEngine;

namespace QuickPing.Utilities
{
    internal static class GO_Ext
    {
        public static T GetRecursiveComponentInParents<T>(Transform root)
        {
            if (root.parent == null) return default;

            var res = root.GetComponentInParent<T>();
            if (res != null)
                return res;
            else
                return GetRecursiveComponentInParents<T>(root.parent);
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

    internal static class DataUtils
    {
        public static bool Compare(this Minimap.PinData x, Minimap.PinData y)
        {
            return x.m_pos == y.m_pos
                && x.m_name == y.m_name;
        }
    }
}
