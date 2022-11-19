using HarmonyLib;
using Jotunn.Configs;
using System;
using UnityEngine;
using UnityEngine.Events;
using static Chat;
using UnityEngine.UI;

namespace QuickPing.Patches
{
    class Player_Patch
    {
        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {


            if (Player.m_localPlayer == __instance)
            {
                if (!Settings.PingWhereLooking.Value)
                {
                    return;
                }
                if (Player.m_localPlayer && ZInput.instance != null)
                    if (Settings.PingKey.Value != KeyCode.None)
                        if (ZInput.GetButtonDown(Settings.pingBtn.Name))
                        {
                            // TODO: improve object detection range

                            var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.TransformDirection(Vector3.forward));

                            var mask = Pathfinding.instance.m_layers | Pathfinding.instance.m_waterLayers;

                            Physics.Raycast(ray, out var hit, 500f, mask);


                            Vector3 pos = hit.point;
                            QuickPing.Instance.PingText = $"Ping : x:{(int)pos.x}  y:{(int)pos.y}";
                            bool pinClose = false;

                            if (__instance.GetHoverCreature() != null)

                            {
                                Character creature = __instance.GetHoverCreature();
                                pos = creature.GetCenterPoint();
                                QuickPing.Instance.PingText = creature.m_name;
                            }
                            else if (__instance.GetHoverObject() != null)
                            {
                                Hoverable hoverable = __instance.GetHoverObject().GetComponentInParent<Hoverable>();
                                if (hoverable != null && Settings.AddPin.Value)
                                {
                                    AddPin(__instance, pos, out pinClose);
                                    QuickPing.Instance.PingText = hoverable.GetHoverName();
                                    Jotunn.Logger.LogInfo("Pinged : " + QuickPing.Instance.PingText);
                                }

                                pos = __instance.GetHoverObject().transform.position;

                                //pinText += hoverable.GetHoverName();
                            }
                            if (!pinClose)
                                SendPing(pos, QuickPing.Instance.PingText);

                        }
            }
        }

        private static void AddPin(Player instance, Vector3 pos, out bool pinable)
        {
            pinable = false;
            var obj = instance.GetHoverObject().GetComponentInParent<Hoverable>();
            var objectName = obj.ToString();
            if (string.IsNullOrEmpty(objectName) || !Settings.AddPin.Value) { return; }

            global::Minimap.PinData pinData = new global::Minimap.PinData();


            if (pinable = objectName.Contains("rock4_copper"))
            {
                pinData.m_type = global::Minimap.PinType.Icon2;
                pinData.m_name = "Copper";
            }
            else if (pinable = objectName.Contains("RaspberryBush"))
            {
                pinData.m_type = global::Minimap.PinType.Icon3;
                pinData.m_name = "Raspberry";
            }
            else if (pinable = objectName.Contains("BlueberryBush"))
            {
                pinData.m_type = global::Minimap.PinType.Icon3;
                pinData.m_name = "Blueberry";
            }
            else if (pinable = objectName.Contains("MineRock_Tin"))
            {
                pinData.m_type = global::Minimap.PinType.Icon2;
                pinData.m_name = "Tin";
            }
            else if (pinable = objectName.Contains("FirePlace"))
            {
                pinData.m_type = global::Minimap.PinType.Icon0;
                pinData.m_name = "FirePlace";
            }
            else if (pinable = objectName.Contains("portal_wood"))
            {
                
                pinData.m_type = global::Minimap.PinType.Icon4;
                string portalText = obj.GetHoverText();
                string tag = portalText.Split('"')[1];
                Jotunn.Logger.LogInfo(tag);
                pinData.m_name = tag;
            }
            else
            {
                return;
            }
            global::Minimap.PinData closestPin = Minimap_Patch.GetClosestPin(global::Minimap.instance, pos, 1f);
            if (closestPin == null)
                global::Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, instance.GetPlayerID());
            else
            {
                if (pinData.m_type == global::Minimap.PinType.Icon4 && closestPin.m_type == global::Minimap.PinType.Icon4)
                {
                    global::Minimap.instance.RemovePin(closestPin);
                    global::Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, instance.GetPlayerID());

                }
            }

        }





        private static void SendPing(Vector3 position, string text)
        {

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer)
            {
                Vector3 vector = position;
                vector.y = localPlayer.transform.position.y;
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", vector, 3, localPlayer.GetPlayerName(), text, PrivilegeManager.GetNetworkUserId());
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", vector.x, vector.z));
                }
            }
        }

    }
}
