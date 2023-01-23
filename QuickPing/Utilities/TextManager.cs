using QuickPing.Patches;
using UnityEngine;

namespace QuickPing.Utilities
{
    internal class TextManager
    {

        public static readonly TextManager Instance;

        public string PingText { get; set; }

        // TODO: Add more text fields here

        static TextManager()
        {
            Instance = new TextManager();
        }

        private TextManager()
        {
            PingText = "PING !";
        }

        public static string GetHoverName(GameObject hover, HoverType type, string pingText)
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

                    if (pingText == "$")
                        pingText = hoverable.GetHoverName();


                    if (pingText == "noStr")
                        pingText = hoverable.GetHoverName();
                    //Check furnace & map (maybe others?)
                    if (IsPieceComponent(hover, hoverable))
                        pingText = hover.GetComponentInParent<Piece>().m_name;

                    if (pingText == "$piece_portal")
                    {
                        pingText += ":" + GetPortalTag(hoverable);
                    }
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

        private static bool IsPieceComponent(GameObject hover, Hoverable hoverable)
        {
            return hoverable.ToString().Contains("ReadMap")
                                    || hoverable.ToString().Contains("WriteMap")
                                    || hoverable.ToString().Contains("add_ore")
                                    || hoverable.ToString().Contains("add_wood")
                                    || hoverable.ToString().Contains("door")
                                    && hover.transform.parent && hover.transform.GetComponentInParent<Piece>();
        }

        internal static string GetPortalTag(Hoverable hoverable)
        {
            string portalText = hoverable.GetHoverText();
            string tag = portalText.Split('"')[1];
            return tag;
        }



    }
}
