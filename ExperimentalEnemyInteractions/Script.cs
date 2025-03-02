using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using NaturalSelection.EnemyPatches;
using UnityEngine;
using System.Collections.Generic;

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
    internal static float clampedAgentRadius;
    internal static bool isExperimental;
    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        BoundingConfig = new MyModConfig(base.Config);
        stableToggle = BoundingConfig.stableMode.Value;
        clampedAgentRadius = Mathf.Clamp(BoundingConfig.agentRadiusModifier.Value, 0.1f, 1f);
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

        Logger.LogInfo("Patching "+ MyPluginInfo.PLUGIN_NAME + " ...");
        if (isExperimental)
        {
            Logger.LogError("LOADING EXPERIMENTAL " + MyPluginInfo.PLUGIN_NAME.ToUpper() + ", DOWNLOAD STABLE NATURAL SELECTION INSTEAD!");
        }
        Harmony.PatchAll(typeof(AICollisionDetectPatch));
        Harmony.PatchAll(typeof(EnemyAIPatch));
        Harmony.PatchAll(typeof(Networking));
        Harmony.PatchAll(typeof(NetworkingMethods));
        Harmony.PatchAll(typeof(InitializeGamePatch));
        try
        {
            NaturalSelectionLib.NaturalSelectionLib.LibrarySetup(Logger, BoundingConfig.spammyLogs.Value, BoundingConfig.debugLibrary.Value);
            Logger.LogMessage("Library successfully setup! Version " + NaturalSelectionLib.MyPluginInfo.PLUGIN_VERSION);
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

        if (!stableToggle)
        {
        if (BoundingConfig.enableNutcracker.Value)Harmony.PatchAll(typeof(NutcrackerAIPatch));
        if (BoundingConfig.enableSporeLizard.Value)Harmony.PatchAll(typeof(PufferAIPatch));
        if (BoundingConfig.enableSpider.Value)Harmony.PatchAll(typeof(SandSpiderAIPatch));
        if (BoundingConfig.enableSpiderWebs.Value) Harmony.PatchAll(typeof(SandSpiderWebTrapPatch));

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
