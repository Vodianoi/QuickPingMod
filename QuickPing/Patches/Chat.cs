using HarmonyLib;
using TMPro;
using UnityEngine;

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
        public static bool AddInworldText(Chat __instance, GameObject go, long senderID, Vector3 position, Talker.Type type, UserInfo user, string text)
        {
            Chat.WorldTextInstance worldTextInstance = __instance.FindExistingWorldText(senderID);
            if (worldTextInstance == null)
            {
                worldTextInstance = new Chat.WorldTextInstance();
                worldTextInstance.m_talkerID = senderID;
                worldTextInstance.m_gui = Object.Instantiate(__instance.m_worldTextBase, __instance.transform);
                worldTextInstance.m_gui.gameObject.SetActive(value: true);
                Transform transform = worldTextInstance.m_gui.transform.Find("Text");
                worldTextInstance.m_textMeshField = transform.GetComponent<TextMeshProUGUI>();
                __instance.m_worldTexts.Add(worldTextInstance);
            }

            worldTextInstance.m_userInfo = user;
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
            worldTextInstance.m_textMeshField.richText = true;
            worldTextInstance.m_textMeshField.color = color;
            //worldTextInstance.m_textMeshField.GetComponent<Outline>().enabled = type != Talker.Type.Whisper;
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
        public static bool UpdateWorldTextField(Chat.WorldTextInstance wt)
        {
            string text = "";
            if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
            {
                //"<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + user + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
                text = "<color=#" + ColorUtility.ToHtmlStringRGBA(Settings.PlayerColor.Value) + ">" + wt.m_name + "</color>: ";
            }

            text += "<color=#" + ColorUtility.ToHtmlStringRGBA(wt.m_textMeshField.color) + ">" + wt.m_text + "</color>";
            wt.m_textMeshField.text = text;

            return false;
        }

    }

}
