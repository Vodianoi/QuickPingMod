using HarmonyLib;
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
        private static GameObject panel;
        private static InputField nameInput;
        private static Toggle toggleSaveName;

        public static bool IsNaming = false;

        private static string tempOriginalText;

        /// <summary>
        /// Pin type for each pingable name token (or literal name). Tokens come from the game data
        /// (Valheim 1.0.21, see the quickping_dump dev command). Location tokens are mapped in Localization_Patch.
        /// </summary>
        private static readonly Dictionary<Minimap.PinType, List<string>> Pinables = new()
        {
            //Fire pin
            {
                Minimap.PinType.Icon0,
                new List<string>
                {
                    "$piece_firepit",
                    "$piece_bonfire",
                    "$piece_fire",
                    "GoblinCamp",
                }
            },

            //Home pin: traders and settlements
            {
                Minimap.PinType.Icon1,
                new List<string>
                {
                    "Haldor",
                    "Hildir",
                    "$npc_bogwitch",
                    "$quickping_location_village",
                    "$quickping_location_farm",
                    "$quickping_location_lumbercamp",
                    "$quickping_location_harbour",
                    "$quickping_location_house",
                    "$quickping_location_swamphut",
                }
            },

            //Hammer pin: ores and minable resources
            {
                Minimap.PinType.Icon2,
                new List<string>
                {
                    "$piece_deposit_copper",
                    "$piece_deposit_tin",
                    "$piece_deposit_obsidian",
                    "$piece_deposit_silver",
                    "$piece_deposit_silvervein",
                    "$piece_mudpile",
                    "$piece_giant_brain",
                    "$item_softtissue", // same text as $piece_giant_brain
                    "$piece_giant_bone",
                    "$piece_giant_helmet",
                    "$piece_giant_sword",
                    "$item_flametalore",
                    "$item_pickable_bogiron",
                    "$item_pickable_sulfurrock",
                    "$piece_deposit_goldvein", // Deep North: Petrified Gammeltroll
                    "$item_tar",                    "$item_destructible_gucksack",
                    "$quickping_leviathan",
                }
            },

            //Point pin: wild pickables
            {
                Minimap.PinType.Icon3,
                new List<string>
                {
                    "$item_raspberries",
                    "$item_blueberries",
                    "$item_cloudberries",
                    "$item_lingonberries",
                    "$item_vineberry",
                    "$item_dragonegg",
                    "$item_voltureegg",
                    "$item_dandelion",
                    "$item_mushroomcommon",
                    "$item_mushroomyellow",
                    "$item_magecap",
                    "$item_mushroomblue",
                    "$item_thistle",
                    "$item_jotunpuffs",
                    "$item_royaljelly",
                    "$item_fiddleheadfern",
                    "$item_smokepuff",
                    "$item_glowworm",
                    "$item_ancientroot",
                    "$item_carrotseeds",
                    "$item_turnipseeds",
                    "$item_onionseeds",
                    "$item_kaleseeds",
                    // fix #58 conflicts with PlantEverything
                    "$peRaspberryBushName",
                    "$peBlueberryBushName",
                    "$peCloudberryBushName",
                    "$pePickableMushroomName",
                    "$pePickableYellowMushroomName",
                    "$pePickableBlueMushroomName",
                    "$pePickableThistleName",
                    "$pePickableDandelionName",
                    "$pePickableJotunpuffsName",
                }
            },

            //Rune pin: dungeons and points of interest
            {
                Minimap.PinType.Icon4,
                new List<string>
                {
                    "$location_forestcave",
                    "$location_forestcrypt",
                    "$location_bearcave",
                    "Stone Henge",
                    "$location_sunkencrypt",
                    "$location_mountaincave",
                    "$location_dvergrtown",
                    "$location_dvergrboss",
                    "$location_morgenhole",
                    "$charredfortress",
                    "$location_thehole",
                    "$location_darkesthole",
                    "$location_dnbossroom",
                    "$location_dnbossroomnew",
                    "$location_morkhalla",
                    "$location_mausoleum",
                    "$quickping_location_ruins",
                    "$quickping_location_tower",
                    "$quickping_location_lighthouse",
                    "$quickping_location_shipwreck",
                    "$piece_portal",
                }
            },

            //Boss pin: boss altars (locations named after their boss, see Localization_Patch)
            {
                Minimap.PinType.Boss,
                new List<string>
                {
                    "$enemy_eikthyr",
                    "$enemy_gdking",
                    "$enemy_bonemass",
                    "$enemy_dragon",
                    "$enemy_goblinking",
                    "$enemy_fader",
                }
            },

            //Hildir's quest dungeons use their vanilla map icons
            { Minimap.PinType.Hildir1, new List<string> { "$hud_pin_hildir1" } },
            { Minimap.PinType.Hildir2, new List<string> { "$hud_pin_hildir2" } },
            { Minimap.PinType.Hildir3, new List<string> { "$hud_pin_hildir3" } },
        };

        /// <summary>
        /// Check if an object can be pinned
        /// </summary>
        /// <param name="strID">Name token or localized name of the pinged object</param>
        /// <returns>Pin type, or None if it is not pinned automatically</returns>
        public static Minimap.PinType IsPinable(string strID)
        {
            string baseLocalization = Localization_Patch.GetBaseTranslation(strID);

            foreach (var pinables in Pinables)
            {
                if (pinables.Value.Contains(strID) || pinables.Value.Contains(baseLocalization))
                {
                    return pinables.Key;
                }
            }

            return Minimap.PinType.None;
        }

        /// <summary>
        /// Pin type of a pinged object, or None if it is not pinned automatically.
        /// </summary>
        private static Minimap.PinType GetPinType(GameObject hover, string strID)
        {
            // Dropped items share their name with pickables (berries, mushrooms...)
            if (hover && hover.GetComponent<ItemDrop>())
                return Minimap.PinType.None;

            Minimap.PinType type = IsPinable(strID);

            // Boss names pin their altar, not the boss during the fight
            if (type == Minimap.PinType.Boss && hover && hover.GetComponent<Character>())
                return Minimap.PinType.None;

            return type;
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
        public static void ForceAddPin(HoverObject obj) =>
            AddPin(obj.Hover, obj.Destructible, obj.Name, obj.center, true);

        /// <summary>
        /// Add correct pin to map if Settings.
        /// </summary>
        /// <param name="hover">Pinged object</param>
        /// <param name="idestructible">IDestructible interface found in "hover"</param>
        /// <param name="strID">Display Name of hover object</param>
        /// <param name="pos">Pinged position (last raycast point or hover center)</param>
        /// <param name="force">Bypass filters</param>
        public static void AddPin(GameObject hover, IDestructible idestructible, string strID, Vector3 pos,
            bool force = false)
        {
            if (!Settings.AddPin.Value && !force)
            {
                return;
            }

            Minimap.PinType type = GetPinType(hover, strID);
            Minimap.PinData closestPin = Minimap.instance.GetClosestPin(pos, Settings.ClosestPinRange.Value);

            if (strID == "$piece_portal")
            {
                if (!hover || !hover.TryGetComponent(out Hoverable hoverable))
                    return;

                // Replace this portal's pin: its tag may have changed
                if (closestPin != null)
                {
                    Minimap.instance.RemovePin(closestPin);
                }

                AddTrackedPin(idestructible, pos, type, GetPortalTag(hoverable));
                return;
            }

            if (closestPin != null)
            {
                return;
            }

            if (type == Minimap.PinType.None)
            {
                if (!force) return;
                type = Settings.DefaultPinType.Value;
            }

            AddTrackedPin(idestructible, pos, type, ResolvePinName(strID));
        }

        /// <summary>
        /// Pin name for an object: the name saved by the player, else its localized name.
        /// </summary>
        private static string ResolvePinName(string strID)
        {
            if (string.IsNullOrEmpty(strID))
                return Settings.DefaultPingText;

            if (DataManager.CustomNames.TryGetValue(strID, out string customName))
                return customName;

            string name = Localization.instance.Localize(strID);
            return string.IsNullOrEmpty(name) ? Settings.DefaultPingText : name;
        }

        private static Minimap.PinData AddTrackedPin(IDestructible idestructible, Vector3 pos, Minimap.PinType type, string name)
        {
            Minimap.PinData pinData = Minimap.instance.AddPin(pos, type, name, true, false, 0L);
            QuickPingPlugin.Log.LogInfo($"Add Pin : Name:{name} x:{pos.x}, y:{pos.y}, Type:{type}");
            TrackPin(idestructible, pinData);
            return pinData;
        }

        /// <summary>
        /// Link a pin to its object, so it is removed when the object is destroyed.
        /// </summary>
        private static void TrackPin(IDestructible idestructible, Minimap.PinData pinData)
        {
            if (idestructible == null) return;

            FieldInfo fieldInfo = idestructible.GetType()
                .GetField("m_nview", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                QuickPingPlugin.Log.LogWarning(
                    $"Unable to link destructible {idestructible} to pin: {pinData.m_name}. (Is it a god?)");
                return;
            }

            ZNetView netView = fieldInfo.GetValue(idestructible) as ZNetView;
            if (!netView || netView.GetZDO() == null)
            {
                QuickPingPlugin.Log.LogWarning($"Unable to link destructible {idestructible} to pin: {pinData.m_name}. (No ZDO)");
                return;
            }

            ZDOID uid = netView.GetZDO().m_uid;

            if (!DataManager.PinnedObjects.ContainsKey(uid))
            {
                DataManager.PinnedObjects[uid] = pinData;
            }
        }

        #region CustomNamePing

        /// <summary>
        /// Icons offered by the Custom Name Ping window (the ones of the vanilla map).
        /// </summary>
        private static readonly Minimap.PinType[] PinIcons =
        {
            Minimap.PinType.Icon0,
            Minimap.PinType.Icon1,
            Minimap.PinType.Icon2,
            Minimap.PinType.Icon3,
            Minimap.PinType.Icon4,
        };

        private static readonly Dictionary<Minimap.PinType, Image> iconImages = new();

        private static Minimap.PinData editedPin;
        private static Vector3 editedPos;
        private static IDestructible editedDestructible;
        private static Minimap.PinType selectedType;
        private static int closingFrame = -1;

        /// <summary>
        /// Custom Name Ping: choose the name and icon of the pin where the player is looking.
        /// Edits the pin already there, or prepares a new one.
        /// </summary>
        internal static void OpenCustomNamePing(HoverObject obj)
        {
            if (IsNaming || !Minimap.instance) return;

            editedPin = Minimap.instance.GetClosestPin(obj.center, Settings.ClosestPinRange.Value);
            editedPos = obj.center;
            editedDestructible = obj.Destructible;
            tempOriginalText = obj.Name;

            string name;
            if (editedPin != null)
            {
                name = editedPin.m_name;
                selectedType = editedPin.m_type;
            }
            else
            {
                name = ResolvePinName(obj.Name);
                selectedType = GetPinType(obj.Hover, obj.Name);
                if (selectedType == Minimap.PinType.None)
                    selectedType = Settings.DefaultPinType.Value;
            }

            if (!InitNameInput(name)) return;

            closingFrame = -1;
            InputBlocker.Block(true);
        }

        public static void UpdateNameInput()
        {
            // Release the game input one frame after closing, so Enter/Escape don't also open the chat or menu
            if (closingFrame >= 0)
            {
                if (Time.frameCount > closingFrame)
                {
                    closingFrame = -1;
                    IsNaming = false;
                    InputBlocker.Block(false);
                }
                return;
            }

            if (!panel || !nameInput)
            {
                CloseCustomNamePing();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ValidateNameInput(nameInput, toggleSaveName.isOn);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCustomNamePing();
                return;
            }

            // Keep typing in the name field, unless an icon was just clicked
            if (EventSystem.current && !EventSystem.current.currentSelectedGameObject)
            {
                EventSystem.current.SetSelectedGameObject(nameInput.gameObject);
            }
        }

        private static void CloseCustomNamePing()
        {
            DestroyGUI();
            editedPin = null;
            editedDestructible = null;
            tempOriginalText = null;
            closingFrame = Time.frameCount;
        }

        private static void DestroyGUI()
        {
            iconImages.Clear();
            if (panel) GameObject.Destroy(panel);
            panel = null;
            nameInput = null;
            toggleSaveName = null;
        }

        private static void ValidateNameInput(InputField inputField, bool save)
        {
            string text = inputField.text;
            text = text.Replace('$', ' ');
            text = text.Replace('<', ' ');
            text = text.Replace('>', ' ');
            string originalText = tempOriginalText;

            if (editedPin != null && Minimap.instance.m_pins.Contains(editedPin))
            {
                UpdatePin(editedPin, text, selectedType);
            }
            else
            {
                AddTrackedPin(editedDestructible, editedPos, selectedType, text);
            }

            // Persistent save of text value for this pinned object
            if (save && !string.IsNullOrEmpty(originalText))
            {
                if (text == "")
                {
                    DataManager.CustomNames.Remove(originalText);
                }
                else
                {
                    QuickPingPlugin.Log.LogInfo($"Save name {text} for {originalText}");
                    SaveName(text, originalText);
                }
            }

            CloseCustomNamePing();
        }

        /// <summary>
        /// Rename and change the icon of a pin already on the map.
        /// </summary>
        private static void UpdatePin(Minimap.PinData pin, string name, Minimap.PinType type)
        {
            // Pins loaded from the save are tracked by copy (matched on name and position): track this one instead
            foreach (ZDOID id in DataManager.PinnedObjects.Where(p => p.Value.Compare(pin)).Select(p => p.Key).ToList())
            {
                DataManager.PinnedObjects[id] = pin;
            }

            pin.m_name = name;
            pin.m_type = type;
            pin.m_icon = Minimap.instance.GetSprite(type);

            // Markers are rebuilt on the next map update
            Minimap.instance.DestroyPinMarker(pin);
            pin.m_NamePinData = string.IsNullOrEmpty(name) ? null : new Minimap.PinNameData(pin);
            Minimap.instance.m_pinUpdateRequired = true;
        }

        /// <summary>
        /// Persistent save original name for this pinned object
        /// </summary>
        /// <param name="m_name"></param>
        /// <param name="originalName"></param>
        private static void SaveName(string m_name, string originalName)
        {
            DataManager.CustomNames[originalName] = m_name;
        }

        private static void SelectIcon(Minimap.PinType type)
        {
            selectedType = type;
            foreach (var icon in iconImages)
            {
                icon.Value.color = icon.Key == type ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
        }

        private static bool InitNameInput(string name)
        {
            Transform canvas = GameUI.Canvas;
            if (!canvas) return false;

            IsNaming = true;

            panel = GameUI.CreateWoodPanel(canvas, 260f, 150f);

            var verticalLayoutGroup = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.spacing = 10f;
            verticalLayoutGroup.padding = new RectOffset(15, 15, 15, 15);
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            nameInput = GameUI.CreateInputField(panel.transform, "Pin Name", 16, 200f, 30f);
            nameInput.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            nameInput.text = name;

            // Icon picker
            GameObject icons = new GameObject("Icons", typeof(RectTransform));
            icons.transform.SetParent(panel.transform, false);
            icons.AddComponent<LayoutElement>().preferredHeight = 32f;
            var iconsLayout = icons.AddComponent<HorizontalLayoutGroup>();
            iconsLayout.spacing = 8f;
            iconsLayout.childAlignment = TextAnchor.MiddleCenter;
            iconsLayout.childControlWidth = false;
            iconsLayout.childControlHeight = false;
            iconsLayout.childForceExpandWidth = false;
            iconsLayout.childForceExpandHeight = false;

            foreach (Minimap.PinType type in PinIcons)
            {
                GameObject icon = new GameObject(type.ToString(), typeof(RectTransform));
                icon.transform.SetParent(icons.transform, false);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(32f, 32f);

                Image image = icon.AddComponent<Image>();
                image.sprite = Minimap.instance.GetSprite(type);
                image.preserveAspect = true;

                Button button = icon.AddComponent<Button>();
                button.targetGraphic = image;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => SelectIcon(type));

                iconImages[type] = image;
            }
            SelectIcon(selectedType);

            toggleSaveName = GameUI.CreateToggle(panel.transform, "Remember this name", 20f);
            toggleSaveName.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(nameInput.gameObject);
            return true;
        }

        #endregion

        /// <summary>
        /// Portal tag shown in its hover text ("Portal "tag""), or the portal name when it has no tag.
        /// </summary>
        private static string GetPortalTag(Hoverable hoverable)
        {
            string[] parts = hoverable.GetHoverText().Split('"');
            if (parts.Length > 2 && parts[1].Trim() != "")
                return parts[1];

            return Localization.instance.Localize(hoverable.GetHoverName());
        }

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


            foreach (var p in DataManager.PinnedObjects)
            {
                if (p.Value.Compare(pin))
                {
                    // The stored PinData may be a copy: target the pin actually on the map
                    pin = __instance.GetClosestPin(p.Value.m_pos, Settings.ClosestPinRange.Value) ?? pin;
                    DataManager.PinnedObjects.Remove(p.Key);
                    break;
                }
            }

            // --- vanilla ---
            __instance.m_pinUpdateRequired = true;
            __instance.DestroyPinMarker(pin);
            __instance.m_pins.Remove(pin);
            return false;
        }

        #endregion
    }
}