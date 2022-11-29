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
        public static bool SaveData(World world, ZPackage zPackage)
        {
            bool cloudSaveFailed;
            if (world.m_fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(World.GetWorldSavePath(world.m_fileSource));
            }

            string metaPath = GetPinnedPath(world);
            string text = metaPath + ".new";
            string oldFile = metaPath + ".old";
            byte[] array = zPackage.GetArray();
            FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, world.m_fileSource);
            fileWriter.m_binary.Write(array.Length);
            fileWriter.m_binary.Write(array);
            fileWriter.Finish();


            cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && world.m_fileSource == FileHelpers.FileSource.Cloud;
            if (!cloudSaveFailed)
            {
                FileHelpers.ReplaceOldFile(metaPath, text, oldFile, world.m_fileSource);
            }

            return cloudSaveFailed;
        }

        public static string GetPinnedPath(World world)
        {
            FileHelpers.SplitFilePath(world.GetDBPath(), out string directory, out string fileName, out string fileExtension);
            var worldSavePath = directory + fileName;
            const string __MyExtension = ".mod.quickping.pinned";


            //INFO
            //QuickPingPlugin.Log.LogInfo("World .pinned save path: " + worldSavePath);

            return worldSavePath + __MyExtension;
        }
    }
}
