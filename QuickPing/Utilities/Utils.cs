using System.IO;
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
    internal static class DataUtils
    {

        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }
        public static bool Compare(this Minimap.PinData x, Minimap.PinData y)
        {
            return x.m_pos == y.m_pos
                && x.m_name == y.m_name;
        }


    }
}
