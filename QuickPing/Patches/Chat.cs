using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QuickPing.Patches
{

    public class ChatPing_Patch
    {

        [HarmonyPatch(typeof(Chat))]
        [HarmonyPatch("AddInworldText")]
        [HarmonyPrefix]
        [HarmonyDebug]
        public static bool AddInworldText(Chat __instance, GameObject go, long senderID, Vector3 position, Talker.Type type, string user, string text)
        {
            Chat.WorldTextInstance worldTextInstance = __instance.FindExistingWorldText(senderID);
            if (worldTextInstance == null)
            {
                worldTextInstance = new Chat.WorldTextInstance();
                worldTextInstance.m_talkerID = senderID;
                worldTextInstance.m_gui = UnityEngine.Object.Instantiate(__instance.m_worldTextBase, __instance.transform);
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
                    break;
                default:
                    color = Settings.DefaultColor.Value;
                    break;
            }
            worldTextInstance.m_textField.color = color;
            worldTextInstance.m_textField.GetComponent<Outline>().enabled = type != Talker.Type.Whisper;
            worldTextInstance.m_timer = 0f;
            worldTextInstance.m_text = text;

            __instance.UpdateWorldTextField(worldTextInstance);

            return false;
        }




    }

}
