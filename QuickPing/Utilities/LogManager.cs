using BepInEx.Logging;
using UnityEngine;
using static QuickPing.Utilities.DataManager;

namespace QuickPing.Utilities
{
    /// <summary>
    /// Logger class, uses Settings.LogLevel to determine what to log
    /// </summary>
    internal static class LogManager
    {
        private static readonly ManualLogSource Logger = QuickPingPlugin.Log;
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (level > Settings.LogLevel.Value) return;
            switch (level)
            {
                case LogLevel.Debug:
                    Logger.LogDebug(message);
                    break;
                case LogLevel.Info:
                    Logger.LogInfo(message);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Logger.LogError(message);
                    break;
            }
        }

        public static void Log(object obj, LogLevel level = LogLevel.Info)
        {
            Log(obj.ToString(), level);
        }

        public static void Log(DataManager.PinnedObject pinnedObject, LogLevel level = LogLevel.Info)
        {
            Log($"PinnedObject: {pinnedObject}", level);
        }


        public static void Log(GameObject obj, HoverType type, IDestructible destructible)
        {

            switch (type)
            {
                case HoverType.GameObject:
                    Log($"GameObject: {obj.name} \n-> {obj}", LogLevel.Warning);
                    break;
                case HoverType.Hoverable:
                    if (obj.TryGetComponent(out Hoverable hoverable))
                        Log($"Hoverable: {obj.name} \n-> {hoverable.GetHoverName()}", LogLevel.Warning);
                    else
                        Log($"Hoverable: {obj.name} \n-> {obj}", LogLevel.Warning);
                    break;
                case HoverType.Piece:
                    if (obj.TryGetComponent(out Piece piece))
                        Log($"Piece: {obj.name} \n-> {piece.m_name} \n-> {piece} \n-> trad: {Localization.instance.Localize(piece.m_name)}", LogLevel.Warning);
                    else
                        Log($"Piece: {obj.name} \n-> {obj}", LogLevel.Warning);
                    break;
                case HoverType.Location:
                    if (obj.TryGetComponent(out Location location))
                        Log($"Location: {obj.name} \n-> {location} \n-> trad: {Localization.instance.Localize(location.name)}", LogLevel.Warning);
                    else
                        Log($"Location: {obj.name} \n-> {obj}", LogLevel.Warning);
                    break;
                case HoverType.Character:
                    if (obj.TryGetComponent(out Character character))
                        Log($"Character: {obj.name} \n-> {character} \n-> trad: {Localization.instance.Localize(character.GetHoverName())}", LogLevel.Warning);
                    else
                        Log($"Character: {obj.name} \n-> {obj}", LogLevel.Warning);
                    break;
            }
            if (destructible != null)
                Log($"Linked destructible: {destructible} \n-> {destructible.GetDestructibleType()}", LogLevel.Warning);
        }


        #region Extensions
        public static string ToString(this DataManager.PinnedObject pinnedObject)
        {
            return $"PinnedObject: {pinnedObject.ZDOID} \n-> {pinnedObject.PinData}";
        }

        public static string ToString(this Minimap.PinData pinData)
        {
            return $"PinData: \n\t pos: {pinData.m_pos} \n\tlabel: {pinData.m_name} \n\ttype: {pinData.m_type} \n\ticon: {pinData.m_icon}";
        }

        public static void ToString(this Status status)
        {
            switch (status)
            {
                case Status.Success:
                    Log("Save/Load successful");
                    break;
                case Status.Failed:
                    Log("Save/Load failed", BepInEx.Logging.LogLevel.Warning);
                    break;
                case Status.NoData:
                    Log("No data to save/load", BepInEx.Logging.LogLevel.Warning);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
