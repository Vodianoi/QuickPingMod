using HarmonyLib;
using QuickPing.Utilities;
using UnityEngine;

namespace QuickPing.Patches
{

    #region Patches
    /// <summary>
    /// Patch Player Class to add Ping Key and automatic pins
    /// </summary>
    static class Player_Patch
    {
        /// <summary>
        /// Check for Key Input
        /// </summary>
        /// <param name="__instance">Local Player</param>
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {
            //Check ZInput instance 
            if (ZInput.instance == null) return;
            //Check instance
            if (!Player.m_localPlayer || Player.m_localPlayer != __instance) return;
            //Check mod settings
            if (!Settings.PingWhereLooking.Value && !Settings.AddPin.Value) return;

            if (Minimap_Patch.IsNaming)
            {
                Minimap_Patch.UpdateNameInput();
            }

            //Check if player can use input (fix #34)
            if (!Player.m_localPlayer.TakeInput()) return;

            //Check Keys
            if (Settings.PingKey.Value != KeyCode.None)
            {
                if (ZInput.GetButtonDown(Settings.RenameBtn.Name))
                {
                    DataManager.PinnedObject ping = TypeManager.GetPinnedObject(500f);

                    QuickPingPlugin.OnRenameEvent.Invoke(ping);
                }
                else
                if (ZInput.GetButtonDown(Settings.PingBtn.Name))
                {
                    DataManager.PinnedObject ping = TypeManager.GetPinnedObject(500f);

                    QuickPingPlugin.OnPingEvent.Invoke(ping);
                }
                else
                if (ZInput.GetButtonDown(Settings.PingEverythingBtn.Name))
                {
                    DataManager.PinnedObject ping = TypeManager.GetPinnedObject(500f);

                    QuickPingPlugin.OnPingEverythingEvent.Invoke(ping);
                }

            }
        }

        #endregion


        public static void SendPing(DataManager.PinnedObject pinData)
        {
            var pin = pinData.PinData;
            SendPing(pin.m_pos, Localization.instance.Localize(pin.m_name));

        }
        internal static void SendPing(Vector3 position, string text, bool local = false)
        {

            //TODO : PORTAL check if necessary after your awesome refactoring

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer && Settings.PingWhereLooking.Value)
            {
                LogManager.Log("SendPing : " + text);
                ZRoutedRpc.instance.InvokeRoutedRPC(local ? Player.m_localPlayer.GetZDOID().userID : ZRoutedRpc.Everybody, "ChatMessage", position, 3, localPlayer.GetPlayerName(), text, 1);
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", position.x, position.z));
                }
            }
        }
    }
}
