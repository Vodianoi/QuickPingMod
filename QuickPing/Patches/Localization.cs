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
            { "Eikthyrnir", "$enemy_eikthyr" },

            // Black Forest
            { "TrollCave", "$location_forestcave" },
            { "TrollCave02", "$location_forestcave" },
            { "Crypt2", "$location_forestcrypt" },
            { "Crypt3", "$location_forestcrypt" },
            { "Crypt4", "$location_forestcrypt" },
            { "BearCave", "$location_bearcave" },
            { "Vendor_BlackForest", "Haldor" },
            { "GDKing", "$enemy_gdking" },
            { "Hildir_crypt", "$hud_pin_hildir1" },

            // Swamp
            { "SunkenCrypt1", "$location_sunkencrypt" },
            { "SunkenCrypt2", "$location_sunkencrypt" },
            { "SunkenCrypt3", "$location_sunkencrypt" },
            { "SunkenCrypt4", "$location_sunkencrypt" },
            { "BogWitch_Camp", "$npc_bogwitch" },
            { "Bonemass", "$enemy_bonemass" },

            // Mountains
            { "DrakeNest01", "$item_dragonegg" },
            { "Dragonqueen", "$enemy_dragon" },
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
            { "GoblinKing", "$enemy_goblinking" },

            // Mistlands
            { "Mistlands_DvergrTownEntrance1", "$location_dvergrtown" },
            { "Mistlands_DvergrTownEntrance2", "$location_dvergrtown" },
            { "Mistlands_DvergrBossEntrance1", "$location_dvergrboss" },

            // Ashlands
            { "MorgenHole1", "$location_morgenhole" },
            { "MorgenHole2", "$location_morgenhole" },
            { "MorgenHole3", "$location_morgenhole" },
            { "CharredFortress", "$charredfortress" },
            { "FaderLocation", "$enemy_fader" },

            // Deep North (other dungeons use the location's discover label)
            { "TheHole01", "$location_thehole" },
            { "TheDarkestHole", "$location_darkesthole" },

            // Structures without a game name (QuickPing tokens, see Translations)
            { "WoodVillage1", "$quickping_location_village" },
            { "WoodVillage2", "$quickping_location_village" },
            { "NorthVillage", "$quickping_location_village" },
            { "WoodFarm1", "$quickping_location_farm" },
            { "LumberCamp", "$quickping_location_lumbercamp" },
            { "Mistlands_Harbour1", "$quickping_location_harbour" },
            { "Mistlands_Lighthouse1_new", "$quickping_location_lighthouse" },
            { "WoodHouse1", "$quickping_location_house" },
            { "WoodHouse2", "$quickping_location_house" },
            { "WoodHouse3", "$quickping_location_house" },
            { "WoodHouse4", "$quickping_location_house" },
            { "WoodHouse5", "$quickping_location_house" },
            { "WoodHouse6", "$quickping_location_house" },
            { "WoodHouse7", "$quickping_location_house" },
            { "WoodHouse8", "$quickping_location_house" },
            { "WoodHouse9", "$quickping_location_house" },
            { "WoodHouse10", "$quickping_location_house" },
            { "WoodHouse11", "$quickping_location_house" },
            { "WoodHouse12", "$quickping_location_house" },
            { "WoodHouse13", "$quickping_location_house" },
            { "StoneHouse3", "$quickping_location_house" },
            { "StoneHouse4", "$quickping_location_house" },
            { "StoneHouse1_heath", "$quickping_location_house" },
            { "StoneHouse2_heath", "$quickping_location_house" },
            { "StoneHouse5_heath", "$quickping_location_house" },
            { "AbandonedLogCabin02", "$quickping_location_house" },
            { "AbandonedLogCabin03", "$quickping_location_house" },
            { "AbandonedLogCabin04", "$quickping_location_house" },
            { "DN_hut01", "$quickping_location_house" },
            { "SwampHut1", "$quickping_location_swamphut" },
            { "SwampHut1_1", "$quickping_location_swamphut" },
            { "SwampHut2", "$quickping_location_swamphut" },
            { "SwampHut2_1", "$quickping_location_swamphut" },
            { "SwampHut3", "$quickping_location_swamphut" },
            { "SwampHut3_1", "$quickping_location_swamphut" },
            { "SwampHut4", "$quickping_location_swamphut" },
            { "SwampHut5", "$quickping_location_swamphut" },
            { "GoblinHut01", "GoblinCamp" },
            { "GoblinHut02", "GoblinCamp" },
            { "GoblinHut03", "GoblinCamp" },
            { "StoneTower1", "$quickping_location_tower" },
            { "StoneTower3", "$quickping_location_tower" },
            { "Mistlands_GuardTower1_new", "$quickping_location_tower" },
            { "Mistlands_GuardTower2_new", "$quickping_location_tower" },
            { "Mistlands_GuardTower3_new", "$quickping_location_tower" },
            { "Ruin1", "$quickping_location_ruins" },
            { "Ruin2", "$quickping_location_ruins" },
            { "Ruin3", "$quickping_location_ruins" },
            { "CombatRuin01", "$quickping_location_ruins" },
            { "SwampRuin1", "$quickping_location_ruins" },
            { "SwampRuin2", "$quickping_location_ruins" },
            { "StoneTowerRuins03", "$quickping_location_ruins" },
            { "StoneTowerRuins04", "$quickping_location_ruins" },
            { "StoneTowerRuins05", "$quickping_location_ruins" },
            { "StoneTowerRuins05_leet", "$quickping_location_ruins" },
            { "StoneTowerRuins07", "$quickping_location_ruins" },
            { "StoneTowerRuins07_sunk", "$quickping_location_ruins" },
            { "StoneTowerRuins08", "$quickping_location_ruins" },
            { "StoneTowerRuins08_sunk", "$quickping_location_ruins" },
            { "StoneTowerRuins09", "$quickping_location_ruins" },
            { "StoneTowerRuins09_sunk", "$quickping_location_ruins" },
            { "StoneTowerRuins10", "$quickping_location_ruins" },
            { "StoneTowerRuins10_sunk", "$quickping_location_ruins" },
            { "Mistlands_GuardTower1_ruined_new", "$quickping_location_ruins" },
            { "Mistlands_GuardTower1_ruined_new2", "$quickping_location_ruins" },
            { "Mistlands_GuardTower3_ruined_new", "$quickping_location_ruins" },
            { "AshlandRuins", "$quickping_location_ruins" },
            { "FortressRuins", "$quickping_location_ruins" },
            { "CharredRuins1", "$quickping_location_ruins" },
            { "CharredRuins2", "$quickping_location_ruins" },
            { "CharredRuins3", "$quickping_location_ruins" },
            { "CharredRuins4", "$quickping_location_ruins" },
            { "CharredTowerRuins1", "$quickping_location_ruins" },
            { "CharredTowerRuins1_dvergr", "$quickping_location_ruins" },
            { "CharredTowerRuins2", "$quickping_location_ruins" },
            { "CharredTowerRuins3", "$quickping_location_ruins" },
            { "ShipWreck01", "$quickping_location_shipwreck" },
            { "ShipWreck02", "$quickping_location_shipwreck" },
            { "ShipWreck03", "$quickping_location_shipwreck" },
            { "ShipWreck04", "$quickping_location_shipwreck" },
            { "ShipWreck01_DN", "$quickping_location_shipwreck" },
            { "ShipWreck02_DN", "$quickping_location_shipwreck" },
            { "FrozenShip01_DN", "$quickping_location_shipwreck" },
            { "FrozenShip02_DN", "$quickping_location_shipwreck" },
            { "FrozenShip03_DN", "$quickping_location_shipwreck" },
        };

        /// <summary>
        /// QuickPing's own names, for structures the game doesn't name (registered through Jotunn).
        /// </summary>
        private static readonly Dictionary<string, string> Translations = new()
        {
            { "quickping_location_village", "Village" },
            { "quickping_location_farm", "Farm" },
            { "quickping_location_lumbercamp", "Lumber Camp" },
            { "quickping_location_harbour", "Harbour" },
            { "quickping_location_lighthouse", "Lighthouse" },
            { "quickping_location_house", "Abandoned House" },
            { "quickping_location_swamphut", "Swamp Hut" },
            { "quickping_location_tower", "Tower" },
            { "quickping_location_ruins", "Ruins" },
            { "quickping_location_shipwreck", "Shipwreck" },
            { "quickping_leviathan", "Leviathan" },
        };

        public static void AddTranslations()
        {
            Jotunn.Managers.LocalizationManager.Instance.GetLocalization().AddTranslation("English", Translations);
        }

        /// <summary>
        /// Localized name of a location: known prefabs first, then the label the game shows when discovering it.
        /// </summary>
        /// <param name="location">GameObject Location component</param>
        /// <returns>Localized name, or "" if unknown</returns>
        public static string Localize(Location location)
        {
            string token = GetToken(location);
            return token == "" ? "" : Localization.instance.Localize(token);
        }

        /// <summary>
        /// Name token of a location (e.g. "$location_forestcave"): known prefabs first, then its discover label.
        /// Pings use the token, so pin types never depend on a reverse translation lookup.
        /// </summary>
        /// <param name="location">GameObject Location component</param>
        /// <returns>Name token (or literal name), or "" if unknown</returns>
        public static string GetToken(Location location)
        {
            string prefabName = location.name.Replace("(Clone)", "").Trim();
            if (LocationNames.TryGetValue(prefabName, out string token))
                return token;

            return location.m_discoverLabel ?? "";
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
