using HarmonyLib;
using QuickPing.Utilities;

namespace QuickPing.Patches
{
    public class ZNet_Patch
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.LoadWorld))]
        [HarmonyPostfix]
        private static void LoadWorld(ZNet __instance)
        {
            DataManager.Load(ZNet.m_world, Game.instance.GetPlayerProfile());

        }



        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread))]
        [HarmonyPostfix]
        private static void SaveWorldThread()
        {
            bool cloudSaveFailed = DataManager.Save(ZNet.m_world, Game.instance.GetPlayerProfile());

            QuickPingPlugin.Log.LogInfo($"cloud save : {!cloudSaveFailed}");


        }

    }

}