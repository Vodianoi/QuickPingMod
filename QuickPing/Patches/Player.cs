using HarmonyLib;
using System;
using UnityEngine;

namespace QuickPing.Patches
{
    /// <summary>
    /// Patch Player Class to add Ping Key and automatic pins
    /// </summary>
    class Player_Patch
    {
        /// <summary>
        /// Check for Key Input
        /// </summary>
        /// <param name="__instance">Local Player</param>
        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {
            string pingText = Settings.pingText;
            if (Player.m_localPlayer == __instance)
            {
                if (!Settings.PingWhereLooking.Value)
                {
                    return;
                }
                if (Player.m_localPlayer && ZInput.instance != null)
                    if (Settings.PingKey.Value != KeyCode.None)
                        if (ZInput.GetButtonDown(Settings.pingBtn.Name))
                        {
                            FindHoverObject(
                                out GameObject hover,
                                out Character hoverCreature,
                                out HoverType type,
                                out Vector3 pos,
                                out Vector3 center,
                                500f);

                            bool pinClose = false;
                            if (hover != null || hoverCreature != null)
                            {

                                if (hoverCreature != null)
                                {
                                    if (hoverCreature.m_level != 1)
                                        pingText = $"{hoverCreature.GetHoverName()} {Localization.instance.Localize("$msg_level")} : {hoverCreature.m_level}";
                                    else
                                        pingText = hoverCreature.GetHoverName();
                                }
                                else if (hover != null)
                                {
                                    pingText = GetHoverName(pingText, hover, type);
                                }

                                if (Settings.AddPin.Value)
                                {
                                    Minimap_Patch.AddPin(__instance, hover, pingText, center, out pinClose);
                                }
                                pingText = Localization.instance.Localize(pingText);
                            }
                            SendPing(pos, pingText, pinClose);


                        }
            }
        }

        private static string GetHoverName(string pingText, GameObject hover, HoverType type)
        {
            Hoverable hoverable;
            Piece piece;
            Location location;
            switch (type)
            {
                case HoverType.GameObject:
                    pingText = hover.name;
                    break;
                case HoverType.Hoverable:
                    hover.TryGetComponent(out hoverable);
                    pingText = !hoverable.GetHoverName().StartsWith("$")
                        ? Localization_Patch.GetBaseTranslation(hoverable.GetHoverName())
                        : hoverable.GetHoverName();

                    if (pingText == "noStr")
                        pingText = hoverable.GetHoverName();
                    //Check furnace & map (maybe others?)
                    if ((hoverable.ToString().Contains("ReadMap")
                        || hoverable.ToString().Contains("WriteMap")
                        || hoverable.ToString().Contains("add_ore")
                        || hoverable.ToString().Contains("add_wood"))
                        || hoverable.ToString().Contains("door")
                        && hover.transform.parent && hover.transform.GetComponentInParent<Piece>())
                        pingText = hover.GetComponentInParent<Piece>().m_name;
                    break;
                case HoverType.Piece:
                    piece = hover.GetComponent<Piece>();
                    pingText = piece.m_name;
                    break;
                case HoverType.Location:
                    location = hover.GetComponent<Location>();
                    pingText = Localization_Patch.Localize(location);
                    break;
            }

            return pingText;
        }

        public enum HoverType
        {
            GameObject,
            Hoverable,
            Piece,
            Location
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hover"></param>
        /// <param name="hoverCreature"></param>
        /// <param name="pos"></param>
        /// <param name="center"></param>
        /// <param name="range"></param>
        public static void FindHoverObject(out GameObject hover, out Character hoverCreature, out HoverType type, out Vector3 pos, out Vector3 center, float range)
        {

            type = HoverType.GameObject;
            hover = null;
            hoverCreature = null;
            pos = Player.m_localPlayer.GetHeadPoint();
            center = pos;
            LayerMask m_interactMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "character_noenv", "vehicle");
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, range + 10, layerMask: m_interactMask);
            Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            RaycastHit[] array2 = array;
            //Out of range handler
            if (array2.Length == 0)
            {
                var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
                pos = ray.GetPoint(range);
                center = pos;
            }
            //get closest actually
            for (int i = 0; i < array2.Length; i++)
            {
                pos = array2[i].point;
                RaycastHit raycastHit = array2[i];
                if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == Player.m_localPlayer.gameObject)
                {
                    continue;
                }

                if (hoverCreature == null)
                {
                    Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());

