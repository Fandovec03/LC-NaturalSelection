using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using NaturalSelection.EnemyPatches;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

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
    internal static bool isExperimental = false;

    internal static bool debugBool = false;
    internal static bool spammyLogs = false;
    internal static bool debugNetworking = false;
    internal static bool debugLibrary = false;
    internal static bool debugTriggerFlags = false;
    internal static bool debugGiants = false;
    internal static bool debugHygrodere = false;
    internal static bool debugNutcrackers = false;
    internal static bool debugRedBees = false;
    internal static bool debugSandworms = false;
    internal static bool debugSpiders = false;
    internal static bool debugSpiderWebs = false;
    internal static bool debugUnspecified = false;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        BoundingConfig = new MyModConfig(base.Config);
        stableToggle = BoundingConfig.stableMode.Value;

        debugBool = BoundingConfig.debugBool.Value;
        spammyLogs = BoundingConfig.spammyLogs.Value;
        debugNetworking = BoundingConfig.debugNetworking.Value;
        debugLibrary = BoundingConfig.debugLibrary.Value;
        debugTriggerFlags = BoundingConfig.debugTriggerFlags.Value;
        debugGiants = BoundingConfig.debugGiants.Value;
        debugHygrodere = BoundingConfig.debugHygrodere.Value;
        debugNutcrackers = BoundingConfig.debugNutcrackers.Value;
        debugRedBees = BoundingConfig.debugRedBees.Value;
        debugSandworms = BoundingConfig.debugSandworms.Value;
        debugSpiders = BoundingConfig.debugSpiders.Value;
        debugSpiderWebs = BoundingConfig.debugSpiderWebs.Value;
        debugUnspecified = BoundingConfig.debugUnspecified.Value;


        if (MyPluginInfo.PLUGIN_VERSION.Contains("99"))
        {
            isExperimental = true;
        }

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogInfo($"Patching {MyPluginInfo.PLUGIN_NAME}...");
        if (isExperimental)
        {
            Logger.LogError($"LOADING EXPERIMENTAL {MyPluginInfo.PLUGIN_NAME.ToUpper()}, DOWNLOAD STABLE NATURAL SELECTION INSTEAD!");
        }
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
        if (BoundingConfig.enableSpiderWebs.Value) Harmony.PatchAll(typeof(SandSpiderWebTrapPatch));

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
