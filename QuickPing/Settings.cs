using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace QuickPing
{
    static class Settings
    {
        public const string pingText = "PING !";
        public static ConfigEntry<bool> PingWhereLooking { get; private set; }
        public static ConfigEntry<KeyCode> PingKey { get; private set; }
        public static ConfigEntry<bool> AddPin { get; private set; }
        public static ConfigEntry<Color> PlayerColor { get; private set; }
        public static ConfigEntry<Color> ShoutColor { get; private set; }
        public static ConfigEntry<Color> WhisperColor { get; private set; }
        public static ConfigEntry<Color> PingColor { get; private set; }
        public static ConfigEntry<Color> DefaultColor { get; private set; }
        public static ConfigEntry<float> ClosestPinRange { get; private set; }

        public static ButtonConfig pingBtn { get; private set; }


        public static void Init()
        {
            //GENERAL
            PingWhereLooking = QuickPing.Instance.Config.Bind("General",
                "PingWhereLooking",
                true,
                "Create a ping where you are looking when you press <PingKey>");

            AddPin = QuickPing.Instance.Config.Bind("General",
                "AddPinOnMap",
                true,
                "If true, add a pin when useful resources (copper, berries, campfire, portals etc.) are pinged.");

            ClosestPinRange = QuickPing.Instance.Config.Bind("General",
                "ClosestPinRange",
                2f,
                "Minimum distance between objects to pin/replace portal tag");

            //COLORS
            PlayerColor = QuickPing.Instance.Config.Bind("Colors",
                "PlayerColor",
                Color.green,
                "Color for Player name in pings/messages.");

            ShoutColor = QuickPing.Instance.Config.Bind("Colors",
                "ShoutColor",
                Color.yellow,
                "Color for Shout ping.");
            WhisperColor = QuickPing.Instance.Config.Bind("Colors",
                "WhisperColor",
                new Color(1f, 1f, 1f, 0.75f),
                "Color for Whisper ping.");
            PingColor = QuickPing.Instance.Config.Bind("Colors",
                "PingColor",
                new Color(0.6f, 0.7f, 1f, 1f),
                "Color for \"Ping\" ping.");
            DefaultColor = QuickPing.Instance.Config.Bind("Colors",
                "DefaultColor",
                Color.white,
                "Default color");

            //BINDINGS
            PingKey = QuickPing.Instance.Config.Bind("Bindings",
                "PingInputKey",
                KeyCode.T,
                "The keybind to trigger a ping where you are looking");
            AddInputs();
        }

        private static void AddInputs()
        {
            pingBtn = new ButtonConfig
            {
                Name = "Ping",
                Key = PingKey.Value,
                HintToken = pingText,
                ActiveInCustomGUI = true,

            };

            Jotunn.Managers.InputManager.Instance.AddButton(MyPluginInfo.GUID, pingBtn);

        }
    }
}