                    if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                    {
                        hoverCreature = character;
                        center = hoverCreature.transform.position;
                    }
                }

                if (Vector3.Distance(Player.m_localPlayer.GetEyePoint(), raycastHit.point) < range)
                {

                    //Collider
                    if (raycastHit.collider.GetComponent<Piece>())
                    {
                        hover = raycastHit.collider.gameObject;
                        type = HoverType.Piece;
                    }
                    else if (raycastHit.collider.GetComponent<Hoverable>() != null)
                    {

                        hover = raycastHit.collider.gameObject;
                        type = HoverType.Hoverable;
                    }
                    else if ((bool)raycastHit.collider.attachedRigidbody)
                    {
                        hover = raycastHit.collider.attachedRigidbody.gameObject;
                    }
                    //Children
                    else if (raycastHit.collider.GetComponentInChildren<GameObject>() != null)
                    {
                        var list = raycastHit.collider.GetComponentsInChildren<GameObject>();
                        foreach (var go in list)
                        {
                            if (go.TryGetComponent(out Piece _))
                            {
                                type = HoverType.Piece;
                                hover = go;
                            }
                            else if (go.TryGetComponent(out Hoverable _))
                            {
                                type = HoverType.Hoverable;
                                hover = go;
                            }

                            if (hover && type == HoverType.Piece)
                                break;
                        }

                    }
                    //Parents
                    else if (raycastHit.collider.transform.parent != null)
                    {

                        if ((hover = GetRecursiveParentWithComponent<Location>(raycastHit.collider.transform)) != null)
                        {
                            type = HoverType.Location;
                        }
                        else if ((hover = GetRecursiveParentWithComponent<Piece>(raycastHit.collider.transform)) != null)
                        {
                            type = HoverType.Piece;
                        }
                        else if ((hover = GetRecursiveParentWithComponent<Hoverable>(raycastHit.collider.transform)) != null)
                        {
                            type = HoverType.Hoverable;
                        }
                    }
                }
                break;
            }

            if (hover)
            {
                center = hover.transform.position;

#if DEBUG
                //DEBUG
                if (hoverCreature)
                    QuickPing.Log.LogWarning($"Ping ! : {hoverCreature} (Character) ->  Name: {hoverCreature.m_name} -> Trad: {hoverCreature.GetHoverName()}");
                switch (type)
                {
                    case HoverType.GameObject:
                        QuickPing.Log.LogWarning($"Ping ! : {hover} (GameObject)");
                        break;
                    case HoverType.Hoverable:
                        if (hover.TryGetComponent(out Hoverable hoverable))
                            QuickPing.Log.LogWarning($"Ping ! : {hoverable} (Hoverable) -> Name: {hoverable.GetHoverName()}");
                        break;
                    case HoverType.Piece:
                        if (hover.TryGetComponent(out Piece piece))
                            QuickPing.Log.LogWarning($"Ping ! : {piece} (Piece) -> Name: {piece.name} -> Trad: {Localization.instance.Localize(piece.name)}");
                        break;
                    case HoverType.Location:
                        if (hover.TryGetComponent(out Location location))
                            QuickPing.Log.LogWarning($"Ping ! : {location} (Location) -> Name: {location.name} -> Trad: {Localization.instance.Localize(location.name)}");

                        break;
                }
#endif
            }
        }

        public static T GetRecursiveComponentInParents<T>(Transform root)
        {

            if (root.parent == null) return default;
            var parent = root.parent;
            if (parent.gameObject.TryGetComponent(out T res))
                return res;
            else
                return GetRecursiveComponentInParents<T>(parent);
        }

        public static GameObject GetRecursiveParentWithComponent<T>(Transform root)
        {
            if (!root.parent) return null;
            if (root.parent.gameObject.TryGetComponent(out T _))
            {
                return root.parent.gameObject;
            }
            else
                return GetRecursiveParentWithComponent<T>(root.parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="text"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        private static bool SendPing(Vector3 position, string text, bool local = false)
        {

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer)
            {
                Vector3 vector = position;
                //vector.y = localPlayer.transform.position.y;
                QuickPing.Log.LogInfo("SendPing : " + text);
                ZRoutedRpc.instance.InvokeRoutedRPC(local ? Player.m_localPlayer.GetZDOID().userID : ZRoutedRpc.Everybody, "ChatMessage", vector, 3, localPlayer.GetPlayerName(), text, 1);
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", vector.x, vector.z));
                }
            }
            return false;
        }









    }
}
