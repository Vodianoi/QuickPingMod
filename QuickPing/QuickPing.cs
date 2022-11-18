using Jotunn.Entities;
using Jotunn.Managers;
using BepInEx;
using HarmonyLib;
using QuickPing.Patches;
using BepInEx.Logging;

namespace QuickPing
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class QuickPing : BaseUnityPlugin
    {
        public static QuickPing Instance { get; set; }
        public const string PluginGUID = "com.atopy.plugins.quickping";
        public const string PluginName = "QuickPing";
        public const string PluginVersion = "0.1.0";
        public static ManualLogSource Log { get; private set; }
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        
        private void Awake()
        {
            Log = Logger;
            Instance = this;
            Log.LogInfo($"Plugin {PluginGUID} is loaded!");

            Settings.Init();

            Harmony.CreateAndPatchAll(typeof(Player_Patch), PluginGUID);
            Harmony.CreateAndPatchAll(typeof(ChatPing_Patch), PluginGUID);
            Harmony.CreateAndPatchAll(typeof(Minimap_Patch), PluginGUID);


            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }
    }
}

