#if DEBUG
using HarmonyLib;
using QuickPing.Utilities;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace QuickPing.Tests
{
    /// <summary>
    /// With the -quickping-test launch argument: load (or create) the QuickPingTest character and world,
    /// run the in-game tests, then quit. The menu's last character and world are put back before quitting.
    /// Used by tests/run-ingame-tests.ps1.
    /// </summary>
    internal static class AutoRun
    {
        public const string LaunchArgument = "-quickping-test";
        private const string TestName = "QuickPingTest";
        private const string TestSeed = "QuickPing";

        public static bool Enabled { get; } = Environment.GetCommandLineArgs()
            .Any(arg => string.Equals(arg, LaunchArgument, StringComparison.OrdinalIgnoreCase));

        private static bool menuStarted;
        private static bool testsStarted;
        private static string savedProfile;
        private static string savedWorld;

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        [HarmonyPostfix]
        private static void MenuStart(FejdStartup __instance)
        {
            // Only once: if the world fails to load, the game comes back to the menu and the script times out
            if (!Enabled || menuStarted) return;
            menuStarted = true;
            __instance.StartCoroutine(StartTestWorld(__instance));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        private static void PlayerSpawned(Player __instance)
        {
            if (!Enabled || testsStarted || __instance != Player.m_localPlayer) return;
            testsStarted = true;
            QuickPingPlugin.Instance.StartCoroutine(RunTestsAndQuit());
        }

        private static IEnumerator StartTestWorld(FejdStartup menu)
        {
            yield return new WaitForSecondsRealtime(1f);
            Log($"starting {TestName} world");
            savedProfile = PlatformPrefs.GetString("profile");
            savedWorld = PlatformPrefs.GetString("world");

            string profileFile = TestName.ToLower();
            if (!PlayerProfile.HaveProfile(profileFile))
            {
                // Same steps as the new character screen (it needs its character preview)
                menu.OnCharacterNew();
                yield return null;
                menu.m_csNewCharacterName.text = TestName;
                menu.OnNewCharacterDone(forceLocal: true);
                yield return null;
            }

            menu.SetSelectedProfile(profileFile);
            PlayerProfile profile = menu.m_profiles[menu.m_profileIndex];
            if (profile.GetFilename() != profileFile)
            {
                Quit($"character {TestName} not found");
                yield break;
            }
            // No intro text or Valkyrie flight: the tests need the player on the ground
            profile.m_firstSpawn = false;
            profile.Save();

            menu.OnCharacterStart();
            yield return null;

            if (!World.HaveWorld(TestName))
            {
                menu.m_newWorldName.text = TestName;
                menu.m_newWorldSeed.text = TestSeed;
                menu.OnNewWorldDone(forceLocal: true);
                yield return null;
            }

            menu.m_world = menu.FindWorld(TestName);
            if (menu.m_world == null)
            {
                Quit($"world {TestName} not found");
                yield break;
            }

            // Local game only: no server listing or online login
            menu.m_openServerToggle.isOn = false;
            menu.m_publicServerToggle.isOn = false;
            menu.m_crossplayServerToggle.isOn = false;
            menu.OnWorldStart();
        }

        private static IEnumerator RunTestsAndQuit()
        {
            float end = Time.realtimeSinceStartup + 60f;
            while (!ZoneSystem.instance.IsZoneLoaded(Player.m_localPlayer.transform.position) && Time.realtimeSinceStartup < end)
                yield return null;
            // Let the terrain and nearby objects finish loading
            yield return new WaitForSeconds(3f);

            ResetTestWorld();
            bool done = false;
            TestRunner.Start("", _ => done = true);
            while (!done) yield return null;

            Quit(null);
        }

        /// <summary>
        /// Start every run from the same state: no pins, remembered names or dropped items left by an earlier run.
        /// Only in the QuickPingTest world.
        /// </summary>
        private static void ResetTestWorld()
        {
            Vector3 player = Player.m_localPlayer.transform.position;
            foreach (ItemDrop drop in ItemDrop.s_instances.Where(d => d && Vector3.Distance(d.transform.position, player) < 30f).ToList())
                ZNetScene.instance.Destroy(drop.gameObject);

            foreach (Minimap.PinData pin in Minimap.instance.m_pins.Where(p => p.m_save).ToList())
                Minimap.instance.RemovePin(pin);
            DataManager.PinnedObjects.Clear();
            DataManager.CustomNames.Clear();
            int leftovers = InGameTests.RemoveLeftovers(30f);
            Log($"test world reset ({leftovers} test objects left by an earlier run removed)");
        }

        private static void Quit(string error)
        {
            if (error != null)
            {
                QuickPingPlugin.Log.LogError($"AutoRun: {error}");
                System.IO.File.WriteAllText(TestRunner.ResultsPath, $"{TestRunner.ResultLinePrefix} setup failed: {error}\n");
            }

            Restore("profile", savedProfile);
            Restore("world", savedWorld);
            PlatformPrefs.Save();
            Application.Quit();
        }

        private static void Restore(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) PlatformPrefs.DeleteKey(key);
            else PlatformPrefs.SetString(key, value);
        }

        private static void Log(string text) => QuickPingPlugin.Log.LogInfo($"AutoRun: {text}");
    }
}
#endif
