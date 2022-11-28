using HarmonyLib;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using static CharacterAnimEvent;

namespace QuickPing.Patches
{

    public struct HoverObject
    {
        public string name;
        public GameObject hover;
        public IDestructible destructible;
        public Vector3 pos;
        public Vector3 center;
        public HoverType type;
        public bool pinable;
    }
    /// <summary>
    /// Patch Player Class to add Ping Key and automatic pins
    /// </summary>
    static class Player_Patch
    {
        public static UnityEvent<HoverObject> OnPlayerPing = new UnityEvent<HoverObject>();

        /// <summary>
        /// Check for Key Input
        /// </summary>
        /// <param name="__instance">Local Player</param>
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {
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
                            HoverObject ping = FindHoverObject(500f);

                            //if (ping.hover != null)
                            //{
                            ping.name = GetHoverName(ping.name, ping.hover, ping.type);

                            //if (Settings.AddPin.Value)
                            //{
                            //    Minimap_Patch.AddPin(ping.hover, ping.name, ping.center);
                            //}
                            ping.name = Localization.instance.Localize(ping.name);
                            OnPlayerPing.Invoke(ping);
                            //}


                        }
            }
        }

        private static string GetHoverName(string pingText, GameObject hover, HoverType type)
        {
            Hoverable hoverable;
            Character hoverCreature;
            Piece piece;
            Location location;
            switch (type)
            {
                case HoverType.GameObject:
                    //pingText = Settings.pingText;
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
                case HoverType.Character:
                    hoverCreature = hover.GetComponent<Character>();
                    pingText = hoverCreature.m_name;

                    break;
            }
            return pingText;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hover"></param>
        /// <param name="hoverCreature"></param>
        /// <param name="pos"></param>
        /// <param name="center"></param>
        /// <param name="range"></param>
        public static HoverObject FindHoverObject(float range)
        {
            HoverObject hoverObj = new HoverObject()
            {
                type = HoverType.GameObject,
                pos = Player.m_localPlayer.GetHeadPoint(),
                center = Player.m_localPlayer.GetHeadPoint(),
                name = Settings.DefaultPingText
            };
            LayerMask m_interactMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "character_noenv", "vehicle");
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, range + 10, layerMask: m_interactMask);
            Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            RaycastHit[] array2 = array;
            //Out of range handler
            if (array2.Length == 0)
            {
                var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
                hoverObj.pos = ray.GetPoint(range);
                hoverObj.center = hoverObj.pos;
                return hoverObj;
            }
            //get closest actually
            for (int i = 0; i < 1; i++)
            {
                hoverObj.pos = array2[i].point;
                RaycastHit raycastHit = array2[i];
                if (OverlappingPlayer(ref raycastHit))
                {
                    continue;
                }


                //If ray hit in range
                if (Vector3.Distance(Player.m_localPlayer.GetEyePoint(), raycastHit.point) < range)
                {
                    Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());

                    if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                    {
                        hoverObj.type = HoverType.Character;
                        hoverObj.hover = character.gameObject;
                        hoverObj.center = character.transform.position;
                    }

                    else if ((hoverObj.type = CheckType(raycastHit.collider.transform, out hoverObj.destructible))
                        != HoverType.GameObject)
                    {
                        hoverObj.hover = raycastHit.collider.transform.gameObject;
                        hoverObj.center = hoverObj.hover.transform.position;
                        QuickPing.Log.LogWarning("Root");
                    }


                    //Children (might be useless)
                    else if (raycastHit.collider.GetComponentInChildren<GameObject>() != null)
                    {
                        var list = raycastHit.collider.GetComponentsInChildren<GameObject>();
                        foreach (var go in list)
                        {
                            if ((hoverObj.type = CheckType(go.transform, out hoverObj.destructible)) != HoverType.GameObject)
                            {
                                hoverObj.hover = go.transform.gameObject;
                                hoverObj.center = hoverObj.hover.transform.position;
                                QuickPing.Log.LogWarning("Child");
                            }
                        }

                    }
                    //Parents
                    else if (raycastHit.collider.transform.parent != null)
                    {
                        hoverObj.destructible = Utilities.GO_Ext.GetRecursiveComponentInParents<IDestructible>(raycastHit.collider.transform);
                        //TODO Check order
                        if (hoverObj.hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Location>(raycastHit.collider.transform))
                        {
                            hoverObj.type = HoverType.Location;
                        }
                        else if (hoverObj.hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Piece>(raycastHit.collider.transform))
                        {
                            hoverObj.type = HoverType.Piece;
                        }
                        else if (hoverObj.hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Hoverable>(raycastHit.collider.transform))
                        {
                            hoverObj.type = HoverType.Hoverable;
                        }
                    }
                }
                break;
            }

            if (hoverObj.hover)
            {
                hoverObj.center = hoverObj.hover.transform.position;

#if DEBUG
                //DEBUG

                switch (hoverObj.type)
                {
                    case HoverType.GameObject:
                        QuickPing.Log.LogWarning($"Ping ! : {hoverObj.hover} (GameObject)");
                        break;
                    case HoverType.Hoverable:
                        if (hoverObj.hover.TryGetComponent(out Hoverable hoverable))
                            QuickPing.Log.LogWarning($"Ping ! : {hoverable} (Hoverable) -> Name: {hoverable.GetHoverName()}");
                        break;
                    case HoverType.Piece:
                        if (hoverObj.hover.TryGetComponent(out Piece piece))
                            QuickPing.Log.LogWarning($"Ping ! : {piece} (Piece) -> Name: {piece.name} -> Trad: {Localization.instance.Localize(piece.name)}");
                        break;
                    case HoverType.Location:
                        if (hoverObj.hover.TryGetComponent(out Location location))
                            QuickPing.Log.LogWarning($"Ping ! : {location} (Location) -> Name: {location.name} -> Trad: {Localization.instance.Localize(location.name)}");

                        break;
                    case HoverType.Character:
                        if (hoverObj.hover.TryGetComponent(out Character hoverCreature))
                            QuickPing.Log.LogWarning($"Ping ! : {hoverCreature} (Character) ->  Name: {hoverCreature.m_name} -> Trad: {hoverCreature.GetHoverName()}");

                        break;

                }
                if (hoverObj.destructible != null)
                {
                    QuickPing.Log.LogWarning($"Ping ! : {hoverObj.destructible} (Destructible) -> Type: {hoverObj.destructible.GetDestructibleType()}");
                }
                //if (destructible != null)
                //{
                //    QuickPing.Log.LogWarning($"Found ${destructible}");
                //}
#endif
            }
            else
            {
                hoverObj.center = hoverObj.pos;
            }
            return hoverObj;
        }

        private static HoverType CheckType(Transform root, out IDestructible destructible)
        {
            //Test IDestructible
            destructible = root.GetComponent<IDestructible>();
            //Test Hoverable (Character)
            if (root.GetComponent<Hoverable>() != null)
            {
                return HoverType.Hoverable;
            }

            //Test IDestructible
            if (root.GetComponent<Location>() != null)
            {
                return HoverType.Location;
            }



            //Test Piece (check PieceType)
            if (root.GetComponent<Piece>())
            {
                return HoverType.Piece;

            }
            return HoverType.GameObject;
        }

        private static bool OverlappingPlayer(ref RaycastHit raycastHit)
        {
            return (bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == Player.m_localPlayer.gameObject;
        }




        public static void SendPing(HoverObject ping) => SendPing(ping.pos, ping.name);
        public static void SendPing(Vector3 position, string text, bool local = false)
        {

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer)
            {
                QuickPing.Log.LogInfo("SendPing : " + text);
                ZRoutedRpc.instance.InvokeRoutedRPC(local ? Player.m_localPlayer.GetZDOID().userID : ZRoutedRpc.Everybody, "ChatMessage", position, 3, localPlayer.GetPlayerName(), text, 1);
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", position.x, position.z));
                }
            }
        }









    }
}
