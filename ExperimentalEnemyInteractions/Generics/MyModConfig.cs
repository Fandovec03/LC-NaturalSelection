using BepInEx.Configuration;
using Dissonance;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace NaturalSelection.Generics;
    class MyModConfig
    {
    //experimental fixes
    public readonly ConfigEntry<bool> sandwormCollisionOverride;
    public readonly ConfigEntry<bool> blobAICantOpenDoors;
    //settings
    public readonly ConfigEntry<bool> stableMode;
    public readonly ConfigEntry<bool> IgnoreImmortalEnemies;
    public readonly ConfigEntry<float> agentRadiusModifier;
    //enemy bools
    public readonly ConfigEntry<bool> enableSpider;
    public readonly ConfigEntry<bool> enableSlime;
    public readonly ConfigEntry<bool> enableLeviathan;
    public readonly ConfigEntry<bool> enableSporeLizard;
    public readonly ConfigEntry<bool> enableRedBees;
    public readonly ConfigEntry<bool> enableNutcracker;
    public readonly ConfigEntry<bool> enableGiant;
    public readonly ConfigEntry<bool> enableHoardingBug;

    ////enemy settings
    
    //Giant
    public readonly ConfigEntry<float> beesSetGiantsOnFireMinChance;
    public readonly ConfigEntry<float> beesSetGiantsOnFireMaxChance;
    public readonly ConfigEntry<int> giantExtinguishChance;
    //Hygrodere
    public readonly ConfigEntry<bool> blobConsumesCorpses;
    public readonly ConfigEntry<bool> blobPathfindToCorpses;
    public readonly ConfigEntry<bool> blobPathfind;
    //Sandworm
    public readonly ConfigEntry<bool> sandwormDoNotEatPlayersInsideLeavingShip;
    public readonly ConfigEntry<bool> sandwormFilterTypes;
    //Spider web
    public readonly ConfigEntry<bool> enableSpiderWebs;
    public readonly ConfigEntry<string> speedModifierList;
    public readonly ConfigEntry<float> webStrength;
    //Spider
    public readonly ConfigEntry<float> chaseAfterEnemiesModifier;

    //blacklists
    public readonly ConfigEntry<string> beeBlacklist;
    public readonly ConfigEntry<string> blobBlacklist;
    public readonly ConfigEntry<string> sandwormBlacklist;
    public readonly ConfigEntry<string> spiderWebBlacklist;
    public readonly ConfigEntry<string> spiderBlacklist;
    //debug
    public ConfigEntry<bool> debugBool;
    public ConfigEntry<bool> spammyLogs;
    public ConfigEntry<bool> debugTriggerFlags;
    public ConfigEntry<bool> debugNetworking;
    public ConfigEntry<bool> debugRedBees;
    public ConfigEntry<bool> debugSandworms;
    public ConfigEntry<bool> debugHygrodere;
    public ConfigEntry<bool> debugNutcrackers;
    public ConfigEntry<bool> debugSpiders;
    public ConfigEntry<bool> debugGiants;
    public ConfigEntry<bool> debugUnspecified;
    public ConfigEntry<bool> debugLibrary;
    public ConfigEntry<bool> debugSpiderWebs;
    public Dictionary<string,ConfigEntry<bool>> debugEntries = new Dictionary<string, ConfigEntry<bool>>();
    public Dictionary<string, ConfigEntry<bool>> CompatibilityEntries = new Dictionary<string, ConfigEntry<bool>>();
    //Compatibility overrides
    public ConfigEntry<bool> enhancedMonstersCompToggle;
    public ConfigEntry<bool> sellBodiesFixedCompToggle;
    public ConfigEntry<bool> ReXuvinationCompToggle;
    public MyModConfig(ConfigFile cfg)
    {
        cfg.SaveOnConfigSet = false;

        {
            //experimental fixes
            sandwormCollisionOverride = cfg.Bind("Experimental Fixes", "Sandworm collision override", false, "Override vanilla sandworm collisions. May fix lag when sandworm collides with multiple enemies at once. \n \n May be removed in the future.");
            blobAICantOpenDoors = cfg.Bind("Experimental Fixes", "Blob cannot open doors", true, "Blob can't open doors.");

            //general settings
            stableMode = cfg.Bind("General Settings", "Toggle stable mode", true, "When true, the mod will exlude patches that are WIP or are experimental from loading. Requires restart.");
            IgnoreImmortalEnemies = cfg.Bind("General Settings", "Ignore Immortal Enemies", false, "All immortal enemies will be ignored by majority of entities.");
            agentRadiusModifier = cfg.Bind("General Settings", "Agent radius modifier", 0.50f, "Modifies agent radius of entities for more reliable collisions.");

            //enable entities
            enableSpider = cfg.Bind("DEV", "Enable spider", false, "Enable changes to apply to to spider and modify it's behavior. Unfinished.");
            enableSlime = cfg.Bind("Entity settings", "Enable slime", true, "Enable changes to apply to to slime and modify it's behavior.");
            enableLeviathan = cfg.Bind("Entity settings", "Enable leviathan", true, "Enable changes to apply to to leviathan and modify it's behavior.");
            enableSporeLizard = cfg.Bind("DEV", "Enable SporeLizard", false, "Enable changes to apply to to spore lizard. \n\n Early build. DEV ONLY");
            enableRedBees = cfg.Bind("Entity settings", "Enable Red bees (Circuit bees)", true, "Enable changes to apply to red bees and modify it's behavior.");
            enableNutcracker = cfg.Bind("DEV", "Enable Nutcracker", false, "Enable changes to nutcracker to apply to and modify its behavior. \n\n Early build. DEV ONLY");
            enableGiant = cfg.Bind("Entity settings", "Enable Giant", true, "Enable changes to apply to to forest giant.");
            enableHoardingBug = cfg.Bind("DEV", "Enable Hoarding bug", false, "Enable changes to apply to to hoarding bug");
            enableSpiderWebs = cfg.Bind("Entity settings", "Enable Spider Webs", true, "Enables changes to apply to to spider webs. Webs will stick to and slow down enemies.");
            //entity settings
            //Giant
            giantExtinguishChance = cfg.Bind("Entity settings | Giant", "Extinguish chance", 33, new ConfigDescription("Chance of giants extinguishing themselves in percent.", new AcceptableValueRange<int>(0,100)));
            beesSetGiantsOnFireMinChance = cfg.Bind("Entity settings | Giant", "Ignite giants min chace", 1.5f, new ConfigDescription("The minimum chance bees will set giant on fire on hit in percent. Applies to calm bees.", new AcceptableValueRange<float>(0f,100f)));
            beesSetGiantsOnFireMaxChance = cfg.Bind("Entity settings | Giant", "Ignite giants max chace", 8f, new ConfigDescription("The minimum chance bees will set giant on fire on hit in percent. Applies to angry bees.", new AcceptableValueRange<float>(0f,100f)));
            //Hygrodere
            blobConsumesCorpses = cfg.Bind("Entity settings | Hygrodere", "Consume corpses", true, "Hygrodere consume dead enemy corpses.");
            blobPathfindToCorpses = cfg.Bind("Entity settings | Hygrodere", "Pathfind to corpses", true, "Hygrodere move towards corpses to consume.");
            blobPathfind = cfg.Bind("Entity settings | Hygrodere", "Pathfind", true, "Pathfind to other entities.");
            //Sandworm
            sandwormDoNotEatPlayersInsideLeavingShip = cfg.Bind("Entity settings | Sandworm", "Do not eat players inside leaving ship", false, "Worms do not eat players inside ship leaving moon.");
            sandwormFilterTypes = cfg.Bind("Entity settings | Sandworm", "Filter out enemy types", true, "Filter out enemies by enemy type. When disabled allows sandworms to target that are filtered out (Vanilla enemies smaller than employee). Blacklisting enemies is highly recommended when this setting is disabled.");
            //Spider/Spider Web
            chaseAfterEnemiesModifier = cfg.Bind("Entity settings | Spider/Spider Web", "Chase after enemies modifier", 3f, "Modifies chase timer for chasing enemies. When chasing another enemy, hunter's chase timer is divided by set number.");
            speedModifierList = cfg.Bind("Entity settings | Spider/Spider Web", "Web speed modifiers", "", "Modifies final speed of enemy caught in web. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:Speed ] \n This config generates automatically.");
            webStrength = cfg.Bind("Entity settings | Spider/Spider Web", "Spider Web Strenght", 1.3f, "Strength of spider webs. Stronger spider web slows enemies more.");
            //blacklists
            beeBlacklist = cfg.Bind("Blacklists", "Bees Blacklist", "", "Any enemy with set value to true will be ignored by circuit bees. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:True/False] \n This config generates automatically.");
            blobBlacklist = cfg.Bind("Blacklists", "Blob Blacklist", "", "Any enemy with set value to true will be ignored by hygroderes. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:True/False ] \n This config generates automatically.");
            sandwormBlacklist = cfg.Bind("Blacklists", "Sandworm Blacklist", "", "Any enemy with set value to true will be ignored by sandworms. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:True/False] \n This config generates automatically.");
            spiderWebBlacklist = cfg.Bind("Blacklists", "Web blacklist", "", "Any enemy with set value to true will be ignored by webs. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:True/False] \n This config generates automatically.");
            spiderBlacklist = cfg.Bind("Blacklists", "Spider blacklist", "", "Any enemy with set value to true will be ignored by spider. \n \n [The ',' acts as a separator between each entry. Entry format: EnemyName:True/False] \n This config generates automatically.");
            //debug
            debugBool = cfg.Bind("Debug","Debug mode",false,"Enables debug mode for more debug logs. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugBool), debugBool);
            spammyLogs = cfg.Bind("Debug","Spammy logs",false,"Enables spammy logs for extra logs. Can be changed at runtime via config mods."); debugEntries.Add(nameof(spammyLogs), spammyLogs);
            debugNetworking = cfg.Bind("Debug", "Debug networking", false, "Enables debug logs for networking. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugNetworking), debugNetworking);
            debugTriggerFlags = cfg.Bind("Debug","Trigger flags",false,"Enables logs with trigger flag."); debugEntries.Add(nameof(debugTriggerFlags), debugTriggerFlags);
            debugUnspecified = cfg.Bind("Debug", "Log unspecified", false, "Enables logs for unspecified. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugUnspecified), debugUnspecified);
            debugLibrary = cfg.Bind("Debug", "Log library", false, "Enables logs for the library. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugLibrary), debugLibrary);
            debugRedBees = cfg.Bind("Debug","Log bees",false,"Enables logs for bees. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugRedBees), debugRedBees);
            debugSandworms = cfg.Bind("Debug","Log sandworms",false,"Enables logs for sandowrms. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugSandworms), debugSandworms);
            debugHygrodere = cfg.Bind("Debug","Log hydrogere",false,"Enables logs for hydrogere. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugHygrodere), debugHygrodere);
            debugNutcrackers = cfg.Bind("Debug","Log nutcrackers",false,"Enables logs for nutcrackers. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugNutcrackers), debugNutcrackers);
            debugSpiders = cfg.Bind("Debug","Log spiders",false,"Enables logs for spiders. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugSpiders), debugSpiders);
            debugGiants = cfg.Bind("Debug", "Log giants", false, "Enables logs for giants. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugGiants), debugGiants);
            debugSpiderWebs = cfg.Bind("Debug", "Log spider webs", false, "Enables logs for spider webs. Can be changed at runtime via config mods."); debugEntries.Add(nameof(debugSpiderWebs), debugSpiderWebs);

            //Compatibility overrides
            ReXuvinationCompToggle = cfg.Bind("Compatibility toggles", "ReXuvination compatibility", false, "Manually toggles compatibility patches for ReXuvination."); CompatibilityEntries.Add("XuuXiaolan.ReXuvination", ReXuvinationCompToggle);
            enhancedMonstersCompToggle = cfg.Bind("DEV", "Enhanced monsters compatibility", false, "Manually toggles compatibility patches for Enhanced monsters."); CompatibilityEntries.Add("com.velddev.enhancedmonsters", enhancedMonstersCompToggle);
            sellBodiesFixedCompToggle = cfg.Bind("DEV", "Sellbodiesfixed compatibility", false, "Manually toggles compatibility patches for Sellbodiesfixed."); CompatibilityEntries.Add("Entity378.sellbodies", sellBodiesFixedCompToggle);
        }
        ClearOrphanedEntries(cfg);
        cfg.Save();
        cfg.SaveOnConfigSet = true;
    }
    public void ClearOrphanedEntries(ConfigFile cfg)
    {
    PropertyInfo orphanedEnriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
    var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEnriesProp.GetValue(cfg);

    foreach(var orphanedEntry in orphanedEntries)
    {
        Script.Logger.LogWarning($"Found orphaned entry of {orphanedEntry.Key.Section}: {orphanedEntry.Key.Key}, {orphanedEntry.Value}. Make sure your configuration was not altered.");
    }

    orphanedEntries.Clear();
    }
}
