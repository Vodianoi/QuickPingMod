using HarmonyLib;
using QuickPing.Utilities;
using System;

namespace QuickPing.Patches
{
    public class ZNet_Patch
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
        [HarmonyPostfix]
        private static void Start()
        {
            DataManager.Load(ZNet.m_world, Game.instance.GetPlayerProfile());
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Save))]
        [HarmonyPostfix]
        private static void Save()
        {
            bool cloudSaveFailed = DataManager.Save(ZNet.m_world, Game.instance.GetPlayerProfile());

            QuickPingPlugin.Log.LogInfo($"cloud save : {!cloudSaveFailed}");
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        [HarmonyPostfix]
        private static void OnNewConnection(ZNetPeer peer, ZNet __instance)
        {

            QuickPingPlugin.Log.LogInfo($"New connection : {peer.m_socket.GetHostName()}");
            if (__instance.IsServer())
            {
                //Init for clients
                peer.m_rpc.Register("OnClientInitializeSync", new Action<ZRpc, ZPackage>(Sync.OnClientInitializeSync));
                //PinnedObjects
                peer.m_rpc.Register("OnClientAddPinnedObject", new Action<ZRpc, ZPackage>(Sync.OnClientAddPinnedObject));
                peer.m_rpc.Register("OnClientRemovePinnedObject", new Action<ZRpc, ZPackage>(Sync.OnClientRemovePinnedObject));


            }
            else
            {
                //PinnedObjects
                peer.m_rpc.Register("OnServerAddPinnedObject", new Action<ZRpc, ZPackage>(Sync.OnServerAddPinnedObject));
                peer.m_rpc.Register("OnServerRemovePinnedObject", new Action<ZRpc, ZPackage>(Sync.OnServerRemovePinnedObject));
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
        [HarmonyPostfix]
        private static void Disconnect(ZNetPeer peer, ZNet __instance)
        {
            DataManager.Save(ZNet.m_world, Game.instance.GetPlayerProfile());
        }
    }
}