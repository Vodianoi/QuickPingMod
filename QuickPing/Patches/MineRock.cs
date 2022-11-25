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

    [HarmonyPatch(typeof(WearNTear))]
    internal static class WearNTear_Patch
    {
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
        [HarmonyPostfix]
        public static void Awake(WearNTear __instance)
        {
            var pos = __instance.transform.position;
            if (Minimap_Patch.FindPin(pos, Minimap.PinType.Icon4) is Minimap.PinData pin)
            {
                __instance.m_onDamaged += () => Minimap.instance.RemovePin(pin);
                QuickPing.Log.LogWarning($"Removed ping at x:{pos.x}, y:{pos.y}, z:{pos.z}");
            }
        }
    }

    [HarmonyPatch(typeof(Destructible))]
    internal static class Destructible_Patch
    {
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
        [HarmonyPostfix]
        public static void Awake(Destructible __instance)
        {
            var pos = __instance.transform.position;
            if (Minimap_Patch.FindPin(pos) is Minimap.PinData pin)
            {
                __instance.m_onDamaged += () => Minimap.instance.RemovePin(pin);
                QuickPing.Log.LogWarning($"Removed ping at x:{pos.x}, y:{pos.y}, z:{pos.z}");
            }
        }
    }

}
