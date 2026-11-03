#if DEBUG
using BepInEx;
using Jotunn.Entities;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuickPing.Utilities
{
    /// <summary>
    /// Debug-only console command: dumps locations, pingable prefabs and localization keys
    /// to BepInEx/QuickPingDump.txt, to keep the pinnable lists in sync with the game.
    /// Run it in a loaded world (F5 console): quickping_dump
    /// </summary>
    internal class DevDumpCommand : ConsoleCommand
    {
        public override string Name => "quickping_dump";

        public override string Help => "[QuickPing dev] Dump locations, pingable prefabs and localization keys";

        public override void Run(string[] args)
        {
            if (!ZNetScene.instance || !ZoneSystem.instance)
            {
                Console.instance.Print("quickping_dump: load a world first");
                return;
            }

            var sb = new StringBuilder();

            sb.AppendLine("## LOCATIONS\tprefab\tbiome\tenabled\tquantity\tgroup");
            foreach (var loc in ZoneSystem.instance.m_locations.OrderBy(l => l.m_prefabName))
                sb.AppendLine($"L\t{loc.m_prefabName}\t{loc.m_biome}\t{loc.m_enable}\t{loc.m_quantity}\t{loc.m_group}");

            sb.AppendLine("## PREFABS\tprefab\tcomponent\ttoken\tlocalized");
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(p => p).OrderBy(p => p.name))
            {
                foreach (Component c in prefab.GetComponents<Component>())
                {
                    string token = Token(c);
                    if (token == null) continue;
                    sb.AppendLine($"P\t{prefab.name}\t{c.GetType().Name}\t{token}\t{Localization.instance.Localize(token)}");
                }
            }

            sb.AppendLine("## TRANSLATIONS\tkey\tvalue");
            foreach (var kv in Localization.instance.m_translations.OrderBy(kv => kv.Key))
                sb.AppendLine($"T\t{kv.Key}\t{kv.Value.Replace('\n', ' ').Replace('\t', ' ')}");

            string path = Path.Combine(Paths.BepInExRootPath, "QuickPingDump.txt");
            File.WriteAllText(path, sb.ToString());
            Console.instance.Print($"quickping_dump: written to {path}");
        }

        private static string Token(Component c)
        {
            try
            {
                switch (c)
                {
                    case Pickable p:
                        return !string.IsNullOrEmpty(p.m_overrideName) ? p.m_overrideName
                            : p.m_itemPrefab ? p.m_itemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name : null;
                    case MineRock m: return m.m_name;
                    case MineRock5 m: return m.m_name;
                    case HoverText h: return h.m_text;
                    case Piece p: return p.m_name;
                    case Character ch: return ch.m_name;
                    case ItemDrop: return null; // items on the ground are never pinned
                    case Hoverable h: return h.GetHoverName();
                    default: return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
#endif
