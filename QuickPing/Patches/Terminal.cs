using HarmonyLib;
using System;
using UnityEngine;

namespace QuickPing.Patches
{
    internal class Terminal_Patch
    {

        [HarmonyPatch(typeof(Terminal), "AddString", new Type[] { typeof(string), typeof(string), typeof(Talker.Type), typeof(bool) })]
        [HarmonyPrefix]
        public static bool AddString(Terminal __instance, string user, string text, Talker.Type type, bool timestamp = false)
        {
            Color color;
            switch (type)
            {
                case Talker.Type.Shout:
                    color = Settings.ShoutColor.Value;
                    text = text.ToUpper();
                    break;
                case Talker.Type.Whisper:
                    color = Settings.WhisperColor.Value;
                    text = text.ToLowerInvariant();
                    break;
                default:
                    color = Settings.DefaultColor.Value;
                    break;
            }

            string text2 = (timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "");
            text2 = text2 + "<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + user + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
            __instance.AddString(text2);
            return false;
        }
    }
}
