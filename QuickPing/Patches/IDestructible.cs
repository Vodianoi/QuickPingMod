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
                }
            }
        }
    }

    [HarmonyPatch(typeof(Destructible))]
    internal static class Destructible_Patch
    {
        /// <summary>
        /// Full replacement of Destructible.Destroy (kept in sync with vanilla 0.221).
        /// Adds pin handling: if the destructible spawns a MineRock5 (e.g. ore deposit),
        /// move the pin to the spawned object's ZDOID; otherwise remove the pin.
        /// </summary>
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
        [HarmonyPrefix]
        public static bool Destroy(Destructible __instance, HitData hit)
        {
            // --- vanilla ---
            Vector3 hitPoint = hit?.m_point ?? Vector3.zero;
            Vector3 hitDir = hit?.m_dir ?? Vector3.zero;
            Character hitCharacter = hit?.GetAttacker();
            __instance.CreateDestructionEffects(hitPoint, hitDir, hitCharacter);
            if (__instance.m_destroyNoise > 0f && (hit == null || hit.m_hitType != HitData.HitType.CinderFire))
            {
                Player closestPlayer = Player.GetClosestPlayer(__instance.transform.position, 10f);
                if ((bool)closestPlayer)
                {
                    closestPlayer.AddNoise(__instance.m_destroyNoise);
                }
            }

            bool cheated = false;
            if (hit?.GetAttacker() is Player player && (bool)player)
            {
                cheated = player.GetInventory().CheatedDamagingItemEquipped() && !PlayerProfile.s_bypassCheatChecks;
                if (cheated)
                {
                    __instance.m_nview.GetZDO().SetCheated(isCheated: true);
                }
            }

            // --- mod ---
            ZDOID id = __instance.m_nview.GetZDO().m_uid;

            if ((bool)__instance.m_spawnWhenDestroyed)
            {
                // --- vanilla ---
                GameObject obj = UnityEngine.Object.Instantiate(__instance.m_spawnWhenDestroyed, __instance.transform.position, __instance.transform.rotation);
                ZNetView component = obj.GetComponent<ZNetView>();
                if ((object)component != null)
                {
                    component.SetLocalScale(__instance.transform.localScale);
                    if (cheated)
                    {
                        component.GetZDO().SetCheated(isCheated: true);
                    }
                }
                obj.GetComponent<Gibber>()?.Setup(hitPoint, hitDir);

                // --- mod: move the pin to the spawned rock ---
                if (obj.TryGetComponent(out MineRock5 _) && component != null && DataManager.PinnedObjects.TryGetValue(id, out Minimap.PinData pinData))
                {
                    ZDOID newId = component.GetZDO().m_uid;
                    if (!DataManager.PinnedObjects.ContainsKey(newId))
                        DataManager.PinnedObjects.Add(newId, pinData);
                    DataManager.PinnedObjects.Remove(id);
                }

                // --- vanilla ---
                if (hit != null)
                {
                    obj.GetComponent<MineRock5>()?.Damage(hit);
                }
            }
            else if (DataManager.PinnedObjects.TryGetValue(id, out Minimap.PinData pinData))
            {
                Minimap.instance.RemovePin(pinData);
            }

            // --- vanilla ---
            __instance.m_onDestroyed?.Invoke();
            ZNetScene.instance.Destroy(__instance.gameObject);
            __instance.m_destroyed = true;

            return false;
        }
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
                }
            }
        }
    }

    [HarmonyPatch(typeof(Leviathan))]
    internal static class Leviathan_Patch
    {
        // A Leviathan dives away for good after being mined: remove its pin
        [HarmonyPatch(typeof(Leviathan), nameof(Leviathan.RPC_Left))]
        [HarmonyPostfix]
        public static void RPC_Left(Leviathan __instance)
        {
            if (__instance.m_nview && __instance.m_nview.GetZDO() != null)
            {
                var id = __instance.m_nview.GetZDO().m_uid;
                if (DataManager.PinnedObjects.ContainsKey(id))
                {
                    Minimap.instance.RemovePin(DataManager.PinnedObjects[id]);
                }
            }
        }
    }

}
