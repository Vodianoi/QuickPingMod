using HarmonyLib;
using QuickPing.Utilities;
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
                if (DataManager.PinnedObjects.ContainsKey(id))
                {
                    Minimap.instance.RemovePin(DataManager.PinnedObjects[id]);
                    //Minimap_Patch.PinnedObjects.Remove(id);
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
                if (DataManager.PinnedObjects.ContainsKey(id))
                {
                    Minimap.instance.RemovePin(DataManager.PinnedObjects[id]);
                    //Minimap_Patch.PinnedObjects.Remove(id);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Destructible))]
    internal static class Destructible_Patch
    {
        /// <summary>
        /// Complete patch, check if Destructible place something on destroy, if true change zdoid of PinnedObjects to ondestroy object
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
                    if (!DataManager.PinnedObjects.ContainsKey(component.GetZDO().m_uid)
                        && DataManager.PinnedObjects.ContainsKey(id))
                    {
                        ZDOID zdoid = component.GetZDO().m_uid;
                        Minimap.PinData pinData = DataManager.PinnedObjects[id];
                        DataManager.PinnedObjects.Add(zdoid, pinData);
                        DataManager.PinnedObjects.Remove(id);
                    }
                    else if (DataManager.PinnedObjects.ContainsKey(id))
                    {
                        DataManager.PinnedObjects.Remove(id);
                    }
                }
            }
            else if (DataManager.PinnedObjects.ContainsKey(id))
                Minimap.instance.RemovePin(DataManager.PinnedObjects[id]);

            __instance.m_onDestroyed?.Invoke();


            ZNetScene.instance.Destroy(__instance.gameObject);
            __instance.m_destroyed = true;

            return false;
        }

        [HarmonyPatch(typeof(Character))]
        internal static class Character_Patch
        {
            // Patch for Character to remove pin on death
            [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
            [HarmonyPrefix]
            public static void OnDeath(Character __instance)
            {
                if (__instance.m_nview)
                {
                    var id = __instance.m_nview.GetZDO().m_uid;
                    if (DataManager.PinnedObjects.ContainsKey(id))
                    {
                        Minimap.instance.RemovePin(DataManager.PinnedObjects[id]);
                        //Minimap_Patch.PinnedObjects.Remove(id);
                    }
                }
            }
        }
    }
}
