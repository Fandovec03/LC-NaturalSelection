using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using NaturalSelection.EnemyPatches;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using BepInEx.Configuration;
using System.Linq;
using Unity.Burst.CompilerServices;
using JetBrains.Annotations;
using System.Text;
using BepInEx.Bootstrap;
using NaturalSelection.Compatibility;

namespace NaturalSelection;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("fandovec03.NaturalSelectionLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]

public class Script : BaseUnityPlugin
{
    public static Script Instance { get; private set; } = null!;

    public new static ManualLogSource Logger = null!;
    internal static Harmony? Harmony { get; set; }

    internal static MyModConfig BoundingConfig { get; set; } = null!;
    internal static bool stableToggle;
    private static bool isExperimental = false;
    private static bool isPrerelease = false;

    private static bool debugBool = false;
    private static bool spammyLogs = false;
    private static bool debugNetworking = false;
    private static bool debugLibrary = false;
    private static bool debugTriggerFlags = false;
    private static bool debugGiants = false;
    private static bool debugHygrodere = false;
    private static bool debugNutcrackers = false;
    private static bool debugRedBees = false;
    private static bool debugSandworms = false;
    private static bool debugSpiders = false;
    private static bool debugSpiderWebs = false;
    private static bool debugUnspecified = false;
    //Compatibilities
    internal static bool enhancedMonstersPresent = false;
    internal static bool sellBodiesPresent = false;
    internal static bool rexuvinationPresent = false;


    internal static Dictionary<string,bool> Bools = new Dictionary<string, bool>();
    internal static List<EnemyAI> loadedEnemyList = new List<EnemyAI>();
    public static Action<string, bool>? OnConfigSettingChanged;

    static void SubscribeDebugConfigBools(ConfigEntry<bool> entryKey,bool boolParam, string entry)
    {
        entryKey.SettingChanged += (obj, args) => { boolParam = entryKey.Value; Logger.LogMessage($"Updating with entry.Value {entryKey.Value}. Result: {boolParam}"); OnConfigSettingChanged?.Invoke(entry, entryKey.Value); };
    }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        BoundingConfig = new MyModConfig(base.Config);
        stableToggle = BoundingConfig.stableMode.Value;

        Bools.Add(nameof(debugBool),debugBool);
        Bools.Add(nameof(spammyLogs),spammyLogs);
        Bools.Add(nameof(debugNetworking),debugNetworking);
        Bools.Add(nameof(debugLibrary),debugLibrary);
        Bools.Add(nameof(debugTriggerFlags),debugTriggerFlags);
        Bools.Add(nameof(debugGiants),debugGiants);
        Bools.Add(nameof(debugHygrodere),debugHygrodere);
        Bools.Add(nameof(debugNutcrackers),debugNutcrackers);
        Bools.Add(nameof(debugRedBees),debugRedBees);
        Bools.Add(nameof(debugSandworms),debugSandworms);
        Bools.Add(nameof(debugSpiders),debugSpiders);
        Bools.Add(nameof(debugSpiderWebs),debugSpiderWebs);
        Bools.Add(nameof(debugUnspecified),debugUnspecified);

        foreach(var entry in BoundingConfig.debugEntries)
        {
            if(Bools.ContainsKey(entry.Key))
            {
                Bools[entry.Key] = entry.Value.Value;
                SubscribeDebugConfigBools(entry.Value, Bools[entry.Key], entry.Key);
                //Logger.LogMessage($" {entry.Key} found with value {entry.Value.Value} to {Bools[entry.Key]}");
            }
            else Logger.LogError($"Failed to find bool for config entry {entry.Key}");
        }

        char[] chars = MyPluginInfo.PLUGIN_VERSION.ToCharArray();
        if (chars[0] == '9' && chars[1] == '9' || chars[0] == '8' && chars[1] == '8')
        {
            if (chars[0] == '9' && chars[1] == '9') isExperimental = true;
            if(chars[0] == '8' && chars[1] == '8') isPrerelease = true;
            chars[0] = '0'; chars[1] = '0';
        }
        Patch();

        StringBuilder version = new StringBuilder();

        version.Append(chars);
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{version} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogInfo($"Patching {MyPluginInfo.PLUGIN_NAME}...");

        foreach (var item in Chainloader.PluginInfos)
        {
            //Logger.LogInfo($"GUID in Metadata> {item.Value.Metadata.GUID} : GUID in Key> {item.Key}");
            string comment = "";

            switch (item.Key)
            {
                case "com.velddev.enhancedmonsters":
                    {
                        enhancedMonstersPresent = true;
                        comment = "Enhanced Monsters is present";
                        break;
                    }
                case "Entity378.sellbodies":
                    {
                        sellBodiesPresent = true;
                        comment = "SellbodiesFixed is present";
                        break;
                    }
                case "XuuXiaolan.ReXuvination":
                    {
                        rexuvinationPresent = true;
                        comment = "ReXuvination is present";
                        break;
                    }
            }
            if (comment != "") Logger.LogInfo(comment);
        }

        foreach (var item in BoundingConfig.CompatibilityEntries)
        {
            string comment = "";
            if (item.Value.Value == true)
            {
                switch (item.Key)
                {
                    case "com.velddev.enhancedmonsters":
                        enhancedMonstersPresent = true;
                        break;
                    case "Entity378.sellbodies":
                        sellBodiesPresent = true;
                        break;
                    case "XuuXiaolan.ReXuvination":
                        rexuvinationPresent = true;
                        break;
                }
                comment = $"Forcefully enabling compatibility for {item.Key}";
            }
            if (comment != "") Logger.LogInfo(comment);
        }

        if (isExperimental) Logger.LogFatal($"LOADING EXPERIMENTAL BUILD OF {MyPluginInfo.PLUGIN_NAME.ToUpper()}, DOWNLOAD NATURAL SELECTION INSTEAD FOR MORE STABLE EXPERIENCE!");
        if (isPrerelease) Logger.LogWarning($"LOADING PRERELASE BUILD OF {MyPluginInfo.PLUGIN_NAME.ToUpper()}, DOWNLOAD NATURAL SELECTION INSTEAD FOR MORE STABLE EXPERIENCE!");

        Harmony.PatchAll(typeof(AICollisionDetectPatch));
        Harmony.PatchAll(typeof(EnemyAIPatch));
        Harmony.PatchAll(typeof(Networking));
        Harmony.PatchAll(typeof(NetworkingMethods));
        Harmony.PatchAll(typeof(InitializeGamePatch));

        try
        {
            NaturalSelectionLib.NaturalSelectionLib.SetLibraryLoggers(Logger, spammyLogs, debugLibrary);
            Logger.LogMessage($"Library successfully setup! Version {NaturalSelectionLib.NaturalSelectionLib.ReturnVersion()}");
        }
        catch
        {
            Logger.LogError("Failed to setup library!");
        }
        Harmony.PatchAll(typeof(RoundManagerPatch));
        if (BoundingConfig.enableLeviathan.Value)Harmony.PatchAll(typeof(SandWormAIPatch));
        if (BoundingConfig.enableSlime.Value)Harmony.PatchAll(typeof(BlobAIPatch));
        if (BoundingConfig.enableHoardingBug.Value)Harmony.PatchAll(typeof(HoarderBugPatch));
        if (BoundingConfig.enableRedBees.Value) Harmony.PatchAll(typeof(BeeAIPatch));
        if (BoundingConfig.enableGiant.Value)Harmony.PatchAll(typeof(ForestGiantPatch));
        if (BoundingConfig.enableSpiderWebs.Value)Harmony.PatchAll(typeof(SandSpiderWebTrapPatch));

        //Compatibilities
        if (enhancedMonstersPresent && !stableToggle) Harmony.PatchAll(typeof(EnhancedMonstersCompatibility));
        if (sellBodiesPresent && !stableToggle) SellBodiesFixedCompatibility.AddTracerScriptToPrefabs();
        if (rexuvinationPresent) Harmony.PatchAll(typeof(ReXuvinationPatch));

        if (!stableToggle)
        {
        if (BoundingConfig.enableSpider.Value)Harmony.PatchAll(typeof(SandSpiderAIPatch));

        if (isExperimental)
        {
            if (BoundingConfig.enableNutcracker.Value) Harmony.PatchAll(typeof(NutcrackerAIPatch));
            if (BoundingConfig.enableSporeLizard.Value) Harmony.PatchAll(typeof(PufferAIPatch));
        }
        else
        {
                Logger.LogWarning("Limited access. Some patches cannot be enabled in stable branch.");
        }

            Logger.LogInfo("Stable mode off. Loaded all patches.");
        }
        else
        {
        Logger.LogInfo("Stable mode on. Excluded unstable and WIP patches from loading.");
        }
        Logger.LogInfo("Finished patching " + MyPluginInfo.PLUGIN_NAME + " !");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
