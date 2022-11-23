//using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;

namespace QuickPing.Patches
{
    public static class Localization_Patch
    {
        /// <summary>
        /// Attribute translation to Location prefab
        /// </summary>
        /// <param name="location">GameObject Location component</param>
        /// <returns></returns>
        public static string Localize(Location location)
        {
            string res = "";
            foreach (var l in Location.m_allLocations)
            {
                if (location.name.Contains(l.name))
                {
                    string str = l.name;
                    if (l.name.Contains("(Clone)"))
                        str = l.name.Replace("(Clone)", "").Trim();
                    switch (str)
                    {
                        #region Blackforest
                        case "TrollCave":
                            res = Localization.instance.Localize("$location_forestcave");
                            break;
                        case "Crypt2":
                        case "Crypt3":
                        case "Crypt4":
                            res = Localization.instance.Localize("$location_forestcrypt");
                            break;
                        #endregion
                        #region Heath
                        case "GoblinCamp1":
                        case "GoblinCamp2":
                            res = Localization.instance.Localize("GoblinCamp");
                            break;
                        case "StoneHenge1":
                        case "StoneHenge2":
                        case "StoneHenge3":
                        case "StoneHenge4":
                        case "StoneHenge5":
                        case "StoneHenge6":
                            res = Localization.instance.Localize("Stone Henge");
                            break;
                        #endregion
                        #region Swamp
                        case "SunkenCrypt1":
                        case "SunkenCrypt2":
                        case "SunkenCrypt3":
                        case "SunkenCrypt4":
                        case "SunkenCrypt4old":
                            res = Localization.instance.Localize("$location_sunkencrypt");
                            break;
                        #endregion
                        #region Mountains
                        case "DrakeNest01":
                            res = Localization.instance.Localize("$item_dragonegg");
                            break;
                        case "MountainCave01":
                        case "MountainCave02":
                            res = Localization.instance.Localize("$location_mountaincave");
                            break;
                            #endregion
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// HEAVY AND NOT SAFE : MAY CHANGE  TODO
        /// </summary>
        /// <param name="str">Trnslated text</param>
        /// <returns></returns>
        public static string GetBaseTranslation(string str)
        {
            //QuickPing.Log.LogWarning($"Get base translation for -> {str}");
            if (str == "") return "noStr";
            Dictionary<string, string> values = Localization.instance.m_translations;
            var res = values.SingleOrDefault(x => x.Value == str).Key;

            //QuickPing.Log.LogWarning($"{str} -> ${res}");
            return "$" + res;
        }
    }
}
