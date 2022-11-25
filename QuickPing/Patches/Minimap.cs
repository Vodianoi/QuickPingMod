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
        public static Dictionary<Vector3, GameObject> Pins { get; private set; }
        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="strID"></param>
        /// <param name="pos"></param>
        /// <param name="pinable"></param>
        public static void AddPin(GameObject hover, string strID, Vector3 pos, out bool pinable)
        {
            pinable = false;

            if (string.IsNullOrEmpty(strID) || !Settings.AddPin.Value) { return; }

            Minimap.PinData pinData = new Minimap.PinData();

            string baseLocalization = Localization_Patch.GetBaseTranslation(strID);

            Dictionary<Minimap.PinType, List<string>> pinables = new Dictionary<Minimap.PinType, List<string>>
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
                    pinData.m_type = pinType;

                    if (pinData.m_name == null)
                        pinData.m_name = Localization.instance.Localize(strID);

                    //Check if there is another pin in range
                    Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);

                    //PORTAL :Check if an already existing pin is at pos
                    if (strID == "$piece_portal")
                    {
                        pinData.m_type = Minimap.PinType.Icon4;
                        if (hover.TryGetComponent(out Hoverable hoverable))
                        {

                            string portalText = hoverable.GetHoverText();
                            string tag = portalText.Split('"')[1];
                            Jotunn.Logger.LogInfo(tag);
                            pinData.m_name = tag;
                            //TODO CHECK BEFORE REMOVE NON MAIS OH
                            if (closestPin != null)
                                Minimap.instance.RemovePin(closestPin);
                            var pin = Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);
                            pinable = true;
                            //if (idestructible != null)
                            //{
                            //    switch (idestructible)
                            //    {
                            //        case WearNTear wearNTear:
                            //            QuickPing.Log.LogWarning($"Add on destroyed to -> {wearNTear.gameObject.name}");
                            //            wearNTear.m_onDestroyed += () => Minimap.instance.RemovePin(pin);
                            //            break;
                            //    }
                            //}
                            //INFO
                            QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                        }
                    }
                    //OTHERS
                    else if (closestPin == null)
                    {
                        var pin = Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);
                        pinable = true;
                        //if (idestructible != null)
                        //{
                        //    switch (idestructible)
                        //    {
                        //        case Destructible destructible:
                        //            QuickPing.Log.LogWarning($"Add on destroyed to -> {destructible.gameObject.name}");
                        //            destructible.m_onDestroyed += () => Minimap.instance.RemovePin(pin);
                        //            break;
                        //        case MineRock5 mineRock5:
                        //            QuickPing.Log.LogWarning($"Add on destroyed to -> {mineRock5.gameObject.name}");
                        //            break;
                        //    }
                        //}

                        //INFO
                        QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                    }
                    else
                        break;
                }
            }



        }
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

        public static bool RemovePin(Vector3 pos, PinType type = PinType.None, string name = null)
        {
            if (FindPin(pos, type, name) is not PinData pin)
                return false;

            Minimap.instance.RemovePin(pin);

            return true;
        }
    }
}
