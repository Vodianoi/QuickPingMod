using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace QuickPing
{
    static class Settings
    {

        public static ConfigEntry<bool> PingWhereLooking { get; private set; }
        public static ConfigEntry<KeyCode> PingKey { get; private set; }
        public static ConfigEntry<bool> AddPin { get; private set; }
        public static ConfigEntry<Color> ShoutColor { get; private set; }
        public static ConfigEntry<Color> WhisperColor { get; private set; }
        public static ConfigEntry<Color> PingColor { get; private set; }
        public static ConfigEntry<Color> DefaultColor { get; private set; }

        public static ButtonConfig pingBtn { get; private set; }

        public static void Init()
        {

            PingWhereLooking = QuickPing.Instance.Config.Bind("General",
                "PingWhereLooking",
                true,
                "Create a ping where you are looking when you press <PingKey>");

            PingKey = QuickPing.Instance.Config.Bind("General",
                "PingInputKey",
                KeyCode.T,
                "The keybind to trigger a ping where you are looking");

            AddPin = QuickPing.Instance.Config.Bind("General",
                "AddPinOnMap",
                true,
                "If true, add a pin when useful resources (copper, berries, campfire, portals etc.) are pinged.");

            ShoutColor = QuickPing.Instance.Config.Bind("General",
                "ShoutColor",
                Color.cyan,
                "Color for Shout ping.");
            WhisperColor = QuickPing.Instance.Config.Bind("General",
                "WhisperColor",
                new Color(1f, 1f, 1f, 0.75f),
                "Color for Whisper ping.");
            PingColor = QuickPing.Instance.Config.Bind("General",
                "PingColor",
                new Color(0.6f, 0.7f, 1f, 1f),
                "Color for \"Ping\" ping.");
            DefaultColor = QuickPing.Instance.Config.Bind("General",
                "DefaultColor",
                Color.white,
                "Default color.");

            AddInputs();
        }

        private static void AddInputs()
        {

            pingBtn = new ButtonConfig
            {
                Name = "Ping",
                Key = PingKey.Value,
                ActiveInCustomGUI = true,
                HintToken = "Ping !"
            };

            Jotunn.Managers.InputManager.Instance.AddButton(QuickPing.PluginGUID, pingBtn);
        }
    }
}
