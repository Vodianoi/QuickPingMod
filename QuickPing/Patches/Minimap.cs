using System.Collections.Generic;
using UnityEngine;

namespace QuickPing.Patches
{
    /// <summary>
    /// Not really a patch, but a modified game method used in a patch.
    /// </summary>
    internal static class Minimap_Patch
    {
        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="strID"></param>
        /// <param name="pos"></param>
        /// <param name="pinable"></param>
        public static void AddPin(Player instance, GameObject hover, string strID, Vector3 pos, out bool pinable)
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
                    Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);

                    //Check if an already existing pin is at pos
                    if (strID == "$piece_portal")
                    {
                        pinData.m_type = Minimap.PinType.Icon4;
                        if (hover.TryGetComponent(out Hoverable hoverable))
                        {

                            string portalText = hoverable.GetHoverText();
                            string tag = portalText.Split('"')[1];
                            Jotunn.Logger.LogInfo(tag);
                            pinData.m_name = tag;
                            if (closestPin != null)
                                Minimap.instance.RemovePin(closestPin);
                            Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);

                            //INFO
                            QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                        }
                    }
                    else if (closestPin == null)
                    {
                        Minimap.instance.AddPin(pos, pinData.m_type, pinData.m_name, true, false, 0L);

                        //INFO
                        QuickPing.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
                    }
                    else
                        break;
                }
            }



        }
    }
}
