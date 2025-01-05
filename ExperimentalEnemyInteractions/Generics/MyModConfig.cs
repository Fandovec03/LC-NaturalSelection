using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NaturalSelection.Generics;
    class MyModConfig
    {
    //experimental fixes
    public readonly ConfigEntry<bool> sandwormCollisionOverride;
    public readonly ConfigEntry<bool> blobAICantOpenDoors;
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
    public readonly ConfigEntry<bool> enableNutcracker;
    public readonly ConfigEntry<bool> enableGiant;
    public readonly ConfigEntry<bool> enableHoardingBug;

    //enemy settings
    public readonly ConfigEntry<int> giantExtinguishChance;
    public readonly ConfigEntry<float> beesSetGiantsOnFireMinChance;
    public readonly ConfigEntry<float> beesSetGiantsOnFireMaxChance;
    public readonly ConfigEntry<bool> blobConsumesCorpses;
    public readonly ConfigEntry<bool> blobPathfindToCorpses;
    public readonly ConfigEntry<bool> blobPathfind;
    public readonly ConfigEntry<bool> sandwormDoNotEatPlayersInsideLeavingShip;
    public readonly ConfigEntry<bool> sandwormFilterTypes;
    //blacklists
    public readonly ConfigEntry<string> beeBlacklist;
    public readonly ConfigEntry<string> blobBlacklist;
    public readonly ConfigEntry<string> sandwormBlacklist;
    //debug
    public readonly ConfigEntry<bool> debugBool;
    public readonly ConfigEntry<bool> spammyLogs;
    public readonly ConfigEntry<bool> debugTriggerFlags;
    public readonly ConfigEntry<bool> debugNetworking;
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
            sandwormCollisionOverride = cfg.Bind("Experimental Fixes", "Sandworm collision override", false, "Override vanilla sandworm collisions. May fix lag when sandworm collides with multiple enemies at once");
            blobAICantOpenDoors = cfg.Bind("Experimental Fixes", "Blob cannot open doors", true, "Blob can't open doors.");
            //general settings
            stableMode = cfg.Bind("General Settings", "Toggle stable mode", true, "When true, the mod will exlude patches that are WIP or are experimental from loading");
            IgnoreImmortalEnemies = cfg.Bind("General Settings", "Ignore Immortal Enemies", false, "All immortal enemies will be ignored by majority of entities");
            //WIP
            agentRadiusModifier = cfg.Bind("WIP", "Agent radius modifier", 0.6f, "Agent radius multiplier. Agent size is modified to make collisions more reliable. Lower multiplier makes final Agent radius smaller. \n \n [Values not between 0.1 and 1 are Clamped]");
            agentRadiusModifier.Value = Mathf.Clamp(agentRadiusModifier.Value, 0.1f, 1f);
            spiderHuntHoardingbug = cfg.Bind("WIP", "Spider hunts Hoarding bugs", false, "Bunker spider chases and hunts hoarding bugs. DEV ONLY");
            //enable entities
            enableSpider = cfg.Bind("WIP", "Enable spider", false, "Enable changes to spider and modify it's behavior. DEV ONLY");
            enableSlime = cfg.Bind("Entity settings", "Enable slime", true, "Enable changes to slime and modify it's behavior.");
            enableLeviathan = cfg.Bind("Entity settings", "Enable leviathan", true, "Enable changes to leviathan and modify it's behavior.");
            enableSporeLizard = cfg.Bind("WIP", "Enable SporeLizard", false, "Enable changes to spore lizard. DEV ONLY");
            enableRedBees = cfg.Bind("Entity settings", "Enable Red bees (Circuit bees)", true, "Enable changes red bees and modify it's behavior.");
            enableNutcracker = cfg.Bind("WIP", "Enable Nutcracker", false, "Enable changes to nutcracker and modify its behavior. DEV ONLY");
            enableGiant = cfg.Bind("Entity settings", "Enable Giant", false, "Enable changes to forest giant.");
            enableHoardingBug = cfg.Bind("WIP", "Enable Hoarding bug", false, "Enable changes to hoarding bug");
            //entity settings
            giantExtinguishChance = cfg.Bind("Entity settings", "(Giant) Extinguish chance", 33, "[Accepts int values between 0 and 100] Chance of giants extinguishing themselves.");
            beesSetGiantsOnFireMinChance = cfg.Bind("Entity settings", "(Bees) Ignite giants min chace", 1.5f, "[Accepts float values between 0 and 100]The minimum chance bees will set giant on fire on hit");
            beesSetGiantsOnFireMaxChance = cfg.Bind("Entity settings", "(Bees) Ignite giants max chace", 8f, "[Accepts float values between 0 and 100]The minimum chance bees will set giant on fire on hit");
            blobConsumesCorpses = cfg.Bind("Entity settings", "(Blob) Consume corpses", true, "Hydrogire consume enemy corpses");
            blobPathfindToCorpses = cfg.Bind("Entity settings", "(Blob) Pathfind to corpses", true, "Hydrogire move towards corpses to consume");
            blobPathfind = cfg.Bind("Entity settings", "(Blob) Pathfind", true, "Pathfind to other entities");
            sandwormDoNotEatPlayersInsideLeavingShip = cfg.Bind("Entity settings", "(Sandworm) Do not eat players inside leaving ship", false, "Worms do not eat players inside ship leaving the moon.");
            sandwormFilterTypes = cfg.Bind("Entity settings", "(Sandworm) Filter out enemy types", true, "Filter out enemies by the enemy type. Disabling this allows sandworms to attack other enemies. Blacklisting enemies is highly recommended when this setting is disabled.");
            //blacklists
            beeBlacklist = cfg.Bind("Blacklists", "Bees Blacklist", "", "Any enemy inside the blacklist will be ignored by others. \n \n [The ',' acts as a separator]");
            blobBlacklist = cfg.Bind("Blacklists", "Blob Blacklist", "", "Any enemy inside the blacklist will be ignored by others. \n \n [The ',' acts as a separator]");
            sandwormBlacklist = cfg.Bind("Blacklists", "Sandworm Blacklist", "", "Any enemy inside the blacklist will be ignored by others. \n \n [The ',' acts as a separator]");
            //debug
            debugBool = cfg.Bind("Debug","Debug mode",false,"Enables debug mode for more debug logs.");
            spammyLogs = cfg.Bind("Debug","Spammy logs",false,"Enables spammy logs for extra logs.");
            debugNetworking = cfg.Bind("Debug", "Debug networking", false, "Enables debug logs for networking.");
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
