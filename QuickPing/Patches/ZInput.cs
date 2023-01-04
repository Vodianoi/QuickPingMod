using HarmonyLib;

namespace QuickPing.Patches
{
    internal class ZInput_Patch
    {

        [HarmonyPatch(typeof(ZInput))]
        [HarmonyPatch(nameof(ZInput.Reset))]
        [HarmonyPostfix]
        public static void Reset(ZInput __instance)
        {
            Settings.AddInputs(__instance);
        }
    }
}
