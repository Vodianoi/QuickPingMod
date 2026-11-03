using System.Collections.Generic;
using System.Linq;

namespace QuickPing.Patches
{
    public static class Localization_Patch
    {
        /// <summary>
        /// Location prefab name -> name token (or literal name) used for pings and pins.
        /// Prefab names come from ZoneSystem.m_locations (Valheim 1.0.21, see the quickping_dump dev command).
        /// </summary>
        private static readonly Dictionary<string, string> LocationNames = new()
        {
            // Meadows
            { "Hildir_camp", "Hildir" },

            // Black Forest
            { "TrollCave", "$location_forestcave" },
            { "TrollCave02", "$location_forestcave" },
            { "Crypt2", "$location_forestcrypt" },
            { "Crypt3", "$location_forestcrypt" },
            { "Crypt4", "$location_forestcrypt" },
            { "BearCave", "$location_bearcave" },
            { "Vendor_BlackForest", "Haldor" },
            { "Hildir_crypt", "$hud_pin_hildir1" },

            // Swamp
            { "SunkenCrypt1", "$location_sunkencrypt" },
            { "SunkenCrypt2", "$location_sunkencrypt" },
            { "SunkenCrypt3", "$location_sunkencrypt" },
            { "SunkenCrypt4", "$location_sunkencrypt" },
            { "BogWitch_Camp", "$npc_bogwitch" },

            // Mountains
            { "DrakeNest01", "$item_dragonegg" },
            { "MountainCave01", "$location_mountaincave" },
            { "MountainCave02", "$location_mountaincave" },
            { "Hildir_cave", "$hud_pin_hildir2" },

            // Plains
            { "GoblinCamp1", "GoblinCamp" },
            { "GoblinCamp2", "GoblinCamp" },
            { "GoblinCamp2_1", "GoblinCamp" },
            { "StoneHenge1", "Stone Henge" },
            { "StoneHenge2", "Stone Henge" },
            { "StoneHenge3", "Stone Henge" },
            { "StoneHenge4", "Stone Henge" },
            { "StoneHenge5", "Stone Henge" },
            { "StoneHenge6", "Stone Henge" },
            { "Hildir_plainsfortress", "$hud_pin_hildir3" },

            // Mistlands
            { "Mistlands_DvergrTownEntrance1", "$location_dvergrtown" },
            { "Mistlands_DvergrTownEntrance2", "$location_dvergrtown" },
            { "Mistlands_DvergrBossEntrance1", "$location_dvergrboss" },

            // Ashlands
            { "MorgenHole1", "$location_morgenhole" },
            { "MorgenHole2", "$location_morgenhole" },
            { "MorgenHole3", "$location_morgenhole" },
            { "CharredFortress", "$charredfortress" },

            // Deep North (other dungeons use the location's discover label)
            { "TheHole01", "$location_thehole" },
            { "TheDarkestHole", "$location_darkesthole" },
        };

        /// <summary>
        /// Localized name of a location: known prefabs first, then the label the game shows when discovering it.
        /// </summary>
        /// <param name="location">GameObject Location component</param>
        /// <returns>Localized name, or "" if unknown</returns>
        public static string Localize(Location location)
        {
            string prefabName = location.name.Replace("(Clone)", "").Trim();
            if (LocationNames.TryGetValue(prefabName, out string token))
                return Localization.instance.Localize(token);

            if (!string.IsNullOrEmpty(location.m_discoverLabel))
                return Localization.instance.Localize(location.m_discoverLabel);

            return "";
        }

        /// <summary>
        /// HEAVY AND NOT SAFE : MAY CHANGE  TODO
        /// </summary>
        /// <param name="str">Translated text</param>
        /// <returns></returns>
        public static string GetBaseTranslation(string str)
        {
            if (str == "") return "noStr";
            Dictionary<string, string> values = Localization.instance.m_translations;
            var res = values.FirstOrDefault(x => x.Value == str).Key;

            return "$" + res;
        }
    }
}
