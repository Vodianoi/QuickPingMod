using HarmonyLib;
using Jotunn.Managers;
using QuickPing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuickPing.Patches
{
    /// <summary>
    /// Not really a patch, but a modified game method used in a patch.
    /// </summary>
    internal static class Minimap_Patch
    {
        public static Dictionary<ZDOID, Minimap.PinData> PinnedObjects = new();
        public static Dictionary<string, string> CustomNames = new();

        private static GameObject nameInput;
        private static GameObject toggleSaveName;

        public static bool IsNaming = false;

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
        /// Force rename pin
        /// </summary>
        /// <param name="obj"></param>
        internal static void RenamePin(HoverObject obj) => AddPin(obj.Hover, obj.Destructible, obj.Name, obj.center, rename: true);

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
        public static void AddPin(GameObject hover, IDestructible idestructible, string strID, Vector3 pos, bool force = false, bool rename = false)
        {
            if (!Settings.AddPin.Value && !force) { return; }

            Minimap.PinData pinData = new Minimap.PinData
            {
                m_type = IsPinable(strID)
            };

            Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);

            switch (strID)
            {
                case "$piece_portal":
                    if (hover.TryGetComponent(out Hoverable hoverable))
                    {
                        pinData.m_name = GetPortalTag(hoverable);

                        if (closestPin != null)
                        {
                            Minimap.instance.RemovePin(closestPin);
                        }

                        pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);

                        QuickPingPlugin.Log.LogInfo($"Add Portal Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");
                    }
                    break;
                default:
                    if (closestPin == null || rename)
                    {

                        pinData.m_name ??= Localization.instance.Localize(strID);
                        if (CustomNames.ContainsKey(pinData.m_name))
                            pinData.m_name = CustomNames[pinData.m_name];

                        pinData.m_pos = pos;
                        if (pinData.m_name == null || pinData.m_name == "")
                        {
                            pinData.m_name = Settings.DefaultPingText;
                        }
                        if (pinData.m_type == Minimap.PinType.None && force)
                        {
                            pinData.m_type = Settings.DefaultPinType.Value;
                            pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);
                            QuickPingPlugin.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");
                            break;
                        }

                        if (pinData.m_type != Minimap.PinType.None)
                        {
                            if (closestPin == null)
                                pinData = Minimap.instance.AddPin(pinData.m_pos, pinData.m_type, pinData.m_name, true, false, 0L);
                            else if (rename)
                                pinData = closestPin;
                            QuickPingPlugin.Log.LogInfo($"Add Pin : Name:{pinData.m_name} x:{pinData.m_pos.x}, y:{pinData.m_pos.y}, Type:{pinData.m_type}");

                            //Check if Settings.AskForName.Value is true, and if CustomNames contains its name.
                            //if true ask for user input before adding pin
                            if (rename)
                            {
                                GUIManager.BlockInput(true);
                                InitNameInput();
                                Minimap.instance.ShowPinNameInput(pinData);
                            }
                        }
                    }
                    break;
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
                {
                    PinnedObjects[uid] = pinData;
                }
            }
        }

        #region NameInput

        public static void UpdateNameInput()
        {
            if (Minimap.instance.m_namePin == null)
            {
                Minimap.instance.m_wasFocused = false;
            }
            if (Minimap.instance.m_namePin != null)
            {

                nameInput.SetActive(true);
                toggleSaveName.SetActive(true);
                var inputField = nameInput.GetComponent<InputField>();
                var toggle = toggleSaveName.GetComponent<Toggle>();
                if (!inputField.isFocused)
                {
                    EventSystem.current.SetSelectedGameObject(nameInput);
                }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ValidateNameInput(inputField, toggle.isOn);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelNameInput();
                }
                Minimap.instance.m_wasFocused = true;
            }
            else //reset
            {
                nameInput.gameObject.SetActive(value: false);
                IsNaming = false;
                GUIManager.BlockInput(false);
            }
        }

        private static void CancelNameInput()
        {
            Minimap.instance.m_namePin = null;
            Minimap.instance.m_wasFocused = false;
            nameInput.gameObject.SetActive(value: false);
            IsNaming = false;
            GUIManager.BlockInput(false);
        }

        private static void ValidateNameInput(InputField inputField, bool on)
        {
            string text = inputField.text;
            text = text.Replace('$', ' ');
            text = text.Replace('<', ' ');
            text = text.Replace('>', ' ');
            string originalText = Minimap.instance.m_namePin.m_name;
            if (!CustomNames.ContainsKey(originalText))
            {
                foreach (var pair in CustomNames)
                {
                    if (Minimap.instance.m_namePin.m_name == pair.Value)
                    {
                        originalText = pair.Key;
                    }
                }
            }

            Minimap.instance.m_namePin.m_name = text;

            // Persistent save of text value for this pinned object
            if (on)
            {
                QuickPingPlugin.Log.LogInfo($"Save name {Minimap.instance.m_namePin.m_name} for {originalText}");
                SaveName(Minimap.instance.m_namePin.m_name, originalText);
            }
            Minimap.instance.m_namePin = null;


        }

        /// <summary>
        /// Persistent save original name for this pinned object
        /// </summary>
        /// <param name="m_name"></param>
        /// <param name="originalName"></param>
        private static void SaveName(string m_name, string originalName)
        {
            if (CustomNames.ContainsKey(originalName))
            {
                CustomNames[originalName] = m_name;
            }
            else
            {
                CustomNames.Add(originalName, m_name);
            }
        }

        private static void InitNameInput()
        {
            if (GUIManager.Instance == null)
            {
                QuickPingPlugin.Log.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                QuickPingPlugin.Log.LogError("GUIManager CustomGUI is null");
                return;
            }

            IsNaming = true;

            nameInput = GUIManager.Instance.CreateInputField(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                contentType: InputField.ContentType.Standard,
                placeholderText: "Pin Name",
                fontSize: 16,
                width: 160f,
                height: 30f
            );

            toggleSaveName = GUIManager.Instance.CreateToggle(
                parent: nameInput.transform,
                width: 10f,
                height: 10f
                );



        }
        #endregion

        private static string GetPortalTag(Hoverable hoverable)
        {
            string portalText = hoverable.GetHoverText();
            string tag = portalText.Split('"')[1];
            return tag;
        }




        #region Data Management

        /// <summary>
        /// Pack ZDOID, name, type and position in ZPackage and returns it.
        /// </summary>
        /// <returns>ZPackage containing ZDOID, name, type and position</returns>
        public static ZPackage PackPinnedObjects()
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
        /// Pack originalName=name in ZPackage and returns it.
        /// </summary>
        /// <returns></returns>
        public static ZPackage PackCustomNames()
        {
            ZPackage zPackage = new ZPackage();

            foreach (var x in CustomNames)
            {
                if (x.Key is null or "") continue;
                zPackage.Write(x.Key);
                zPackage.Write(x.Value);
            }

            return zPackage;
        }

        internal static void UnpackCustomNames(ZPackage zPackage)
        {
            CustomNames.Clear();
            while (zPackage.GetPos() < zPackage.Size())
            {
                string originalName = zPackage.ReadString();
                string name = zPackage.ReadString();
                if (originalName is not "" or null)
                    CustomNames.Add(originalName, name);
            }
        }

        /// <summary>
        /// Save all pinned into file (support SteamStorage cloud)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cloudSaveFailed"></param>
        public static void SavePinnedDataToWorld(World world, out bool cloudSaveFailed)
        {
            ZPackage zPackage = PackPinnedObjects();

            cloudSaveFailed = Utilities.DataUtils.SaveWorldData(world, zPackage);

        }
        #endregion

        #region Patches

        [HarmonyPatch(typeof(Minimap))]
        [HarmonyPatch(nameof(Minimap.RemovePin), new Type[] { typeof(Minimap.PinData) })]
        [HarmonyPrefix]
        public static bool RemovePin(Minimap __instance, Minimap.PinData pin)
        {
            //checks 
            if (pin == null || pin.m_name == null || pin.m_name == "")
            {
                return true;
            }


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





        #endregion

    }


}
