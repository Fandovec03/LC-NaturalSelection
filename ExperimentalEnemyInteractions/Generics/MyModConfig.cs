using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ExperimentalEnemyInteractions.Generics;
    class MyModConfig
    {
        public readonly ConfigEntry<bool> stableMode;
        public readonly ConfigEntry<bool> debugBool;
        public readonly ConfigEntry<bool> enableSpider;
        public readonly ConfigEntry<bool> spiderHuntHoardingbug;
        public readonly ConfigEntry<bool> enableSlime;
        public readonly ConfigEntry<bool> enableLeviathan;
        public readonly ConfigEntry<bool> enableSporeLizard;
        public readonly ConfigEntry<bool> enableRedBees;
        public readonly ConfigEntry<bool> enableNutcrackers;

    //debug
        public readonly ConfigEntry<bool> debugRedBees;
        public readonly ConfigEntry<bool> debugSandworms;
        public readonly ConfigEntry<bool> debugHygrodere;
        public readonly ConfigEntry<bool> debugNutcrackers;
        public readonly ConfigEntry<bool> debugSpiders;
        public readonly ConfigEntry<bool> debugGiants;
        public readonly ConfigEntry<bool> debugUnspecified;
        public MyModConfig(ConfigFile cfg)
        {
        cfg.SaveOnConfigSet = false;
        {
            stableMode = cfg.Bind("General Settings","Toggle stable mode",true,"When true, the mod will exlude patches that are WIP or are experimental from loading");
            //enable entities
            enableSpider = cfg.Bind("WIP","Enable spider",false, "Mod applies changes Bunker Spider. DEV ONLY");
            enableSlime = cfg.Bind("Entity settings","Enable slime",true,"Mod applies changes Hygrodere. Slime now damages every entity it passes by.");
            enableLeviathan = cfg.Bind("Entity settings","Enable leviathan",true,"Mod applies changes Earth leviathan. Leviathan now targets other creatures aswell.");
            enableSporeLizard = cfg.Bind("WIP","Enable SporeLizard",false,"Mod applies changes Spore lizard. It is now mortal!");
            enableRedBees = cfg.Bind("Entity settings","Enable Red bees (Circuit bees)",true,"Mod applies changes red bees. They now defend nest from other mobs and kill everything in rampage!");
            enableNutcrackers = cfg.Bind("WIP", "Enable Nutcrackers", false, "Mod applies changes to nutcrackers. DEV ONLY");
            //entity settings
            spiderHuntHoardingbug = cfg.Bind("WIP", "Spider hunts Hoarding bugs", false, "Bunker spider chases and hunts hoarding bugs. DEV ONLY");
            //debug
            debugBool = cfg.Bind("Debug","Debug mode",false,"Enables debug mode for more debug logs.");
            debugRedBees = cfg.Bind("Debug","Log bees",false,"Enables logs for bees.");
            debugSandworms = cfg.Bind("Debug","Log sandworms",false,"Enables logs for sandowrms.");
            debugHygrodere = cfg.Bind("Debug","Log hydrogere",false,"Enables logs for hydrogere.");
            debugNutcrackers = cfg.Bind("Debug","Log nutcrackers",false,"Enables logs for nutcrackers.");
            debugSpiders = cfg.Bind("Debug","Log spiders",false,"Enables logs for spiders.");
            debugGiants = cfg.Bind("Debug", "Log giants", false, "Enables logs for giants.");
            debugUnspecified = cfg.Bind("Debug","Log unspecified",false,"Enables logs for unspecified.");
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
