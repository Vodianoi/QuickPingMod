using System;
using System.Collections.Generic;
using System.IO;

namespace QuickPing.Utilities
{
    internal static class DataManager
    {
        #region Public Fields
        public static Dictionary<ZDOID, Minimap.PinData> PinnedObjects = new();
        public static Dictionary<string, string> CustomNames = new();
        #endregion

        #region Private Fields
        #endregion

        internal struct CustomName
        {
            public string Original;
            public string Custom;
        }

        public class PinnedObject
        {
            public PinnedObject()
            {
                ZDOID = ZDOID.None;
                PinData = new Minimap.PinData();
            }
            public ZDOID ZDOID { get; set; }
            public Minimap.PinData PinData { get; set; }
        }

        internal enum Status
        {
            Success,
            Failed,
            NoData
        }


        #region Public Methods
        public static Status Save(World world)
        {
            Status status = SavePinnedObjects(world);
            return status;
        }

        public static Status Save(PlayerProfile playerProfile)
        {
            Status status = SaveCustomNames(playerProfile);
            return status;
        }


        public static Status Load(PlayerProfile playerProfile)
        {
            Status status = LoadCustomNames(playerProfile);
            return status;
        }

        public static Status Load(World world)
        {
            Status status = LoadPinnedObjects(world);
            return status;
        }
        #endregion

        #region Private Methods
        private static Status LoadPinnedObjects(World world)
        {
            PinnedObjects.Clear();

            var pinnedPath = GetPath(world);

            FileReader fileReader = null;
            try
            {
                fileReader = new FileReader(pinnedPath, world.m_fileSource);
            }
            catch
            {
                fileReader?.Dispose();
                QuickPingPlugin.Log.LogWarning($"Failed to load pinned objects. World: {world.m_name}");
                return Status.NoData;
            }


            BinaryReader binary = fileReader.m_binary;
            int count = binary.ReadInt32();
            ZPackage zPackage = new ZPackage(binary.ReadBytes(count));

            PinnedObjects = UnpackPinnedObjects(zPackage);
            return Status.Success;
        }
        private static Status SavePinnedObjects(World world)
        {

            ZPackage zPackage = PackPinnedObjects();
            var fileSource = world.m_fileSource;

            bool cloudSaveFailed;
            if (fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(World.GetWorldSavePath(fileSource));
            }

            string metaPath = GetPath(world);
            string text = metaPath + ".new";
            string oldFile = metaPath + ".old";
            byte[] array = zPackage.GetArray();
            FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, fileSource);
            fileWriter.m_binary.Write(array.Length);
            fileWriter.m_binary.Write(array);
            fileWriter.Finish();


            cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && fileSource == FileHelpers.FileSource.Cloud;
            if (!cloudSaveFailed)
            {
                FileHelpers.ReplaceOldFile(metaPath, text, oldFile, fileSource);
            }

            return cloudSaveFailed && fileSource == FileHelpers.FileSource.Cloud ? Status.Failed : Status.Success;
        }

        private static Status LoadCustomNames(PlayerProfile playerProfile)
        {
            CustomNames.Clear();
            var customNamesPath = GetPath(playerProfile);
            var fileSource = playerProfile.m_fileSource;

            FileReader fileReader = null;
            try
            {
                fileReader = new FileReader(customNamesPath, fileSource);
            }
            catch
            {
                fileReader?.Dispose();
                QuickPingPlugin.Log.LogWarning($"Failed to load custom names. Profile: {playerProfile.m_playerName}");
                return Status.NoData;
            }


            BinaryReader binary = fileReader.m_binary;
            int count = binary.ReadInt32();
            ZPackage zPackage = new ZPackage(binary.ReadBytes(count));

            CustomNames = UnpackCustomNames(zPackage);
            return Status.Success;
        }

        private static Status SaveCustomNames(PlayerProfile playerProfile)
        {

            ZPackage zPackage = PackCustomNames();
            var fileSource = playerProfile.m_fileSource;
            bool cloudSaveFailed;
            if (fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(World.GetWorldSavePath(fileSource));
            }

            string metaPath = GetPath(playerProfile);
            string text = metaPath + ".new";
            string oldFile = metaPath + ".old";
            byte[] array = zPackage.GetArray();
            FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, fileSource);
            fileWriter.m_binary.Write(array.Length);
            fileWriter.m_binary.Write(array);
            fileWriter.Finish();


            cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && fileSource == FileHelpers.FileSource.Cloud;
            if (!cloudSaveFailed)
            {
                FileHelpers.ReplaceOldFile(metaPath, text, oldFile, fileSource);
            }

