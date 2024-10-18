using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ExperimentalEnemyInteractions;
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


        //debug
        public readonly ConfigEntry<bool> debugRedBees;
        public readonly ConfigEntry<bool> debugSandworms;
        public readonly ConfigEntry<bool> debugHygrodere;
        public readonly ConfigEntry<bool> debugNutcrackers;
        public readonly ConfigEntry<bool> debugSpiders;
        public readonly ConfigEntry<bool> debugUnspecified;
    public MyModConfig(ConfigFile cfg)
        {
        cfg.SaveOnConfigSet = false;
        {
            stableMode = cfg.Bind("WIP","Toggle stable mode",true,"When true, the mod will exlude patches that are being worked or are in WIP from loading");

            enableSpider = cfg.Bind("Experimental","Enable spider",false,"Mod applies changes Bunker Spider. Currently bugged with SpiderPositionFix.");
            enableSlime = cfg.Bind("Entity settings","Enable slime",true,"Mod applies changes Hygrodere. Slime now damages every entity it passes by.");
            enableLeviathan = cfg.Bind("Entity settings","Enable leviathan",true,"Mod applies changes Earth leviathan. Leviathan now targets other creatures aswell.");
            enableSporeLizard = cfg.Bind("WIP","Enable SporeLizard",false,"Mod applies changes Spore lizard. It is now mortal!");
            enableRedBees = cfg.Bind("WIP","Enable Red bees (Circuit bees)",false,"Mod applies changes red bees. They now defend nest from other mobs and kill everythong in rampage!");
            spiderHuntHoardingbug = cfg.Bind("Experimental","Spider hunts Hoarding bugs",false,"Bunker spider chases and hunts hoarding bugs. Currently bugged with SpiderPositionFix.");

            //debug
            debugBool = cfg.Bind("Debug","Debug mode",false,"Enables debug mode for more debug logs.");
            debugRedBees = cfg.Bind("Debug","Log bees",false,"Enables logs for bees.");
            debugSandworms = cfg.Bind("Debug","Log sandworms",false,"Enables logs for sandowrms.");
            debugHygrodere = cfg.Bind("Debug","Log hydrogere",false,"Enables logs for hydrogere.");
            debugNutcrackers = cfg.Bind("Debug","Log nutcrackers",false,"Enables logs for nutcrackers.");
            debugSpiders = cfg.Bind("Debug","Log spider",false,"Enables logs for spider.");
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
