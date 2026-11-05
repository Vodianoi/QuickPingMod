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
            Patch(typeof(Localization_Patch));
            Localization_Patch.AddTranslations();
            Patch(typeof(Player_Patch));
            Patch(typeof(ChatPing_Patch));
            Patch(typeof(Minimap_Patch));
            Patch(typeof(Terminal_Patch));
            Patch(typeof(SaveCollection_Patch));
            Patch(typeof(MineRock5_Patch));
            Patch(typeof(Destructible_Patch));
            Patch(typeof(ZNet_Patch));
            Patch(typeof(WearNTear_Patch));
            Patch(typeof(Character_Patch));
            Patch(typeof(Leviathan_Patch));

            Player_Patch.OnPlayerPing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerPing.AddListener(Minimap_Patch.AddPin);
            Player_Patch.OnPlayerForcePing.AddListener(Player_Patch.SendPing);
            Player_Patch.OnPlayerForcePing.AddListener(Minimap_Patch.ForceAddPin);
            Player_Patch.OnPlayerRename.AddListener(Player_Patch.SendRename);
            Player_Patch.OnPlayerRename.AddListener(Minimap_Patch.RenamePin);

#if DEBUG
            Utilities.DevDumpCommand.Register();
#endif
        }

        /// <summary>
        /// Apply one patch class. If a game update breaks it, log it and keep the rest of the mod working.
        /// </summary>
        private static void Patch(System.Type patchClass)
        {
            try
            {
                Harmony.CreateAndPatchAll(patchClass, MyPluginInfo.PLUGIN_GUID);
            }
            catch (System.Exception e)
            {
                Log.LogError($"Failed to patch {patchClass.Name}, this part of the mod is disabled: {e}");
            }
        }
    }
}

