using HarmonyLib;
using UnityEngine;

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
                var id = __instance.m_nview.GetZDO().m_uid;
                if (Minimap_Patch.PinnedObjects.ContainsKey(id))
                {
                    Minimap_Patch.RemovePin(Minimap_Patch.PinnedObjects[id]);
                    Minimap_Patch.PinnedObjects.Remove(__instance.m_nview.GetZDO().m_uid);
                }
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
                var id = __instance.m_nview.GetZDO().m_uid;
                if (Minimap_Patch.PinnedObjects.ContainsKey(id))
                {
                    Minimap_Patch.RemovePin(Minimap_Patch.PinnedObjects[id]);
                    Minimap_Patch.PinnedObjects.Remove(__instance.m_nview.GetZDO().m_uid);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Destructible))]
    internal static class Destructible_Patch
    {
        /// <summary>
        /// Complete repatch, check if Destructible place something on destroy, if true change zdoid of PinnedObjects to ondestroy object
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="hitPoint"></param>
        /// <param name="hitDir"></param>
        /// <returns></returns>
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

            var id = __instance.m_nview.GetZDO().m_uid;
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
                if (obj.TryGetComponent(out MineRock5 _))
                {
                    if (!Minimap_Patch.PinnedObjects.ContainsKey(component.GetZDO().m_uid)
                        && Minimap_Patch.PinnedObjects.ContainsKey(id))
                    {
                        ZDOID zdoid = component.GetZDO().m_uid;
                        Minimap.PinData pinData = Minimap_Patch.PinnedObjects[id];
                        Minimap_Patch.PinnedObjects.Add(zdoid, pinData);
                        Minimap_Patch.PinnedObjects.Remove(id);
                    }
                    else if (Minimap_Patch.PinnedObjects.ContainsKey(id))
                    {
                        Minimap_Patch.PinnedObjects.Remove(id);
                    }
                }
            }
            else
            {
                if (Minimap_Patch.PinnedObjects.ContainsKey(id))
                {
                    Minimap_Patch.RemovePin(Minimap_Patch.PinnedObjects[id]);
                    Minimap_Patch.PinnedObjects.Remove(id);
                }
            }

            __instance.m_onDestroyed?.Invoke();


            ZNetScene.instance.Destroy(__instance.gameObject);
            __instance.m_destroyed = true;

            return false;
        }
    }
}
