using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ExperimentalEnemyInteractions;
    class MyModConfig
    {
        public readonly ConfigEntry<bool> debugBool;
        public readonly ConfigEntry<bool> enableSpider;
        public readonly ConfigEntry<bool> spiderHuntHoardingbug;
        public readonly ConfigEntry<bool> enableSlime;
        public readonly ConfigEntry<bool> enableLeviathan;
        public readonly ConfigEntry<bool> enableSporeLizard;

    public MyModConfig(ConfigFile cfg)
        {
        cfg.SaveOnConfigSet = false;
        {
            debugBool = cfg.Bind(
                "Debug",
                "Debug mode",
                false,
                "Enables debug mode for more debug logs."
                );
            enableSpider = cfg.Bind(
                "Experimental",
                "Enable spider",
                false,
                "Mod applies changes Bunker Spider. Untested."
                );
            enableSlime = cfg.Bind(
                "Entity settings",
                "Enable slime",
                true,
                "Mod applies changes Hygrodere. Slime now damages every entity it passes by."
                );
            enableLeviathan = cfg.Bind(
                "Entity settings",
                "Enable leviathan",
                true,
                "Mod applies changes Earth leviathan. Leviathan now targets other creatures aswell."
                );
            enableSporeLizard = cfg.Bind(
                "WIP",
                "Enable SporeLizard",
                false,
                "Mod applies changes Earth leviathan. Leviathan now targets other creatures aswell."
                );
            spiderHuntHoardingbug = cfg.Bind(
                    "Experimental",
                    "Spider hunts Hoarding bugs",
                    false,
                    "Bunker spider chases and hunts hoarding bugs. Untested."
                    );
        }
        ClearOrphanedEntries(cfg);
        cfg.Save();
        cfg.SaveOnConfigSet = true;
        }
        public void ClearOrphanedEntries(ConfigFile cfg)
        {
        PropertyInfo orphanedEnriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEnriesProp.GetValue(cfg);
        orphanedEntries.Clear();
        }
    }
