using HarmonyLib;
using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;

namespace QuickPing.Patches
{
    class Player_Patch
    {
        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {
            string pingText = "Ping!";
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
                            // TODO: improve object detection range -> Done ?
                            FindHoverObject(
                                out GameObject hover,
                                out Character hoverCreature,
                                out Vector3 pos,
                                500f);

                            //var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.TransformDirection(Vector3.forward));

                            //var mask = Pathfinding.instance.m_layers | Pathfinding.instance.m_waterLayers;
                            //Physics.Raycast(ray, out var hit, 500f, mask);

                            //pingText = $"Ping : x:{(int)pos.x}  y:{(int)pos.y}";
                            if (hover == null && hoverCreature == null) return;


                            bool pinClose = false;
                            if (hoverCreature != null)
                            {
                                QuickPing.Log.LogError($"Ping : {hoverCreature}");
                                if (Settings.AddPin.Value)
                                {

                                    AddPin(__instance, hoverCreature, pos, out pinClose);
                                }
                                pingText = Localization.instance.Localize(hoverCreature.GetHoverName());
                            }
                            else if (hover != null && hover.TryGetComponent(out Hoverable hoverable))
                            {

                                QuickPing.Log.LogDebug($"Ping : {hoverable}");
                                if (Settings.AddPin.Value)
                                {

                                    AddPin(__instance, hoverable, pos, out pinClose);
                                }
                                pingText = Localization.instance.Localize(hoverable.GetHoverName());
                            }
                            else if (hover.transform.parent && hover.transform.parent.TryGetComponent(out hoverable))
                            {
                                QuickPing.Log.LogDebug($"Ping : {hoverable} in parent");
                                if (Settings.AddPin.Value)
                                {

                                    AddPin(__instance, hoverable, pos, out pinClose);
                                }
                                pingText = Localization.instance.Localize(hoverable.GetHoverName());
                            }
                            else if (hover.transform.parent && hover.transform.parent.parent && hover.transform.parent.parent.TryGetComponent(out hoverable))
                            {
                                QuickPing.Log.LogDebug($"Ping : {hoverable} in parent");
                                if (Settings.AddPin.Value)
                                {

                                    AddPin(__instance, hoverable, pos, out pinClose);
                                }
                                pingText = Localization.instance.Localize(hoverable.GetHoverName());
                            }

                            if (!pinClose)
                                SendPing(pos, pingText);
                            else
                                SendPing(pos, pingText, local: true);
                        }
            }
        }


        public static void FindHoverObject(out GameObject hover, out Character hoverCreature, out Vector3 pos, float range)
        {
            hover = null;
            hoverCreature = null;
            pos = Player.m_localPlayer.GetHeadPoint();
            LayerMask m_interactMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "character_noenv", "vehicle");
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, range + 10, layerMask: m_interactMask);
            Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            RaycastHit[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                pos = array2[i].point;
                RaycastHit raycastHit = array2[i];
                QuickPing.Log.LogWarning(raycastHit.collider.transform.name);
                if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == Player.m_localPlayer.gameObject)
                {
                    continue;
                }

                if (hoverCreature == null)
                {
                    Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
                    QuickPing.Log.LogWarning($"Check Character -> {character}");
                    if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                    {
                        hoverCreature = character;
                    }
                }

                if (Vector3.Distance(Player.m_localPlayer.GetEyePoint(), raycastHit.point) < range)
                {
                    if (raycastHit.collider.GetComponent<Hoverable>() != null)
                    {
                        hover = raycastHit.collider.gameObject;
                    }
                    else if ((bool)raycastHit.collider.attachedRigidbody)
                    {
                        hover = raycastHit.collider.attachedRigidbody.gameObject;
                    }
                    else
                    {
                        hover = raycastHit.collider.gameObject;
                    }
                }

                break;
            }
        }

        private static bool SendPing(Vector3 position, string text, bool local = false)
        {

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer)
            {
                Vector3 vector = position;
                //vector.y = localPlayer.transform.position.y;
                ZRoutedRpc.instance.InvokeRoutedRPC(local ? Player.m_localPlayer.GetZDOID().userID : ZRoutedRpc.Everybody, "ChatMessage", vector, 3, localPlayer.GetPlayerName(), text, PrivilegeManager.GetNetworkUserId());
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", vector.x, vector.z));
                }
            }
            return false;
        }

        /// <summary>
        /// Add correct pin to map if Settings.
        /// TODO : Change "String::Contains" to exact value $ ??
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="hoverable"></param>
        /// <param name="pos"></param>
        /// <param name="pinable"></param>
        private static void AddPin(Player instance, Hoverable hoverable, Vector3 pos, out bool pinable)
        {
            pinable = false;
            var obj = hoverable;
            var objectName = obj.ToString();
            if (string.IsNullOrEmpty(objectName) || !Settings.AddPin.Value) { return; }

            Minimap.PinData pinData = new Minimap.PinData();


            if (pinable = objectName.Contains("rock4_copper"))
            {
                pinData.m_type = Minimap.PinType.Icon2;
                pinData.m_name = "Copper";
            }
            else if (pinable = objectName.Contains("RaspberryBush"))
            {
                pinData.m_type = Minimap.PinType.Icon3;
                pinData.m_name = "Raspberry";
            }
            else if (pinable = objectName.Contains("BlueberryBush"))
            {
                pinData.m_type = Minimap.PinType.Icon3;
                pinData.m_name = "Blueberry";
            }
            else if (pinable = objectName.Contains("MineRock_Tin"))
            {
                pinData.m_type = Minimap.PinType.Icon2;
                pinData.m_name = "Tin";
            }
            else if (pinable = objectName.Contains("FirePlace"))
            {
                pinData.m_type = Minimap.PinType.Icon0;
                pinData.m_name = "FirePlace";
            }
            else if (pinable = objectName.Contains("portal_wood"))
            {

                pinData.m_type = Minimap.PinType.Icon4;
                string portalText = obj.GetHoverText();
                string tag = portalText.Split('"')[1];
                Jotunn.Logger.LogInfo(tag);
                pinData.m_name = tag;
            }
            else
            {
                return;
            }
            Minimap.PinData closestPin = Minimap_Patch.GetClosestPin(Minimap.instance, pos, 1f);
            if (closestPin == null)
                Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, instance.GetPlayerID());
            else
            {
                if (pinData.m_type == Minimap.PinType.Icon4 && closestPin.m_type == Minimap.PinType.Icon4)
                {
                    Minimap.instance.RemovePin(closestPin);
                    Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, instance.GetPlayerID());

                }
            }

        }







    }
}
