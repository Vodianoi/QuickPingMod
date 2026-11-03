using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace QuickPing.Patches
{

    #region Patches
    /// <summary>
    /// Patch Player Class to add Ping Key and automatic pins
    /// </summary>
    static class Player_Patch
    {
        public static UnityEvent<HoverObject> OnPlayerPing = new UnityEvent<HoverObject>();
        public static UnityEvent<HoverObject> OnPlayerForcePing = new UnityEvent<HoverObject>();
        public static UnityEvent<HoverObject> OnPlayerRename = new UnityEvent<HoverObject>();


        /// <summary>
        /// Check for Key Input
        /// </summary>
        /// <param name="__instance">Local Player</param>
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        [HarmonyPostfix]
        private static void Player_Update(Player __instance)
        {
            //Check ZInput instance 
            if (ZInput.instance == null) return;
            //Check instance
            if (!Player.m_localPlayer || Player.m_localPlayer != __instance) return;
            //Check mod settings
            if (!Settings.PingWhereLooking.Value && !Settings.AddPin.Value) return;

            if (Minimap_Patch.IsNaming)
            {
                Minimap_Patch.UpdateNameInput();
            }

            //Check if player can use input (fix #34)
            if (!Player.m_localPlayer.TakeInput()) return;

            //Check Keys
            if (Settings.PingKey.Value != KeyCode.None)
            {
                if (ZInput.GetButtonDown(Settings.RenameBtn.Name))
                {
                    HoverObject ping = FindHoverObject(500f);

                    ping.Name = GetHoverName(ping.Name, ping.Hover, ping.type);

                    OnPlayerRename.Invoke(ping);
                }
                else
                if (ZInput.GetButtonDown(Settings.PingBtn.Name))
                {
                    HoverObject ping = FindHoverObject(500f);

                    ping.Name = GetHoverName(ping.Name, ping.Hover, ping.type);

                    OnPlayerPing.Invoke(ping);
                }
                else
                if (ZInput.GetButtonDown(Settings.PingEverythingBtn.Name))
                {
                    HoverObject ping = FindHoverObject(500f);

                    ping.Name = GetHoverName(ping.Name, ping.Hover, ping.type);

                    OnPlayerForcePing.Invoke(ping);
                }

            }
        }

        #endregion
        private static string GetHoverName(string pingText, GameObject hover, HoverType type)
        {
            switch (type)
            {
                case HoverType.GameObject:
                    //pingText = Settings.pingText;
                    break;
                case HoverType.Hoverable:
                    hover.TryGetComponent(out Hoverable hoverable);
                    pingText = !hoverable.GetHoverName().StartsWith("$")
                        ? Localization_Patch.GetBaseTranslation(hoverable.GetHoverName())
                        : hoverable.GetHoverName();

                    if (pingText == "$")
                        pingText = hoverable.GetHoverName();


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
                    var piece = hover.GetComponent<Piece>();
                    pingText = piece.m_name;
                    break;
                case HoverType.Location:
                    var location = hover.GetComponent<Location>();
                    pingText = Localization_Patch.Localize(location);
                    break;
                case HoverType.Character:
                    var hoverCreature = hover.GetComponent<Character>();
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
                Name = Settings.DefaultPingText
            };
            LayerMask m_interactMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "character_noenv", "vehicle", "item");
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, range + 10, layerMask: m_interactMask);
            Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            // Closest hit that isn't the local player
            int hitIndex = Array.FindIndex(array, hit => !OverlappingPlayer(hit));

            //Out of range handler
            if (hitIndex < 0)
            {
                var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
                hoverObj.pos = ray.GetPoint(range);
                hoverObj.center = hoverObj.pos;
                return hoverObj;
            }

            RaycastHit raycastHit = array[hitIndex];
            hoverObj.pos = raycastHit.point;

            //If ray hit in range
            if (Vector3.Distance(Player.m_localPlayer.GetEyePoint(), raycastHit.point) < range)
            {
                Transform hitTransform = raycastHit.collider.transform;
                Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());

                if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                {
                    hoverObj.type = HoverType.Character;
                    hoverObj.Hover = character.gameObject;
                    hoverObj.center = character.transform.position;
                }
                else if ((hoverObj.type = CheckType(hitTransform, out hoverObj.Destructible)) != HoverType.GameObject)
                {
                    hoverObj.Hover = hitTransform.gameObject;
                    hoverObj.center = hoverObj.Hover.transform.position;
                }
                //Children, then parents
                else if (!TryFindInChildren(hitTransform, hoverObj) && hitTransform.parent != null)
                {
                    hoverObj.Destructible = Utilities.GO_Ext.GetRecursiveComponentInParents<IDestructible>(hitTransform);
                    //TODO Check order
                    if (hoverObj.Hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Location>(hitTransform))
                    {
                        hoverObj.type = HoverType.Location;
                    }
                    else if (hoverObj.Hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Piece>(hitTransform))
                    {
                        hoverObj.type = HoverType.Piece;
                    }
                    else if (hoverObj.Hover = Utilities.GO_Ext.GetRecursiveParentWithComponent<Hoverable>(hitTransform))
                    {
                        hoverObj.type = HoverType.Hoverable;
                    }
                }

                //Structures (villages, ruins, dungeons...): ping the whole location instead of the piece
                TryUseLocation(hoverObj, hitTransform, raycastHit.point);
            }

            if (hoverObj.Hover)
            {
                hoverObj.center = hoverObj.Hover.transform.position;

#if DEBUG
                //DEBUG

                switch (hoverObj.type)
                {
                    case HoverType.GameObject:
                        QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverObj.Hover} (GameObject)");
                        break;
                    case HoverType.Hoverable:
                        if (hoverObj.Hover.TryGetComponent(out Hoverable hoverable))
                            QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverable} (Hoverable) -> Name: {hoverable.GetHoverName()}");
                        break;
                    case HoverType.Piece:
                        if (hoverObj.Hover.TryGetComponent(out Piece piece))
                            QuickPingPlugin.Log.LogWarning($"Ping ! : {piece} (Piece) -> Name: {piece.name} -> Trad: {Localization.instance.Localize(piece.name)}");
                        break;
                    case HoverType.Location:
                        if (hoverObj.Hover.TryGetComponent(out Location location))
                            QuickPingPlugin.Log.LogWarning($"Ping ! : {location} (Location) -> Name: {location.name} -> Trad: {Localization.instance.Localize(location.name)}");

                        break;
                    case HoverType.Character:
                        if (hoverObj.Hover.TryGetComponent(out Character hoverCreature))
                            QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverCreature} (Character) ->  Name: {hoverCreature.m_name} -> Trad: {hoverCreature.GetHoverName()}");

                        break;

                }
                if (hoverObj.Destructible != null)
                {
                    QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverObj.Destructible} (Destructible) -> Type: {hoverObj.Destructible.GetDestructibleType()}");
                }
