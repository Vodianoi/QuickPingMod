using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
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

    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class QuickPingPlugin : BaseUnityPlugin
    {

        public static QuickPingPlugin Instance { get; set; }


        public static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            Settings.Init();
            Harmony.CreateAndPatchAll(typeof(Player_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ChatPing_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Minimap_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Terminal_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(MineRock5_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Destructible_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(ZNet_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(WearNTear_Patch), MyPluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(Character_Patch), MyPluginInfo.PLUGIN_GUID);

            Player_Patch.OnPlayerPing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerPing.AddListener(Minimap_Patch.AddPin);
            Player_Patch.OnPlayerForcePing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerForcePing.AddListener(Minimap_Patch.ForceAddPin);
            Player_Patch.OnPlayerRename.AddListener(Player_Patch.SendRename);
            Player_Patch.OnPlayerRename.AddListener(Minimap_Patch.RenamePin);

        }
    }
}

