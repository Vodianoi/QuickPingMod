using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QuickPing.Patches
{

    public class ChatPing_Patch
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="go"></param>
        /// <param name="senderID"></param>
        /// <param name="position"></param>
        /// <param name="type"></param>
        /// <param name="user"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(Chat))]
        [HarmonyPatch(nameof(Chat.AddInworldText))]
        [HarmonyPrefix]
        public static bool AddInworldText(Chat __instance, GameObject go, long senderID, Vector3 position, Talker.Type type, string user, string text)
        {
            Chat.WorldTextInstance worldTextInstance = __instance.FindExistingWorldText(senderID);
            if (worldTextInstance == null)
            {
                worldTextInstance = new Chat.WorldTextInstance();
                worldTextInstance.m_talkerID = senderID;
                worldTextInstance.m_gui = Object.Instantiate(__instance.m_worldTextBase, __instance.transform);
                worldTextInstance.m_gui.gameObject.SetActive(value: true);
                worldTextInstance.m_textField = worldTextInstance.m_gui.transform.Find("Text").GetComponent<Text>();
                __instance.m_worldTexts.Add(worldTextInstance);
            }

            worldTextInstance.m_name = user;
            worldTextInstance.m_type = type;
            worldTextInstance.m_go = go;
            worldTextInstance.m_position = position;
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
                case Talker.Type.Ping:
                    color = Settings.PingColor.Value;
                    if (text == string.Empty)
                        text = Settings.DefaultPingText;
                    break;
                default:
                    color = Settings.DefaultColor.Value;
                    break;
            }

            worldTextInstance.m_textField.supportRichText = true;
            worldTextInstance.m_textField.color = color;
            worldTextInstance.m_textField.GetComponent<Outline>().enabled = type != Talker.Type.Whisper;
            worldTextInstance.m_timer = 0f;
            worldTextInstance.m_text = text;

            __instance.UpdateWorldTextField(worldTextInstance);

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="wt"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTextField))]
        [HarmonyPrefix]
        public static bool UpdateWorldTextField(Chat __instance, Chat.WorldTextInstance wt)
        {
            string text = "";
            if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
            {
                //"<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + user + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
                text = "<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + wt.m_name + ": ";
            }

            text += "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(wt.m_textField.color) + ">" + wt.m_text + "</color>";
            wt.m_textField.text = text;

            return false;
        }






    }

}
