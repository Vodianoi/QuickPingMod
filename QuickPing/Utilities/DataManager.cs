using System;
using System.Collections.Generic;
using Splatform;
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

        #region Public Methods
        public static bool Save(World world, PlayerProfile playerProfile)
        {
            bool cloudSaveNamesFailed = WritePackage(GetPath(world, playerProfile, "customNames"), world.m_fileSource, PackCustomNames());
            bool cloudSavePinsFailed = WritePackage(GetPath(world, playerProfile, "pinned"), world.m_fileSource, PackPinnedObjects());
            return cloudSaveNamesFailed || cloudSavePinsFailed;
        }


        public static void Load(World world, PlayerProfile playerProfile)
        {
            CustomNames.Clear();
            ZPackage names = ReadPackage(GetPath(world, playerProfile, "customNames"), world.m_fileSource);
            if (names != null)
                UnpackCustomNames(names);
            else
                QuickPingPlugin.Log.LogWarning($"Failed to load custom names. World: {world.m_name} - Profile: {playerProfile.m_playerName}");

            PinnedObjects.Clear();
            ZPackage pins = ReadPackage(GetPath(world, playerProfile, "pinned"), world.m_fileSource);
            if (pins != null)
                UnpackPinnedObjects(pins);
            else
                QuickPingPlugin.Log.LogWarning($"Failed to load pinned objects. World: {world.m_name} - Profile: {playerProfile.m_playerName}");
        }

        /// <summary>
        /// Earlier builds for Valheim 1.0.21 dropped the '/' after the worlds folder and wrote
        /// "worlds&lt;World&gt;.&lt;Player&gt;.mod.quickping.*" at the Steam Cloud root. The game then lists them
        /// as world saves and the world list crashes ("Invalid path"). Move them back into "worlds/".
        /// </summary>
        public static void RepairMisnamedCloudFiles()
        {
            const FileHelpers.FileSource cloud = FileHelpers.FileSource.Cloud;
            if (!FileHelpers.CloudStorageSupported || !FileHelpers.CloudStorageSupportedAndEnabled) return;

            string[] files;
            try
            {
                // Same listing as the game's world list (SaveCollection.GetAllFilesInSource)
                files = FileHelpers.GetFiles(cloud, SaveSystem.GetSavePath(SaveDataType.World, cloud)) ?? Array.Empty<string>();
            }
            catch (Exception e)
            {
                QuickPingPlugin.Log.LogWarning($"Could not list cloud files: {e.Message}");
                return;
            }

            string root = SaveSystem.GetWorldsSaveRootPath(cloud).Trim('/'); // "worlds"
            foreach (string file in files)
            {
                string name = file.Replace('\\', '/').TrimStart('/');
                if (name.Contains("/") || !name.StartsWith(root) || !name.Contains(".mod.quickping.")) continue;

                string target = root + "/" + name.Substring(root.Length);
                try
                {
                    if (!name.EndsWith(".old"))
                    {
                        ZPackage zPackage = ReadPackage(file, cloud);
                        if (zPackage == null || WritePackage(target, cloud, zPackage))
                        {
                            QuickPingPlugin.Log.LogWarning($"Could not move misnamed cloud file {file} to {target}");
                            continue;
                        }
                    }
                    FileHelpers.Delete(file, cloud);
                    QuickPingPlugin.Log.LogInfo($"Moved misnamed cloud file {file} to {target}");
                }
                catch (Exception e)
                {
                    QuickPingPlugin.Log.LogWarning($"Could not repair misnamed cloud file {file}: {e.Message}");
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Read a ZPackage written by <see cref="WritePackage"/>. Returns null if the file is missing or unreadable.
        /// </summary>
        private static ZPackage ReadPackage(string path, FileHelpers.FileSource fileSource)
        {
            FileReader fileReader;
            try
            {
                fileReader = new FileReader(path, fileSource);
            }
            catch
            {
                return null;
            }

            try
            {
                BinaryReader binary = fileReader.m_binary;
                int count = binary.ReadInt32();
                return new ZPackage(binary.ReadBytes(count));
            }
            catch
            {
                return null;
            }
            finally
            {
                fileReader.Dispose();
            }
        }

        /// <summary>
        /// Write a ZPackage next to the world save (via .new/.old swap, like vanilla).
        /// </summary>
        /// <returns>true if the cloud save failed</returns>
        private static bool WritePackage(string path, FileHelpers.FileSource fileSource, ZPackage zPackage)
        {
            if (fileSource != FileHelpers.FileSource.Cloud)
            {
                Directory.CreateDirectory(SaveSystem.GetWorldsSaveRootPath(fileSource));
            }

            string newFile = path + ".new";
            string oldFile = path + ".old";
            byte[] array = zPackage.GetArray();
            FileWriter fileWriter = new FileWriter(newFile, CloudStorageFileGrouping.SameFolder, FileHelpers.FileHelperType.Binary, fileSource);
            fileWriter.m_binary.Write(array.Length);
            fileWriter.m_binary.Write(array);
            fileWriter.Finish();

            bool cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && fileSource == FileHelpers.FileSource.Cloud;
            if (!cloudSaveFailed)
            {
                FileHelpers.ReplaceOldFile(path, newFile, oldFile, CloudStorageFileGrouping.SameFolder, fileSource);
            }

            return cloudSaveFailed;
        }

        /// <summary>
        /// "&lt;worlds folder&gt;/&lt;World&gt;.&lt;Player&gt;.mod.quickping.&lt;extension&gt;", next to the world's .db
        /// (built like World.GetDBPath()).
        /// </summary>
        private static string GetPath(World world, PlayerProfile playerProfile, string extension)
        {
            return $"{SaveSystem.GetWorldsSaveRootPath(world.m_fileSource)}/{world.m_worldName}.{playerProfile.m_playerName}.mod.quickping.{extension}";
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
                zPackage.Write(x.Key);
                zPackage.Write(x.Value);
            }
            return zPackage;
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
                zPackage.Write(x.Key);
                zPackage.Write(x.Value);
            }

            return zPackage;
        }

        private static void UnpackCustomNames(ZPackage zPackage)
        {
            CustomNames.Clear();
            while (zPackage.GetPos() < zPackage.Size())
            {
                string originalName = zPackage.ReadString();
                string name = zPackage.ReadString();
                if (originalName is not (null or ""))
                    CustomNames.Add(originalName, name);
            }
        }

        private static void UnpackPinnedObjects(ZPackage zPackage)
        {
            Dictionary<ZDOID, Minimap.PinData> res = new Dictionary<ZDOID, Minimap.PinData>();

            if (zPackage == null) return;

            while (zPackage.GetPos() < zPackage.Size())
            {
                ZDOID zdoid = zPackage.ReadZDOID();
                Minimap.PinData pinData = zPackage.ReadPinData();
                if (!res.ContainsKey(zdoid))
                    res.Add(zdoid, pinData);

            }

            PinnedObjects = res;
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
            return new()
            {
                m_name = package.ReadString(),
                m_type = (Minimap.PinType)Enum.Parse(typeof(Minimap.PinType), package.ReadString()),
                m_pos = package.ReadVector3()
            };
        }
        #endregion


    }
}
