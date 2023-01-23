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
            DataManager.Status status = DataManager.Load(ZNet.m_world);

            DataManager.StatusCheck(status);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Save))]
        [HarmonyPostfix]
        private static void Save(ZNet __instance)
        {
            if (!__instance.IsServer()) return;
            DataManager.Status status = DataManager.Save(ZNet.m_world);

            DataManager.StatusCheck(status);
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