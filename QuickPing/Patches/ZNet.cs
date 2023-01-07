using HarmonyLib;
using System.IO;

namespace QuickPing.Patches
{
    public class ZNet_Patch
    {
        [HarmonyPatch(typeof(ZNet), "LoadWorld")]
        [HarmonyPostfix]
        private static void LoadWorld(ZNet __instance)
        {
            Minimap_Patch.PinnedObjects.Clear();
            var world = Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;



            var pinnedPath = Utilities.DataUtils.GetPinnedPath(world);

            FileReader fileReader = null;
            try
            {
                fileReader = new FileReader(pinnedPath, world.m_fileSource);
            }
            catch
            {
                fileReader?.Dispose();
                ZLog.Log("  failed to load " + world.m_name);
                //return new World(__name, loadError: true, versionError: false, __fileSource);
                return;
            }


            BinaryReader binary = fileReader.m_binary;
            int count = binary.ReadInt32();
            ZPackage zPackage = new ZPackage(binary.ReadBytes(count));

            Minimap_Patch.PinnedObjects = Utilities.DataUtils.ReadPinnedObjects(zPackage);

        }



        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread))]
        [HarmonyPostfix]
        private static void SaveWorldThread()
        {
            var world = Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
            Minimap_Patch.SavePinnedDataToWorld(world, out bool cloudSaveFailed);

            QuickPingPlugin.Log.LogInfo($"Cloud save : {!cloudSaveFailed}");


        }

    }

}