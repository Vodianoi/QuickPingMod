using HarmonyLib;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using static Attack;

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

            if (__instance.m_nview)
            {
                var pos = __instance.transform.position;
                __instance.m_nview.InvokeRPC("RemovePin", pos, Minimap.PinType.Icon2);
            }

        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Start))]
        [HarmonyPostfix]
        public static void Start(MineRock5 __instance)
        {
            if ((bool)__instance.m_nview && __instance.m_nview.GetZDO() != null)
            {
                if (Minimap_Patch.FindPin(__instance.transform.position) != null)
                    if (!__instance.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                        __instance.m_nview.Register<Vector3>("RemovePin", Minimap_Patch.RPC_RemovePin);
            }
        }


    }

    [HarmonyPatch(typeof(WearNTear))]
    internal static class WearNTear_Patch
    {
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
        [HarmonyPrefix]
        public static void Destroy(WearNTear __instance)
        {
            if ((bool)__instance.m_nview && __instance.m_nview.GetZDO() != null)
            {
                var pos = __instance.transform.position;
                __instance.m_nview.InvokeRPC("RemovePin", pos);

            }

        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
        [HarmonyPostfix]
        public static void Awake(WearNTear __instance)
        {
            if ((bool)__instance.m_nview && __instance.m_nview.GetZDO() != null)
            {
                if (Minimap_Patch.FindPin(__instance.transform.position) != null)
                    if (!__instance.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                        __instance.m_nview.Register<Vector3>("RemovePin", Minimap_Patch.RPC_RemovePin);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible))]
    internal static class Destructible_Patch
    {
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
        [HarmonyPrefix]
        public static bool Destroy(Destructible __instance, Vector3 hitPoint, Vector3 hitDir)
        {

            __instance.CreateDestructionEffects(hitPoint, hitDir);
            if (__instance.m_destroyNoise > 0f)
            {
                Player closestPlayer = Player.GetClosestPlayer(__instance.transform.position, 10f);
                if ((bool)closestPlayer)
                {
                    closestPlayer.AddNoise(__instance.m_destroyNoise);
                }
            }

            if ((bool)__instance.m_spawnWhenDestroyed)
            {
                GameObject obj = UnityEngine.Object.Instantiate(__instance.m_spawnWhenDestroyed, __instance.transform.position, __instance.transform.rotation);
                ZNetView component = obj.GetComponent<ZNetView>();
                component.SetLocalScale(__instance.transform.localScale);
                component.GetZDO().SetPGWVersion(__instance.m_nview.GetZDO().GetPGWVersion());
                Gibber component2 = obj.GetComponent<Gibber>();
                if ((bool)component2)
                {
                    component2.Setup(hitPoint, hitDir);
                }
                if (obj.TryGetComponent(out MineRock5 mineRock))
                {
                    if (!component.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                        component.Register<Vector3>("RemovePin", Minimap_Patch.RPC_RemovePin);
                }
            }
            else
            {
                var pos = __instance.transform.position;
                __instance.m_nview.InvokeRPC("RemovePin", pos);
            }

            __instance.m_onDestroyed?.Invoke();


            ZNetScene.instance.Destroy(__instance.gameObject);
            __instance.m_destroyed = true;

            return false;


        }

        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
        [HarmonyPostfix]
        public static void Awake(Destructible __instance)
        {
            if ((bool)__instance.m_nview && __instance.m_nview.GetZDO() != null)
            {
                if (Minimap_Patch.FindPin(__instance.transform.position) != null)
                    if (!__instance.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                        __instance.m_nview.Register<Vector3>("RemovePin", Minimap_Patch.RPC_RemovePin);
            }
        }


    }

}
