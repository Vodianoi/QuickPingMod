using HarmonyLib;

namespace QuickPing.Patches
{
    [HarmonyPatch(typeof(MineRock5))]
    internal static class MineRock5_Patch
    {
        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.AllDestroyed))]
        [HarmonyPostfix]
        public static void AllDestroyed(MineRock5 __instance, bool __result)
        {
            if (!__result) return;

            var pos = __instance.transform.position;
            if (Minimap_Patch.RemovePin(pos, Minimap.PinType.Icon2))
            {
                QuickPing.Log.LogWarning($"Removed ping at x:{pos.x}, y:{pos.y}, z:{pos.z}");
            }
        }
    }
}
