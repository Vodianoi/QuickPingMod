using HarmonyLib;
using QuickPing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QuickPing.Patches
{
    /// <summary>
    /// Not really a patch, but a modified game method used in a patch.
    /// </summary>
    internal static class Minimap_Patch
    {
        public static Dictionary<ZDOID, Minimap.PinData> PinnedObjects = new();


        /// <summary>
        /// Check if an object can be pinned
        /// </summary>
        /// <param name="strID"></param>
        /// <returns></returns>
        public static Minimap.PinType IsPinable(string strID)
        {

            string baseLocalization = Localization_Patch.GetBaseTranslation(strID);
            Dictionary<Minimap.PinType, List<string>> pinables = new()
            {
                //Fire pin
                {
                    Minimap.PinType.Icon0,
                    new List<string>
                    {
                        "$piece_firepit",
                        "$piece_bonfire",
                        "$piece_fire",
                        "GoblinCamp"
                    }
                },

                //Home pin
                {
                    Minimap.PinType.Icon1,
                    new List<string>
                    {
                        //"$",
                        //"$",
                        //"$",
                        //"$",
                    }
                },

                //Hammer pin
                {
                    Minimap.PinType.Icon2,
                    new List<string>
                    {
                        "$piece_deposit_copper",
                        "$piece_deposit_silver",
                        "$piece_deposit_silvervein",
                        "$piece_deposit_tin",
                        "$piece_mudpile"
                    }
                },

                //Point pin
                {
                    Minimap.PinType.Icon3,
                    new List<string>
                    {
                        "$item_raspberries",
                        "$item_blueberries",
                        "$item_cloudberries",
                        "$item_dragonegg"
                    }
                },

                //Rune pin
                {
                    Minimap.PinType.Icon4,
                    new List<string>
                    {
                        "$location_forestcave",
                        "$location_forestcrypt",
                        "Stone Henge",
                        "$location_sunkencrypt",
                        "$location_mountaincave",
                        "$piece_portal"
                    }
                }
            };

            foreach (var pinType in pinables.Keys)
            {
                if (pinables[pinType].Contains(strID) || pinables[pinType].Contains(baseLocalization))
                {
                    return pinType;
                }
            }
            return Minimap.PinType.None;
        }

        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="obj"></param>
        public static void AddPin(HoverObject obj) => AddPin(obj.Hover, obj.Destructible, obj.Name, obj.center);
        /// <summary>
        /// Add correct pin to map if Settings and bypass filters.
        /// </summary>
        /// <param name="obj"></param>
        public static void ForceAddPin(HoverObject obj) => AddPin(obj.Hover, obj.Destructible, obj.Name, obj.center, true);

        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="hover">Pinged object</param>
        /// <param name="idestructible">IDestructible interface found in "hover"</param>
        /// <param name="strID">Display Name of hover object</param>
        /// <param name="pos">Pinged position (last raycast point or hover center)</param>
        /// <param name="force">Bypass filters</param>
        public static void AddPin(GameObject hover, IDestructible idestructible, string strID, Vector3 pos, bool force = false)
        {

            if ((string.IsNullOrEmpty(strID) || !Settings.AddPin.Value || hover == null) && !force) { return; }

            Minimap.PinData pinData = new Minimap.PinData
            {
                m_type = IsPinable(strID)
            };


            Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);
            if (pinData.m_type != Minimap.PinType.None)
            {

                pinData.m_name ??= Localization.instance.Localize(strID);
                pinData.m_pos = pos;
                //Check if there is another pin in range

                //PORTAL :Check if an already existing pin is at pos


                if (strID == "$piece_portal")
                {
                    if (hover.TryGetComponent(out Hoverable hoverable))
                    {
                        pinData.m_name = GetPortalTag(hoverable);

                        //TODO CHECK BEFORE REMOVE
                        if (closestPin != null)
                        {
                            Minimap.instance.RemovePin(closestPin);
                        }

                        pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                        //INFO
                        QuickPingPlugin.Log.LogInfo($"Add Portal Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");
                    }
                }
                //OTHERS
                else if (closestPin == null)
                {
                    pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                    //INFO
                    QuickPingPlugin.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");
                }

            }
            else if (force && closestPin == null)
            {
                pinData.m_type = Settings.DefaultPinType.Value;
                pinData.m_name ??= Localization.instance.Localize(strID);
                pinData.m_pos = pos;
                switch (pinData.m_name)
                {
                    case null:
                    case "":
                        pinData.m_name = Settings.DefaultPingText;
                        break;
                }
                pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                //INFO
                QuickPingPlugin.Log.LogInfo($"Force Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
            }

            if (idestructible != null)
            {
                FieldInfo fieldInfo = idestructible.GetType().GetField("m_nview", BindingFlags.Instance | BindingFlags.NonPublic);
                ZNetView netView = fieldInfo.GetValue(idestructible) as ZNetView;
                ZDOID uid = netView.GetZDO().m_uid;
                if (uid == null)
                {
                    QuickPingPlugin.Log.LogError($"Try adding {idestructible} but {netView} uid is null");
                }
                if (!PinnedObjects.ContainsKey(uid))
                    PinnedObjects.Add(uid, pinData);
            }
        }

        private static string GetPortalTag(Hoverable hoverable)
        {
            string portalText = hoverable.GetHoverText();
            string tag = portalText.Split('"')[1];
            return tag;
        }



        /// <summary>
        /// Pack ZDOID, name, type and position in ZPackage and returns it.
        /// </summary>
        /// <returns>ZPackage containing ZDOID, name, type and position</returns>
        public static ZPackage PackData()
        {
            ZPackage zPackage = new ZPackage();

            foreach (var x in PinnedObjects)
            {
                zPackage.Write(x.Key);
                zPackage.Write(x.Value);
            }
            return zPackage;
        }

        /// <summary>
        /// Save all pinned into file (support SteamStorage cloud)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cloudSaveFailed"></param>
        public static void SavePinnedDataToWorld(World world, out bool cloudSaveFailed)
        {
            ZPackage zPackage = PackData();

            cloudSaveFailed = Utilities.DataUtils.SaveData(world, zPackage);

        }

        [HarmonyPatch(typeof(Minimap))]
        [HarmonyPatch(nameof(Minimap.RemovePin), new Type[] { typeof(Minimap.PinData) })]
        [HarmonyPrefix]
        public static bool RemovePin(Minimap __instance, Minimap.PinData pin)
        {
            foreach (var p in PinnedObjects)
            {
                if (p.Value.Compare(pin))
                {
                    pin = __instance.GetClosestPin(p.Value.m_pos, Settings.ClosestPinRange.Value);
                    PinnedObjects.Remove(PinnedObjects.FirstOrDefault((x) => x.Value.Compare(p.Value)).Key);
                    break;
                }
            }
            if ((bool)pin.m_uiElement)
            {
                UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
            }
            __instance.m_pins.Remove(pin);
            return false;
        }

    }


}
