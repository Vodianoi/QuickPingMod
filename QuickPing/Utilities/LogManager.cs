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

        private static bool CanLog(LogLevel level)
        {
            return (level <= Settings.LogLevel.Value) && Settings.Logging.Value;
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!CanLog(level))
                return;
            switch (level)
            {
                case LogLevel.None:
                    break;
                case LogLevel.Fatal:
                    Logger.LogError(message);
                    break;
                case LogLevel.Error:
                    Logger.LogError(message);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(message);
                    break;
                case LogLevel.Message:
                    Logger.LogMessage(message);
                    break;
                case LogLevel.Info:
                    Logger.LogInfo(message);
                    break;
                case LogLevel.Debug:
                    Logger.LogDebug(message);
                    break;
                case LogLevel.All:
                    Logger.LogInfo(message);
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

        public static string ToString(this Status status)
        {
            switch (status)
            {
                case Status.Success:
                    return "Save/Load successful";
                case Status.Failed:
                    return "Save/Load failed";
                case Status.NoData:
                    return "No data to save/load";
                default:
                    return "Unknown status";
            }
        }

        #endregion
    }
}
