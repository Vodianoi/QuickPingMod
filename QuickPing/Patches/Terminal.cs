using HarmonyLib;
using Splatform;
using System;
using UnityEngine;

namespace QuickPing.Patches
{
    internal class Terminal_Patch
    {

        [HarmonyPatch(typeof(Terminal), "AddString", new Type[] { typeof(PlatformUserID), typeof(string), typeof(Talker.Type), typeof(bool) })]
        [HarmonyPrefix]
        public static bool AddString(Terminal __instance, PlatformUserID user, string text, Talker.Type type, bool timestamp = false)
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

            // Resolve the character name like vanilla does (falls back to the raw ID)
            string userName = ZNet.TryGetPlayerByPlatformUserID(user, out ZNet.PlayerInfo playerInfo)
                ? CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, user)
                : user.ToString();

            string text2 = (timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "");
            text2 = text2 + "<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + userName + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
            __instance.AddString(text2);
            return false;
        }
    }
}
