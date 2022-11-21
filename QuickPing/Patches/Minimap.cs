using HarmonyLib;
using System;
using UnityEngine;

namespace QuickPing.Patches
{
    internal static class Minimap_Patch
    {

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(global::Minimap), "GetClosestPin", new Type[] { typeof(Vector3), typeof(float) })]
        public static global::Minimap.PinData GetClosestPin(object instance, Vector3 pos, float radius) => throw new NotImplementedException();
    }
}
