using HarmonyLib;
using QuickPing.Utilities;

namespace QuickPing.Patches
{
    public class ZNet_Patch
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.LoadWorld))]
        [HarmonyPostfix]
        private static void LoadWorld(ZNet __instance)
        {
            if (!__instance.IsServer()) return;
            DataManager.Status worldSaveStatus = DataManager.Load(ZNet.m_world);
            DataManager.Status playerSaveStatus = DataManager.Load(Game.instance.GetPlayerProfile());

            LogManager.Log(worldSaveStatus);
            LogManager.Log(playerSaveStatus);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Save))]
        [HarmonyPostfix]
        private static void Save(ZNet __instance)
        {
            if (!__instance.IsServer()) return;
            DataManager.Status worldLoadStatus = DataManager.Save(ZNet.m_world);
            DataManager.Status playerLoadStatus = DataManager.Save(Game.instance.GetPlayerProfile());


            LogManager.Log(worldLoadStatus);
            LogManager.Log(playerLoadStatus);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        [HarmonyPostfix]
        private static void OnNewConnection(ZNetPeer peer)
        {
            if (peer.IsReady())
                QuickPingPlugin.Instance.RPC_Handshake.SendPackage(peer.m_uid, DataManager.PackPinnedObjects());
        }


    }
}