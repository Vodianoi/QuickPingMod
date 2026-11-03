using HarmonyLib;
using QuickPing.Utilities;

namespace QuickPing.Patches
{
    internal static class SaveCollection_Patch
    {
        private static bool repaired;

        /// <summary>
        /// Repair misnamed QuickPing cloud files once, before the game lists the saves
        /// (they would otherwise crash the world list).
        /// </summary>
        [HarmonyPatch(typeof(SaveCollection), nameof(SaveCollection.Reload))]
        [HarmonyPrefix]
        private static void Reload(SaveCollection __instance)
        {
            if (repaired || __instance.m_dataType != SaveDataType.World) return;
            repaired = true;
            DataManager.RepairMisnamedCloudFiles();
        }
    }
}
