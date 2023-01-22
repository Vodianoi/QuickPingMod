using HarmonyLib;
using QuickPing.Utilities;
using System;

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
        private static void OnNewConnection(ZNetPeer peer, ZNet __instance)
        {

            QuickPingPlugin.Log.LogInfo($"New connection : {peer.m_socket.GetHostName()}");
            if (__instance.IsServer())
            {
                //Init for clients
                peer.m_rpc.Register("OnClientHandshake", new Action<ZRpc, ZPackage>(Sync.OnClientHandshake));
                //PinnedObjects
                peer.m_rpc.Register("OnClientAddPinnedObject", new Action<ZRpc, ZPackage>(Sync.OnClientAddPinnedObject));
                peer.m_rpc.Register("OnClientRemovePinnedObject", new Action<ZRpc, ZPackage>(Sync.OnClientRemovePinnedObject));


            }
            else
            {
                //Init for server
                peer.m_rpc.Register("OnServerHandshake", new Action<ZRpc, ZPackage>(Sync.OnServerHandshake));
                //PinnedObjects
                peer.m_rpc.Register("OnServerAddPinnedObject", new Action<ZRpc, ZPackage>(Sync.OnServerAddPinnedObject));
                peer.m_rpc.Register("OnServerRemovePinnedObject", new Action<ZRpc, ZPackage>(Sync.OnServerRemovePinnedObject));
            }
        }
    }
}