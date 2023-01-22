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

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class QuickPingPlugin : BaseUnityPlugin
    {

        public static QuickPingPlugin Instance { get; set; }

        public static UnityEvent<DataManager.PinnedObject> OnPingEvent = new();
        public static UnityEvent<DataManager.PinnedObject> OnPingEverythingEvent = new();
        public static UnityEvent<DataManager.PinnedObject> OnRenameEvent = new();

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
            Harmony.CreateAndPatchAll(typeof(Player_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ChatPing_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Minimap_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Terminal_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(MineRock5_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Destructible_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ZNet_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(WearNTear_Patch), MyPluginInfo.PLUGIN_GUID);

            OnPingEvent.AddListener(RPC_Ping);
            OnPingEverythingEvent.AddListener(RPC_PingEverything);
            OnRenameEvent.AddListener(RPC_PingRename);

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }

        private void OnDestroy()
        {
            Harmony.UnpatchID(MyPluginInfo.PLUGIN_GUID);
        }

        private static void RPC_Ping(DataManager.PinnedObject pinnedObject)
        {
            Player_Patch.SendPing(pinnedObject);
            Minimap_Patch.AddPin(pinnedObject);
        }

        private static void RPC_PingEverything(DataManager.PinnedObject pinnedObject)
        {
            Player_Patch.SendPing(pinnedObject);
            Minimap_Patch.ForceAddPin(pinnedObject);
        }

        private static void RPC_PingRename(DataManager.PinnedObject pinnedObject)
        {
            Minimap_Patch.RenamePin(pinnedObject);
        }
    }
}

