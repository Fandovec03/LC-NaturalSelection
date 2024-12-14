using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NaturalSelection.Generics;
    class MyModConfig
    {
    //experimental fixes
    public readonly ConfigEntry<bool> delayScriptsOnSpawn;
    public readonly ConfigEntry<float> delay;
    //settings
    public readonly ConfigEntry<bool> stableMode;
    public readonly ConfigEntry<bool> spiderHuntHoardingbug;
    public readonly ConfigEntry<float> agentRadiusModifier;
    public readonly ConfigEntry<bool> IgnoreImmortalEnemies;
    //enemy bools
    public readonly ConfigEntry<bool> enableSpider;
    public readonly ConfigEntry<bool> enableSlime;
    public readonly ConfigEntry<bool> enableLeviathan;
    public readonly ConfigEntry<bool> enableSporeLizard;
    public readonly ConfigEntry<bool> enableRedBees;
    public readonly ConfigEntry<bool> enableNutcrackers;
    //enemy settings
    public readonly ConfigEntry<int> giantExtinguishChance;
    public readonly ConfigEntry<float> beesSetGiantsOnFireMinChance;
    public readonly ConfigEntry<float> beesSetGiantsOnFireMaxChance;
    public readonly ConfigEntry<bool> blobConsumesCorpses;
    public readonly ConfigEntry<bool> blobPathfindToCorpses;
    public readonly ConfigEntry<bool> blobPathfind;
    //Load bools
    public readonly ConfigEntry<bool> loadNutcrackers;
    public readonly ConfigEntry<bool> loadSpiders;
    public readonly ConfigEntry<bool> loadSandworms;
    public readonly ConfigEntry<bool> loadGiants;
    public readonly ConfigEntry<bool> loadHoardingBugs;
    public readonly ConfigEntry<bool> loadBlob;
    public readonly ConfigEntry<bool> LoadBees;
    public readonly ConfigEntry<bool> loadSporeLizard;
    //debug
    public readonly ConfigEntry<bool> debugBool;
    public readonly ConfigEntry<bool> spammyLogs;
    public readonly ConfigEntry<bool> debugTriggerFlags;
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
            //experimental fixes
            delayScriptsOnSpawn = cfg.Bind("Experimental Fixes", "Delay enemy scripts on spawn", false, "Delay enemy scripts from taking effect on enemy spawns. Might fix invisible bees");
            delay = cfg.Bind("Experimental Fixes", "Delay", 0.2f, "Set the length of the delay");
            //general settings
            stableMode = cfg.Bind("General Settings", "Toggle stable mode", true, "When true, the mod will exlude patches that are WIP or are experimental from loading");
            IgnoreImmortalEnemies = cfg.Bind("General Settings", "Ignore Immortal Enemies", false, "All immortal enemies will be ignored by majority of entities");
            //WIP
            agentRadiusModifier = cfg.Bind("WIP", "Agent radius modifier", 0.6f, "Agent radius multiplier. Agent size is modified to make collisions more reliable. Lower multiplier makes final Agent radius smaller. \n \n [Values not between 0.1 and 1 are Clamped]");
            agentRadiusModifier.Value = Mathf.Clamp(agentRadiusModifier.Value, 0.1f, 1f);
            spiderHuntHoardingbug = cfg.Bind("WIP", "Spider hunts Hoarding bugs", false, "Bunker spider chases and hunts hoarding bugs. DEV ONLY");
            //enable entities
            enableSpider = cfg.Bind("WIP", "Enable spider", false, "Mod applies changes Bunker Spider. DEV ONLY");
            enableSlime = cfg.Bind("Entity settings", "Enable slime", true, "Mod applies changes Hygrodere. Slime now damages every entity it passes by.");
            enableLeviathan = cfg.Bind("Entity settings", "Enable leviathan", true, "Mod applies changes Earth leviathan. Leviathan now targets other creatures aswell.");
            enableSporeLizard = cfg.Bind("WIP", "Enable SporeLizard", false, "Mod applies changes Spore lizard. It is now mortal!");
            enableRedBees = cfg.Bind("Entity settings", "Enable Red bees (Circuit bees)", true, "Mod applies changes red bees. They now defend nest from other mobs and kill everything in rampage!");
            enableNutcrackers = cfg.Bind("WIP", "Enable Nutcrackers", false, "Mod applies changes to nutcrackers. DEV ONLY");
            //entity settings
            giantExtinguishChance = cfg.Bind("Entity settings", "(Giant) Extinguish chance", 33, "[Accepts int values between 0 and 100] Chance of giants extinguishing themselves.");
            beesSetGiantsOnFireMinChance = cfg.Bind("Entity settings", "(Bees) Ignite giants min chace", 1.5f, "[Accepts float values between 0 and 100]The minimum chance bees will set giant on fire on hit");
            beesSetGiantsOnFireMaxChance = cfg.Bind("Entity settings", "(Bees) Ignite giants max chace", 8f, "[Accepts float values between 0 and 100]The minimum chance bees will set giant on fire on hit");
            blobConsumesCorpses = cfg.Bind("WIP", "(Blob) Consume corpses", true, "Hydrogire consume enemy corpses");
            blobPathfindToCorpses = cfg.Bind("WIP", "(Blob) Pathfind to corpses", false, "[BROKEN - need fixing] Hydrogire move towards corpses to consume");
            blobPathfind = cfg.Bind("WIP", "(Blob) Pathfind", false, "[WIP] Pathfind to other entities");
            //load Entities
            loadSpiders = cfg.Bind("Initialization settings (Not recommended)", "Load spider patches", true, "Load the spider patches. Do not touch.");
            loadBlob = cfg.Bind("Initialization settings (Not recommended)", "Load slime patches", true, "Load the slime patches. Do not touch.");
            loadSandworms = cfg.Bind("Initialization settings (Not recommended)", "Load leviathan patches", true, "Load the leviathan patches. Do not touch.");
            loadGiants = cfg.Bind("Initialization settings (Not recommended)", "Load giant patches", true, "Load the giant patches. Do not touch.");
            LoadBees = cfg.Bind("Initialization settings (Not recommended)", "Load circuit bees patches", true, "Load bees patches. Do not touch.");
            loadNutcrackers = cfg.Bind("Initialization settings (Not recommended)", "Load nutcracker patches", true, "Load the nutcracker patches. Do not touch.");
            loadHoardingBugs = cfg.Bind("Initialization settings (Not recommended)", "Load hoarding bugs patches", true, "Load the hoarding bug patches. Do not touch.");
            loadSporeLizard = cfg.Bind("Initialization settings (Not recommended)", "Load spore lizards patches", true, "Load the spore lizard patches. Do not touch.");
            //debug
            debugBool = cfg.Bind("Debug","Debug mode",false,"Enables debug mode for more debug logs.");
            spammyLogs = cfg.Bind("Debug","Spammy logs",false,"Enables spammy logs for extra logs.");
            debugTriggerFlags = cfg.Bind("Debug","Trigger flags",false,"Enables logs with trigger flag.");
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
