using System.Collections.Generic;
using UnityEngine;
using static Minimap;

namespace QuickPing.Patches
{
    /// <summary>
    /// Not really a patch, but a modified game method used in a patch.
    /// </summary>
    internal static class Minimap_Patch
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strID"></param>
        /// <returns></returns>
        public static PinType IsPinable(string strID)
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
            return PinType.None;
        }

        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="obj"></param>
        public static void AddPin(HoverObject obj) => AddPin(obj.hover, obj.destructible, obj.name, obj.center);
        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="strID"></param>
        /// <param name="pos"></param>
        public static void AddPin(GameObject hover, IDestructible idestructible, string strID, Vector3 pos)
        {

            if (string.IsNullOrEmpty(strID) || !Settings.AddPin.Value || hover == null) { return; }

            Minimap.PinData pinData = new Minimap.PinData
            {
                m_type = IsPinable(strID)
            };


            if (pinData.m_type != PinType.None)
            {

                pinData.m_name ??= Localization.instance.Localize(strID);

                //Check if there is another pin in range
                Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);

                //PORTAL :Check if an already existing pin is at pos
                if (strID == "$piece_portal")
                {
                    if (hover.TryGetComponent(out Hoverable hoverable))
                    {
                        string tag = GetPortalTag(hoverable);
                        Jotunn.Logger.LogInfo(tag);
                        pinData.m_name = tag;
                        //TODO CHECK BEFORE REMOVE NON MAIS OH
                        if (closestPin != null)
                        {
                            Minimap.instance.RemovePin(closestPin);
                        }

                        Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);

                        //INFO
                        QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                    }
                }
                //OTHERS
                else if (closestPin == null)
                {
                    Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);

                    //INFO
                    QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                }
                if(idestructible != null)
                {
                    switch (idestructible)
                    {
                        case Destructible d_:
                            //d_.m_nview.SetPersistent(true);
                            if (!d_.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                                d_.m_nview.Register<Vector3>("RemovePin", RPC_RemovePin);
                            break;

                        case WearNTear d_:
                            //d_.m_nview.SetPersistent(true);
                            if (!d_.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                                d_.m_nview.Register<Vector3>("RemovePin", RPC_RemovePin);
                            break;

                        case MineRock5 d_:
                            //d_.m_nview.SetPersistent(true);
                            if (!d_.m_nview.m_functions.ContainsKey("RemovePin".GetStableHashCode()))
                                d_.m_nview.Register<Vector3>("RemovePin", RPC_RemovePin);
                            break;
                    }
                }
            }
        }

        private static string GetPortalTag(Hoverable hoverable)
        {
            string portalText = hoverable.GetHoverText();
            string tag = portalText.Split('"')[1];
            return tag;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PinData FindPin(Vector3 pos, PinType type = PinType.None, string name = null)
        {
            var pins = new List<(PinData pin, float dis)>();

            foreach (var pin in Minimap.instance.m_pins)
                if (type == PinType.None || pin.m_type == type)
                    pins.Add((pin, Utils.DistanceXZ(pos, pin.m_pos)));

            PinData closest = null;
            float closestDis = float.MaxValue;

            foreach (var (pin, dis) in pins)
                if (closest == null || dis < closestDis)
                {
                    closest = pin;
                    closestDis = dis;
                }

            if (closestDis > 1f)
                return null;

            if (!string.IsNullOrEmpty(name) && closest.m_name != name)
                return null;

            return closest;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void RPC_RemovePin(long sender, Vector3 pos) => RemovePin(pos, PinType.None, null);
        public static void RemovePin(Vector3 pos, PinType type = PinType.None, string name = null)
        {
            if (FindPin(pos, type, name) is not PinData pin)
                return;

            Minimap.instance.RemovePin(pin);
        }
    }
}
