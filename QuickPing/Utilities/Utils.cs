using System;
using System.Collections.Generic;
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

        public static void Write(this ZPackage zPackage, Minimap.PinData pinData)
        {
            zPackage.Write(pinData.m_name);
            zPackage.Write(pinData.m_type.ToString());
            zPackage.Write(pinData.m_pos);
        }


        /// <summary>
        /// Read ZPackage into PinnedObjects list
        /// </summary>
        /// <param name="package">Loaded from file</param>
        public static Dictionary<ZDOID, Minimap.PinData> ReadPinnedObjects(this ZPackage package)
        {
            Dictionary<ZDOID, Minimap.PinData> res = new Dictionary<ZDOID, Minimap.PinData>();

            if (package == null) return res;

            while (package.GetPos() < package.Size())
            {
                ZDOID zdoid = package.ReadZDOID();
                Minimap.PinData pinData = package.ReadPinData();
                if (!res.ContainsKey(zdoid))
                    res.Add(zdoid, pinData);

            }

            return res;

        }

        private static Minimap.PinData ReadPinData(this ZPackage package)
        {
            return new()
            {
                m_name = package.ReadString(),
                m_type = (Minimap.PinType)Enum.Parse(typeof(Minimap.PinType), package.ReadString()),
                m_pos = package.ReadVector3()
            };
        }

        public static bool Compare(this Minimap.PinData x, Minimap.PinData y)
        {
            return x.m_pos == y.m_pos
                && x.m_name == y.m_name
                && x.m_type == y.m_type;
        }

    }
}