#endif
            }
            else
            {
                hoverObj.center = hoverObj.pos;
            }
            return hoverObj;
        }

        // Check if Interface is type of HoverType
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

        /// <summary>
        /// If the hit object is part of a named location (village, ruin, dungeon entrance...), target the location.
        /// Networked location parts (walls, doors, chests...) are spawned without a parent, so the location is
        /// also looked up by position. Player-built pieces and objects worth pinning on their own
        /// (ores, berries, traders...) are kept.
        /// </summary>
        private static void TryUseLocation(HoverObject hoverObj, Transform hitTransform, Vector3 point)
        {
            if (hoverObj.type == HoverType.Character) return;

            Location parentLocation = hitTransform.GetComponentInParent<Location>();
            Piece piece = hitTransform.GetComponentInParent<Piece>();
            bool structurePart = parentLocation
                || (piece ? !piece.IsPlacedByPlayer() : hitTransform.GetComponentInParent<WearNTear>());
            if (!structurePart) return;

            if (hoverObj.Hover && hoverObj.type != HoverType.Location)
            {
                string ownName;
                try
                {
                    ownName = GetHoverName(hoverObj.Name, hoverObj.Hover, hoverObj.type);
                }
                catch (Exception)
                {
                    ownName = null;
                }
                if (ownName != null && Minimap_Patch.IsPinable(ownName) != Minimap.PinType.None) return;
            }

            Location location = parentLocation ? parentLocation : Location.GetLocation(point);
            if (!location || Localization_Patch.Localize(location) == "") return;

            hoverObj.type = HoverType.Location;
            hoverObj.Hover = location.gameObject;
            hoverObj.Destructible = null;
        }

        /// <summary>
        /// Look for a pingable component on the children of <paramref name="root"/> (root excluded).
        /// </summary>
        private static bool TryFindInChildren(Transform root, HoverObject hoverObj)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                if (child == root) continue;
                if ((hoverObj.type = CheckType(child, out hoverObj.Destructible)) != HoverType.GameObject)
                {
                    hoverObj.Hover = child.gameObject;
                    hoverObj.center = child.position;
                    return true;
                }
            }
            return false;
        }

        private static bool OverlappingPlayer(RaycastHit raycastHit)
        {
            return (bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == Player.m_localPlayer.gameObject;
        }

        internal static void SendRename(HoverObject ping) => SendPing(ping);
        public static void SendPing(HoverObject ping) => SendPing(ping.pos, Localization.instance.Localize(ping.Name));
        internal static void SendPing(Vector3 position, string text, bool local = false)
        {

            Player localPlayer = Player.m_localPlayer;
            if ((bool)localPlayer && Settings.PingWhereLooking.Value)
            {
                QuickPingPlugin.Log.LogInfo("SendPing : " + text);
                ZRoutedRpc.instance.InvokeRoutedRPC(local ? Player.m_localPlayer.GetZDOID().UserID : ZRoutedRpc.Everybody, "ChatMessage", position, 3, UserInfo.GetLocalUser(), text);
                if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled())
                {
                    Console.instance.AddString(string.Format("Pinged at: {0}, {1}", position.x, position.z));
                }
            }
        }
    }
}
