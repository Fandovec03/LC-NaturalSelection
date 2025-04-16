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

namespace NaturalSelection;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("fandovec03.NaturalSelectionLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]

public class Script : BaseUnityPlugin
{
    public static Script Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
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
    internal static Dictionary<string,bool> Bools = new Dictionary<string, bool>();

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

        //debugBool = BoundingConfig.debugBool.Value;
        Bools.Add(nameof(debugBool),debugBool);
        //spammyLogs = BoundingConfig.spammyLogs.Value;
        Bools.Add(nameof(spammyLogs),spammyLogs);
        //debugNetworking = BoundingConfig.debugNetworking.Value;
        Bools.Add(nameof(debugNetworking),debugNetworking);
        //debugLibrary = BoundingConfig.debugLibrary.Value;
        Bools.Add(nameof(debugLibrary),debugLibrary);
        //debugTriggerFlags = BoundingConfig.debugTriggerFlags.Value;
        Bools.Add(nameof(debugTriggerFlags),debugTriggerFlags);
        //debugGiants = BoundingConfig.debugGiants.Value;
        Bools.Add(nameof(debugGiants),debugGiants);
        //debugHygrodere = BoundingConfig.debugHygrodere.Value;
        Bools.Add(nameof(debugHygrodere),debugHygrodere);
        //debugNutcrackers = BoundingConfig.debugNutcrackers.Value;
        Bools.Add(nameof(debugNutcrackers),debugNutcrackers);
        //debugRedBees = BoundingConfig.debugRedBees.Value;
        Bools.Add(nameof(debugRedBees),debugRedBees);
        //debugSandworms = BoundingConfig.debugSandworms.Value;
        Bools.Add(nameof(debugSandworms),debugSandworms);
        //debugSpiders = BoundingConfig.debugSpiders.Value;
        Bools.Add(nameof(debugSpiders),debugSpiders);
        //debugSpiderWebs = BoundingConfig.debugSpiderWebs.Value;
        Bools.Add(nameof(debugSpiderWebs),debugSpiderWebs);
        //debugUnspecified = BoundingConfig.debugUnspecified.Value;
        Bools.Add(nameof(debugUnspecified),debugUnspecified);

        //BoundingConfig.debugBool.SettingChanged += (call, arg) => debugBool = BoundingConfig.debugBool.Value;

        foreach(var entry in BoundingConfig.debugEntries)
        {
            if(Bools.ContainsKey(entry.Key))
            {
                Bools[entry.Key] = entry.Value.Value;
                SubscribeDebugConfigBools(entry.Value, Bools[entry.Key], entry.Key);
                Logger.LogMessage($" entry.Key>{entry.Key} found with value entry.Value.Value>{entry.Value.Value} to Bools[entry.Key]>{Bools[entry.Key]}");
            }
            else Logger.LogError($"Failed to find bool for config entry {entry.Key}");
        }

        /*
        BoundingConfig.debugNetworking.SettingChanged += (call, arg) => debugNetworking = BoundingConfig.debugNetworking.Value;
        BoundingConfig.debugLibrary.SettingChanged += (call, arg) => debugLibrary = BoundingConfig.debugLibrary.Value;
        BoundingConfig.debugTriggerFlags.SettingChanged += (call, arg) => debugTriggerFlags = BoundingConfig.debugTriggerFlags.Value;
        BoundingConfig.debugGiants.SettingChanged += (call, arg) => debugGiants = BoundingConfig.debugGiants.Value;
        BoundingConfig.debugHygrodere.SettingChanged += (call, arg) => debugHygrodere = BoundingConfig.debugHygrodere.Value;
        BoundingConfig.debugNutcrackers.SettingChanged += (call, arg) => debugNutcrackers = BoundingConfig.debugNutcrackers.Value;
        BoundingConfig.debugRedBees.SettingChanged += (call, arg) => debugRedBees = BoundingConfig.debugRedBees.Value;
        BoundingConfig.debugSandworms.SettingChanged += (call, arg) => debugSandworms = BoundingConfig.debugSandworms.Value;
        BoundingConfig.debugSpiders.SettingChanged += (call, arg) => debugSpiders = BoundingConfig.debugSpiders.Value;
        BoundingConfig.debugSpiderWebs.SettingChanged += (call, arg) => debugSpiderWebs = BoundingConfig.debugSpiderWebs.Value;
        BoundingConfig.debugUnspecified.SettingChanged += (call, arg) => debugUnspecified = BoundingConfig.debugUnspecified.Value;
        */
        char[] chars = MyPluginInfo.PLUGIN_VERSION.ToCharArray();
        if (chars[0] == '9' && chars[1] == '9' || chars[0] == '8' && chars[1] == '8')
        {
            if (chars[0] == '9' && chars[1] == '9') isExperimental = true;
            if(chars[0] == '8' && chars[1] == '8') isPrerelease = true;
            chars[0] = '0'; chars[1] = '0';
        }
        Patch();
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{chars} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogInfo($"Patching {MyPluginInfo.PLUGIN_NAME}...");

        if (isExperimental) Logger.LogFatal($"LOADING EXPERIMENTAL BUILD OF {MyPluginInfo.PLUGIN_NAME.ToUpper()}, DOWNLOAD NATURAL SELECTION INSTEAD FOR MORE STABLE EXPERIENCE!");
        if (isPrerelease) Logger.LogWarning($"LOADING PRERELASE BUILD OF {MyPluginInfo.PLUGIN_NAME.ToUpper()}, DOWNLOAD NATURAL SELECTION INSTEAD FOR MORE STABLE EXPERIENCE!");

        Harmony.PatchAll(typeof(AICollisionDetectPatch));
        Harmony.PatchAll(typeof(EnemyAIPatch));
        Harmony.PatchAll(typeof(Networking));
        Harmony.PatchAll(typeof(NetworkingMethods));
        Harmony.PatchAll(typeof(InitializeGamePatch));

        try
        {
            NaturalSelectionLib.NaturalSelectionLib.LibrarySetup(Logger, spammyLogs, debugLibrary);
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

        if (!stableToggle)
        {
        if (BoundingConfig.enableNutcracker.Value)Harmony.PatchAll(typeof(NutcrackerAIPatch));
        if (BoundingConfig.enableSporeLizard.Value)Harmony.PatchAll(typeof(PufferAIPatch));
        if (BoundingConfig.enableSpider.Value)Harmony.PatchAll(typeof(SandSpiderAIPatch));

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