            return cloudSaveFailed && fileSource == FileHelpers.FileSource.Cloud ? Status.Failed : Status.Success;
        }


        private static string GetPath(World world)
        {
            FileHelpers.SplitFilePath(world.GetDBPath(), out string directory, out string fileName, out string fileExtension);
            var worldSavePath = directory + fileName;
            string __MyExtension = ".mod.quickping.pinned";


            return worldSavePath + __MyExtension;
        }

        public static string GetPath(PlayerProfile playerProfile)
        {
            FileHelpers.SplitFilePath(playerProfile.GetFilename(), out string directory, out string fileName, out string fileExtension);
            var worldSavePath = directory + fileName;
            string __MyExtension = $".mod.quickping.custom_names";

            return worldSavePath + __MyExtension;
        }

        public static void StatusCheck(Status status)
        {
            switch (status)
            {
                case Status.Success:
                    QuickPingPlugin.Log.LogInfo("Save/Load successful");
                    break;
                case Status.Failed:
                    QuickPingPlugin.Log.LogWarning("Save/Load failed");
                    break;
                case Status.NoData:
                    QuickPingPlugin.Log.LogWarning("No data to save/load");
                    break;
                default:
                    break;
            }
        }

        #endregion


        #region Packaging

        /// <summary>
        /// Pack ZDOID, name, type and position in ZPackage and returns it.
        /// </summary>
        /// <returns>ZPackage containing ZDOID, name, type and position</returns>
        private static ZPackage PackPinnedObjects()
        {
            ZPackage zPackage = new ZPackage();

            foreach (var x in PinnedObjects)
            {
                zPackage.Write(PackPinnedObject(new PinnedObject
                {
                    ZDOID = x.Key,
                    PinData = x.Value
                }));
            }
            return zPackage;
        }

        public static ZPackage PackPinnedObject(PinnedObject pinnedObject)
        {
            ZPackage res = new ZPackage();
            res.Write(pinnedObject.ZDOID);
            res.Write(pinnedObject.PinData);
            return res;
        }


        /// <summary>
        /// Pack originalName=name in ZPackage and returns it.
        /// </summary>
        /// <returns></returns>
        private static ZPackage PackCustomNames()
        {
            ZPackage zPackage = new ZPackage();

            foreach (var x in CustomNames)
            {
                if (x.Key is null or "") continue;

                zPackage.Write(PackCustomName(x));
            }

            return zPackage;
        }

        private static ZPackage PackCustomName(KeyValuePair<string, string> x)
        {
            ZPackage res = new ZPackage();
            res.Write(x.Key);
            res.Write(x.Value);
            return res;
        }

        private static Dictionary<string, string> UnpackCustomNames(ZPackage zPackage)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            while (zPackage.GetPos() < zPackage.Size())
            {
                var data = UnpackCustomName(zPackage);
                if (data.Original is not "" or null)
                    res.Add(data.Original, data.Custom);
            }
            return res;
        }

        public static CustomName UnpackCustomName(ZPackage zPackage)
        {
            string originalName = zPackage.ReadString();
            string name = zPackage.ReadString();
            return new CustomName
            {
                Original = originalName,
                Custom = name
            };

        }

        public static Dictionary<ZDOID, Minimap.PinData> UnpackPinnedObjects(ZPackage zPackage)
        {
            Dictionary<ZDOID, Minimap.PinData> res = new Dictionary<ZDOID, Minimap.PinData>();

            if (zPackage == null) return null;

            while (zPackage.GetPos() < zPackage.Size())
            {
                var data = UnpackPinnedObject(zPackage);
                if (!res.ContainsKey(data.ZDOID))
                    res.Add(data.ZDOID, data.PinData);

            }

            return res;
        }

        public static PinnedObject UnpackPinnedObject(ZPackage zPackage)
        {
            ZDOID zdoid = zPackage.ReadZDOID();
            Minimap.PinData pinData = zPackage.ReadPinData();
            return new PinnedObject
            {
                ZDOID = zdoid,
                PinData = pinData
            };
        }

        #endregion


        #region Extensions
        private static void Write(this ZPackage zPackage, Minimap.PinData pinData)
        {
            pinData.m_name ??= "";
            zPackage.Write(pinData.m_name);
            zPackage.Write(pinData.m_type.ToString());
            zPackage.Write(pinData.m_pos);
        }

        private static Minimap.PinData ReadPinData(this ZPackage package)
        {
            Minimap.PinData pinData = new()
            {
                m_name = package.ReadString(),
                m_type = (Minimap.PinType)Enum.Parse(typeof(Minimap.PinType), package.ReadString()),
                m_pos = package.ReadVector3()
            };
            return pinData;
        }
        #endregion


    }


}
