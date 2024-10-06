using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ExperimentalEnemyInteractions;
    class MyModConfig
    {
        public readonly ConfigEntry<bool> debugBool;
        public readonly ConfigEntry<bool> enableSpider;
        public readonly ConfigEntry<bool> enableSlime;

        public MyModConfig(ConfigFile cfg)
        {
        cfg.SaveOnConfigSet = false;

            debugBool = cfg.Bind(
                "Debug",
                "Debug mode",
                true,
                "Enables debug mode for more debug logs."
                );
            enableSpider = cfg.Bind(
                "Entity settings",
                "Apply to spider",
                true,
                "Mod applies changes Bunker Spider"
                );
            enableSlime = cfg.Bind(
                "Entity settings",
                "Apply to slime",
                true,
                "Mod applies changes Hygrodere"
                );

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
