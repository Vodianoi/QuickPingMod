using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using QuickPing.Patches;
using UnityEngine;


namespace QuickPing
{
    public enum HoverType
    {
        GameObject,
        Character,
        Hoverable,
        Piece,
        Location
    }
    public class HoverObject : MonoBehaviour
    {
        public string Name { get; set; }
        public GameObject Hover;
        public IDestructible Destructible;
        public Vector3 pos;
        public Vector3 center;
        public HoverType type;
        public bool pinable;
    }

    [BepInPlugin(PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class QuickPingPlugin : BaseUnityPlugin
    {

        public const string PLUGIN_GUID = "com.atopy.plugins.quickping";
        public static QuickPingPlugin Instance { get; set; }


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
            Harmony.CreateAndPatchAll(typeof(Player_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ChatPing_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Minimap_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Terminal_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(MineRock5_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Destructible_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ZNet_Patch), PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(WearNTear_Patch), PLUGIN_GUID);

            Player_Patch.OnPlayerPing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerPing.AddListener(Minimap_Patch.AddPin);
            Player_Patch.OnPlayerForcePing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerForcePing.AddListener(Minimap_Patch.ForceAddPin);

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }
    }
}

