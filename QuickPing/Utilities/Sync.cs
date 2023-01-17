namespace QuickPing.Utilities
{
    internal static class Sync
    {

        #region ServerSide
        internal static void OnClientAddPinnedObject(ZRpc client, ZPackage pinData)
        {
            QuickPingPlugin.Log.LogInfo($"OnClientAddPinnedObject : {client.m_socket.GetHostName()}");

            if (Settings.ServerSync.Value)
            {
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                {
                    if (peer.m_rpc != client)
                    {
                        peer.m_rpc.Invoke("OnServerAddPinnedObject", pinData);
                    }
                }
            }
        }


        internal static void OnClientHandshake(ZRpc client, ZPackage _)
        {
            QuickPingPlugin.Log.LogInfo($"OnClientHandshake : {client.m_socket.GetHostName()}");

            if (Settings.ServerSync.Value)
            {
                foreach (var pinnedObject in DataManager.PinnedObjects)
                {
                    ZPackage pinData = DataManager.PackPinnedObject(pinnedObject);
                    client.Invoke("OnServerAddPinnedObject", pinData);
                }

            }
            client.Invoke("OnServerHandshake", null);
        }



        internal static void OnServerHandshake(ZRpc client, ZPackage _)
        {
            QuickPingPlugin.Log.LogInfo($"OnServerHandshake : {client.m_socket.GetHostName()}");


        }

        internal static void OnClientRemovePinnedObject(ZRpc client, ZPackage pinData)
        {
            QuickPingPlugin.Log.LogInfo($"OnClientRemovePinnedObject : {client.m_socket.GetHostName()}");

            if (Settings.ServerSync.Value)
            {
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                {
                    if (peer.m_rpc != client)
                    {
                        peer.m_rpc.Invoke("OnServerRemovePinnedObject", pinData);
                    }
                }
            }
        }
        #endregion


        #region ClientSide

        public static void OnServerAddPinnedObject(ZRpc client, ZPackage pinPackage)
        {
            QuickPingPlugin.Log.LogInfo($"OnServerAddPinnedObject : {client.m_socket.GetHostName()}");
            var pinnedObject = DataManager.UnpackPinnedObject(pinPackage);
            ZDOID zDOID = pinnedObject.ZDOID;
            Minimap.PinData pinData = pinnedObject.PinData;
            pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false /**TODO sync checked**/);
            DataManager.PinnedObjects.Add(zDOID, pinData);
        }

        internal static void OnServerRemovePinnedObject(ZRpc client, ZPackage pinPackage)
        {
            QuickPingPlugin.Log.LogInfo($"OnServerRemovePinnedObject : {client.m_socket.GetHostName()}");
            var pinnedObject = DataManager.UnpackPinnedObject(pinPackage);
            Minimap.instance.RemovePin(pinnedObject.PinData);
        }
        #endregion







    }
}
