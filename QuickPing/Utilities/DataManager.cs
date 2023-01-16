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

        internal struct PinnedObject
        {
            public ZDOID ZDOID { get; set; }
            public Minimap.PinData PinData { get; set; }
        }


        #region Public Methods
        public static bool Save(World world, PlayerProfile playerProfile)
        {
            bool cloudSaveNamesFailed = SaveCustomNames(world, playerProfile);
            bool cloudSavePinsFailed = SavePinnedObjects(world, playerProfile);
            return cloudSaveNamesFailed || cloudSavePinsFailed;
        }


        public static void Load(World world, PlayerProfile playerProfile)
        {
            LoadCustomNames(world, playerProfile);
            LoadPinnedObjects(world, playerProfile);
        }
        #endregion

        #region Private Methods
        private static void LoadPinnedObjects(World world, PlayerProfile playerProfile)
        {
            PinnedObjects.Clear();

            var pinnedPath = GetPath(world, playerProfile, "pinned");

            FileReader fileReader = null;
            try
            {
                fileReader = new FileReader(pinnedPath, world.m_fileSource);
            }
            catch
            {
                fileReader?.Dispose();
                QuickPingPlugin.Log.LogWarning($"Failed to load pinned objects. World: {world.m_name} - Profile: {playerProfile.m_playerName}");
                //return new World(__name, loadError: true, versionError: false, __fileSource);
                return;
            }


            BinaryReader binary = fileReader.m_binary;
            int count = binary.ReadInt32();
            ZPackage zPackage = new ZPackage(binary.ReadBytes(count));

            PinnedObjects = UnpackPinnedObjects(zPackage);
        }
        private static bool SavePinnedObjects(World world, PlayerProfile playerProfile)
        {

            ZPackage zPackage = PackPinnedObjects();


            bool cloudSaveFailed;
            if (world.m_fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(World.GetWorldSavePath(world.m_fileSource));
            }

            string metaPath = GetPath(world, playerProfile, "pinned");
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

        private static void LoadCustomNames(World world, PlayerProfile playerProfile)
        {
            CustomNames.Clear();
            var customNamesPath = GetPath(world, playerProfile, "customNames");

            FileReader fileReader = null;
            try
            {
                fileReader = new FileReader(customNamesPath, world.m_fileSource);
            }
            catch
            {
                fileReader?.Dispose();
                QuickPingPlugin.Log.LogWarning($"Failed to load custom names. World: {world.m_name} - Profile: {playerProfile.m_playerName}");
                return;
            }


            BinaryReader binary = fileReader.m_binary;
            int count = binary.ReadInt32();
            ZPackage zPackage = new ZPackage(binary.ReadBytes(count));

            CustomNames = UnpackCustomNames(zPackage);
        }

        private static bool SaveCustomNames(World world, PlayerProfile playerProfile)
        {

            ZPackage zPackage = PackCustomNames();

            bool cloudSaveFailed;
            if (world.m_fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(World.GetWorldSavePath(world.m_fileSource));
            }

            string metaPath = GetPath(world, playerProfile, "customNames");
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


        private static string GetPath(World world, PlayerProfile playerProfile, string extension)
        {
            FileHelpers.SplitFilePath(world.GetDBPath(), out string directory, out string fileName, out string fileExtension);
            var worldSavePath = directory + fileName;
            string __MyExtension = $".{playerProfile.m_playerName}.mod.quickping.{extension}";


            //INFO
            //QuickPingPlugin.Log.LogInfo("World .pinned save path: " + worldSavePath);

            return worldSavePath + __MyExtension;
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
                zPackage.Write(PackPinnedObject(x));
            }
            return zPackage;
        }

        public static ZPackage PackPinnedObject(ZDOID zdoid, Minimap.PinData pinData) => PackPinnedObject(new KeyValuePair<ZDOID, Minimap.PinData>(zdoid, pinData));
        public static ZPackage PackPinnedObject(KeyValuePair<ZDOID, Minimap.PinData> x)
        {
            ZPackage res = new ZPackage();
            res.Write(x.Key);
            res.Write(x.Value);
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
