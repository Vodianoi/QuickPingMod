using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

namespace QuickPing
{
    static class Settings
    {

        public const string DefaultPingText = "PING !";
        public static ConfigEntry<bool> PingWhereLooking { get; private set; }
        public static ConfigEntry<bool> AddPin { get; private set; }
        public static ConfigEntry<KeyCode> PingKey { get; private set; }
        public static ConfigEntry<KeyCode> PinEverythingKey { get; internal set; }
        public static ConfigEntry<KeyboardShortcut> CustomNamePingKey { get; internal set; }

        public static ConfigEntry<Minimap.PinType> DefaultPinType { get; internal set; }

        public static ConfigEntry<Color> PlayerColor { get; private set; }
        public static ConfigEntry<Color> ShoutColor { get; private set; }
        public static ConfigEntry<Color> WhisperColor { get; private set; }
        public static ConfigEntry<Color> PingColor { get; private set; }
        public static ConfigEntry<Color> DefaultColor { get; private set; }
        public static ConfigEntry<float> ClosestPinRange { get; private set; }

        public static ConfigEntry<bool> ShowDistance { get; private set; }


        public static void Init()
        {
            //GENERAL
            PingWhereLooking = QuickPingPlugin.Instance.Config.Bind("General",
            "PingWhereLooking",
            true,
            "Create a ping where you are looking when you press <PingKey>");

            AddPin = QuickPingPlugin.Instance.Config.Bind("General",
                "AddPinOnMap",
                true,
                "If true, add a pin when useful resources (copper, berries, campfire, portals etc.) are pinged.");

            ClosestPinRange = QuickPingPlugin.Instance.Config.Bind("General",
                "ClosestPinRange",
                2f,
                "Minimum distance between objects to pin/replace portal tag");

            //PING
            ShowDistance = QuickPingPlugin.Instance.Config.Bind("Ping",
                "ShowDistance",
                false,
                "Show your distance to the ping below its text.");

            DefaultPinType = QuickPingPlugin.Instance.Config.Bind("General",
                "DefaultPinType",
                Minimap.PinType.RandomEvent, //ExclamationPoint
                "Default pin when forcing adding a pin on map");

            //COLORS
            PlayerColor = QuickPingPlugin.Instance.Config.Bind("Colors",
                "PlayerColor",
                Color.green,
                "Color for Player name in pings/messages.");

            ShoutColor = QuickPingPlugin.Instance.Config.Bind("Colors",
                "ShoutColor",
                Color.yellow,
                "Color for Shout ping.");
            WhisperColor = QuickPingPlugin.Instance.Config.Bind("Colors",
                "WhisperColor",
                new Color(1f, 1f, 1f, 0.75f),
                "Color for Whisper ping.");
            PingColor = QuickPingPlugin.Instance.Config.Bind("Colors",
                "PingColor",
                new Color(0.6f, 0.7f, 1f, 1f),
                "Color for \"Ping\" ping.");
            DefaultColor = QuickPingPlugin.Instance.Config.Bind("Colors",
                "DefaultColor",
                Color.white,
                "Default color");

            //BINDINGS
            PingKey = QuickPingPlugin.Instance.Config.Bind("Bindings",
                "PingInputKey",
                KeyCode.T,
                "The keybind to trigger a ping where you are looking");

            PinEverythingKey = QuickPingPlugin.Instance.Config.Bind("Bindings",
                "PingEverythingInputKey",
                KeyCode.G,
                "Add a pin on minimap to whatever you're looking at.");
            var customNamePingDefault = new KeyboardShortcut(PingKey.Value, KeyCode.LeftAlt);
            CustomNamePingKey = QuickPingPlugin.Instance.Config.Bind("Bindings",
                "CustomNamePingInputKey",
                customNamePingDefault,
                "Ping where you are looking and choose the name and icon of its pin (also where there is no pin yet).");
            MigrateKey(QuickPingPlugin.Instance.Config, "RenameInputKey", CustomNamePingKey, customNamePingDefault);
        }

        /// <summary>
        /// Keep the binding a player set under a key's old name (renamed in 1.7.0), then drop the old name.
        /// </summary>
        internal static void MigrateKey(ConfigFile config, string oldKey, ConfigEntry<KeyboardShortcut> entry, KeyboardShortcut defaultValue)
        {
            ConfigEntry<KeyboardShortcut> old = config.Bind(entry.Definition.Section, oldKey, defaultValue);
            if (!old.Value.Equals(defaultValue))
                entry.Value = old.Value;

            config.Remove(old.Definition);
            config.Save();
        }

        /// <summary>
        /// Key pressed this frame (keyboard, mouse or gamepad button).
        /// </summary>
        public static bool IsDown(KeyCode key)
        {
            return key != KeyCode.None && ZInput.GetKeyDown(key, false);
        }

        /// <summary>
        /// Main key pressed this frame while all its modifiers are held.
        /// </summary>
        public static bool IsDown(KeyboardShortcut shortcut)
        {
            return IsDown(shortcut.MainKey) && shortcut.Modifiers.All(key => ZInput.GetKey(key, false));
        }
    }
}
