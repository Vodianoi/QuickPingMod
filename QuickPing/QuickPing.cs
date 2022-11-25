using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using QuickPing.Patches;

namespace QuickPing
{

    public static class MyPluginInfo
    {
        public const string GUID = "com.atopy.plugins.quickping";
        public const string NAME = "QuickPing";
        public const string VERSION = "1.0.2";
    }

    [BepInPlugin(MyPluginInfo.GUID, MyPluginInfo.NAME, MyPluginInfo.VERSION)]
    public class QuickPing : BaseUnityPlugin
    {

        public static QuickPing Instance { get; set; }



        public static ManualLogSource Log { get; private set; }


        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {

            Log = Logger;
            Instance = this;
            //Log.LogInfo($"Plugin {MyPluginInfo.GUID} is loaded!");

            Settings.Init();
            Harmony.CreateAndPatchAll(typeof(Player_Patch), MyPluginInfo.GUID);
            Harmony.CreateAndPatchAll(typeof(ChatPing_Patch), MyPluginInfo.GUID);
            Harmony.CreateAndPatchAll(typeof(Minimap_Patch), MyPluginInfo.GUID);
            Harmony.CreateAndPatchAll(typeof(Terminal_Patch), MyPluginInfo.GUID);
            Harmony.CreateAndPatchAll(typeof(MineRock5_Patch), MyPluginInfo.GUID);

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }
    }
}

