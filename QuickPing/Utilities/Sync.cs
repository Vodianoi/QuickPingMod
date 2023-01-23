using System.Collections;

namespace QuickPing.Utilities
{
    internal static class Sync
    {

        #region ServerSide
        internal static IEnumerator OnClientAddPinnedObject(long id, ZPackage package)
        {
            LogManager.Log($"OnClientAddPinnedObject : {id}");

            if (Settings.ServerSync.Value)
            {
                QuickPingPlugin.Instance.RPC_AddPinnedObject.SendPackage(ZNet.instance.m_peers, package);
            }
            yield return package;
        }


        internal static IEnumerator OnClientHandshake(long id, ZPackage pinnedObjects)
        {
            LogManager.Log($"OnClientHandshake : {id}");

            if (Settings.ServerSync.Value)
            {
                QuickPingPlugin.Instance.RPC_Handshake.SendPackage(id, pinnedObjects);

            }
            yield return pinnedObjects;
        }


        internal static IEnumerator OnClientRemovePinnedObject(long id, ZPackage package)
        {
            LogManager.Log($"OnClientRemovePinnedObject : {id}");

            if (Settings.ServerSync.Value)
            {
                QuickPingPlugin.Instance.RPC_RemovePinnedObject.SendPackage(ZNet.instance.m_peers, package);
            }
            yield return package;
        }
        #endregion


        #region ClientSide

        public static IEnumerator OnServerAddPinnedObject(long id, ZPackage package)
        {
            // check if this is the host
            if (ZRoutedRpc.instance.GetServerPeerID() == id)
                yield break;
            var pinnedObject = DataManager.UnpackPinnedObject(package);
            ZDOID zDOID = pinnedObject.ZDOID;
            Minimap.PinData pinData = pinnedObject.PinData;
            if (DataManager.PinnedObjects.ContainsKey(zDOID))
            {
                LogManager.Log($"OnServerAddPinnedObject : {id} already exists");
                yield break;
            }
            pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false /**TODO sync checked**/);
            DataManager.PinnedObjects.Add(zDOID, pinData);
            LogManager.Log($"OnServerAddPinnedObject : {id}");
            yield return package;
        }

        internal static IEnumerator OnServerRemovePinnedObject(long id, ZPackage package)
        {
            // check if this is the host
            if (ZRoutedRpc.instance.GetServerPeerID() == id)
                yield break;
            var pinnedObject = DataManager.UnpackPinnedObject(package);
            if (!DataManager.PinnedObjects.ContainsKey(pinnedObject.ZDOID))
            {
                LogManager.Log($"OnServerRemovePinnedObject : {id} does not exist");
                yield break;
            }
            Minimap.instance.RemovePin(pinnedObject.PinData);
            DataManager.PinnedObjects.Remove(pinnedObject.ZDOID);
            LogManager.Log($"OnServerRemovePinnedObject : {id}");
            yield return package;
        }

        internal static IEnumerator OnServerHandshake(long id, ZPackage pinnedObjects)
        {
            LogManager.Log($"OnServerHandshake : {id}");
            DataManager.PinnedObjects = DataManager.UnpackPinnedObjects(pinnedObjects);

            yield return pinnedObjects;
        }
        #endregion
    }
}
