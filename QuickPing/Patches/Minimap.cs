using System;
using System.Collections.Generic;
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
        /// 
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

                        Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                        //INFO
                        QuickPingPlugin.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");
                    }
                }
                //OTHERS
                else if (closestPin == null)
                {
                    Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

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
                Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                //INFO
                QuickPingPlugin.Log.LogInfo($"Force Add Pin : Name:{pinData.m_name} x:{pos.x}, y:{pos.y}, Type:{pinData.m_type}");
            }

            if (idestructible != null)
            {
                FieldInfo fieldInfo = idestructible.GetType().GetField("m_nview", BindingFlags.Instance | BindingFlags.NonPublic);
                ZNetView netView = fieldInfo.GetValue(idestructible) as ZNetView;
                ZDOID uid = netView.GetZDO().m_uid;
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
        /// Find pin at pos
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Minimap.PinData FindPin(Vector3 pos, Minimap.PinType type = Minimap.PinType.None, string name = null)
        {
            var pins = new List<(Minimap.PinData pin, float dis)>();

            foreach (var pin in Minimap.instance.m_pins)
                if (type == Minimap.PinType.None || pin.m_type == type)
                    pins.Add((pin, Utils.DistanceXZ(pos, pin.m_pos)));

            Minimap.PinData closest = null;
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
        /// Remove pin pinData (need to be exact value)
        /// </summary>
        /// <param name="pinData"></param>
        public static void RemovePin(Minimap.PinData pinData)
        {
            if (pinData == null) return;
            Minimap.instance.RemovePin(pinData);
        }

        /// <summary>
        /// Remove pin in pos, of type and name
        /// </summary>
        /// <param name="pos">Pin to remove position</param>
        /// <param name="type">Pin to remove type</param>
        /// <param name="name">Pin to remove name</param>
        public static void RemovePin(Vector3 pos, Minimap.PinType type = Minimap.PinType.None, string name = null)
        {
            if (FindPin(pos, type, name) is not Minimap.PinData pin)
                return;

            Minimap.instance.RemovePin(pin);
        }


        /// <summary>
        /// Read ZPackage into PinnedObjects list
        /// </summary>
        /// <param name="package">Loaded from file</param>
        public static void ReadPinData(ZPackage package)
        {
            if (package == null) return;

            while (package.GetPos() < package.Size())
            {
                ZDOID zdoid = package.ReadZDOID();
                Minimap.PinData pinData = new()
                {
                    m_name = package.ReadString(),
                    m_type = (Minimap.PinType)Enum.Parse(typeof(Minimap.PinType), package.ReadString()),
                    m_pos = package.ReadVector3()
                };
                if (!PinnedObjects.ContainsKey(zdoid))
                    PinnedObjects.Add(zdoid, pinData);

            }

        }

        /// <summary>
        /// Pack ZDOID, name, type and position in ZPackage and returns it.
        /// </summary>
        /// <returns>ZPackage containing ZDOID, name, type and position</returns>
        public static ZPackage PackData()
        {
            ZPackage zPackage = new ZPackage();

            foreach (var x in Minimap_Patch.PinnedObjects)
            {
                zPackage.Write(x.Key);
                zPackage.Write(x.Value.m_name);
                zPackage.Write(x.Value.m_type.ToString());
                zPackage.Write(x.Value.m_pos);
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

    }


}
