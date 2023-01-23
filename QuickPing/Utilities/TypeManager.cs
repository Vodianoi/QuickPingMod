using System;
using System.Reflection;
using UnityEngine;

namespace QuickPing.Utilities
{
    internal class TypeManager
    {
        private static T GetRecursiveComponentInChildren<T>(Transform root)
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                component = GetRecursiveComponentInChildren<T>(root.GetChild(i));
                if (component != null)
                {
                    return component;
                }
            }

            return default;
        }

        public static GameObject GetRecursiveGameObjectInChildren<T>(Transform root)
        {
            if (root.GetComponent<T>() != null)
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var result = GetRecursiveGameObjectInChildren<T>(root.GetChild(i));
                if (result != null)
                {
                    return result;
                }
            }
            return default;
        }

        private static T GetRecursiveComponentInParents<T>(Transform root)
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            if (root.parent == null) return default;

            component = GetRecursiveComponentInParents<T>(root.parent);
            if (component != null)
            {
                return component;
            }
            else
                return default;
        }

        public static GameObject GetRecursiveGameObjectInParents<T>(Transform root)
        {
            if (root.GetComponent<T>() != null)
            {
                return root.gameObject;
            }

            if (root.parent == null) return null;

            var resultParent = GetRecursiveGameObjectInParents<T>(root.parent);
            if (resultParent != null)
            {
                return resultParent;
            }
            else
                return null;
        }

        private static bool OverlappingPlayer(ref RaycastHit raycastHit)
        {
            return (bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == Player.m_localPlayer.gameObject;
        }

        public static DataManager.PinnedObject GetPinnedObject(float range)
        {

            //Defaults
            DataManager.PinnedObject pinnedObject = new DataManager.PinnedObject();

            GameObject obj = null;
            IDestructible destructible = null;
            HoverType type = HoverType.GameObject;


            LayerMask m_interactMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "character_noenv", "vehicle", "item");
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, range + 10, layerMask: m_interactMask);
            Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            RaycastHit[] array2 = array;
            //Out of range handler
            if (array2.Length == 0)
            {
                var ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
                pinnedObject.PinData.m_pos = ray.GetPoint(range);
                return pinnedObject;
            }
            pinnedObject.PinData.m_pos = array2[0].point;
            RaycastHit raycastHit = array2[0];
            if (OverlappingPlayer(ref raycastHit))
            {
                return pinnedObject;
            }

            //If ray hit in range
            if (Vector3.Distance(Player.m_localPlayer.GetEyePoint(), raycastHit.point) < range)
            {
                Character character =
                    raycastHit.collider.attachedRigidbody
                    ? raycastHit.collider.attachedRigidbody.GetComponent<Character>()
                    : raycastHit.collider.GetComponent<Character>();
                //Root
                if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                {
                    type = HoverType.Character;
                    obj = character.gameObject;
                }
                else
                {

                    //Children
                    destructible ??= GetRecursiveComponentInChildren<IDestructible>(raycastHit.collider.transform);
                    destructible ??= GetRecursiveComponentInParents<IDestructible>(raycastHit.collider.transform);
                    if (obj = GetRecursiveGameObjectInChildren<Hoverable>(raycastHit.collider.transform))
                    {
                        type = HoverType.Hoverable;
                    }
                    else if (obj = GetRecursiveGameObjectInChildren<Piece>(raycastHit.collider.transform))
                    {
                        type = HoverType.Piece;
                    }
                    else if (obj = GetRecursiveGameObjectInChildren<Location>(raycastHit.collider.transform))
                    {
                        type = HoverType.Location;
                    }
                    //Parents
                    else if (obj = GetRecursiveGameObjectInParents<Hoverable>(raycastHit.collider.transform))
                    {
                        type = HoverType.Hoverable;
                    }
                    else if (obj = GetRecursiveGameObjectInParents<Piece>(raycastHit.collider.transform))
                    {
                        type = HoverType.Piece;
                    }
                    else if (obj = GetRecursiveGameObjectInParents<Location>(raycastHit.collider.transform))
                    {
                        type = HoverType.Location;
                    }
                }
            }

            if (obj)
            {
                pinnedObject.PinData.m_pos = obj.transform.position;

                Log(obj, type, destructible);
            }
            else
            {
                pinnedObject.PinData.m_pos = pinnedObject.PinData.m_pos;
            }

            if (destructible != null)
            {
                FieldInfo fieldInfo = destructible.GetType().GetField("m_nview", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo == null)
                {
                    QuickPingPlugin.Log.LogWarning($"Unable to link destructible {destructible} to pin: {pinnedObject.PinData.m_name}. (Is it a god?)");
                    pinnedObject.ZDOID = ZDOID.None;
                    return pinnedObject;
                }
                ZNetView netView = fieldInfo.GetValue(destructible) as ZNetView;
                pinnedObject.ZDOID = netView.GetZDO().m_uid;
            }
            pinnedObject.PinData.m_name = TextManager.GetHoverName(obj, type, pinnedObject.PinData.m_name);
            return pinnedObject;
        }

        private static void Log(GameObject obj, HoverType type, IDestructible destructible)
        {
#if DEBUG
            //DEBUG

            switch (type)
            {
                case HoverType.GameObject:
                    QuickPingPlugin.Log.LogWarning($"Ping ! : {obj} (GameObject)");
                    break;
                case HoverType.Hoverable:
                    if (obj.TryGetComponent(out Hoverable hoverable))
                        QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverable} (Hoverable) -> Name: {hoverable.GetHoverName()}");
                    break;
                case HoverType.Piece:
                    if (obj.TryGetComponent(out Piece piece))
                        QuickPingPlugin.Log.LogWarning($"Ping ! : {piece} (Piece) -> Name: {piece.name} -> Trad: {Localization.instance.Localize(piece.name)}");
                    break;
                case HoverType.Location:
                    if (obj.TryGetComponent(out Location location))
                        QuickPingPlugin.Log.LogWarning($"Ping ! : {location} (Location) -> Name: {location.name} -> Trad: {Localization.instance.Localize(location.name)}");

                    break;
                case HoverType.Character:
                    if (obj.TryGetComponent(out Character hoverCreature))
                        QuickPingPlugin.Log.LogWarning($"Ping ! : {hoverCreature} (Character) ->  Name: {hoverCreature.m_name} -> Trad: {hoverCreature.GetHoverName()}");

                    break;

            }
            if (destructible != null)
            {
                QuickPingPlugin.Log.LogWarning($"Ping ! : {destructible} (Destructible) -> Type: {destructible.GetDestructibleType()}");
            }
#endif
        }
    }



}
