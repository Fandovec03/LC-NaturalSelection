using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using NaturalSelection.EnemyPatches;
using UnityEngine;

namespace NaturalSelection;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("fandovec03.NaturalSelectionLib", BepInDependency.DependencyFlags.HardDependency)]
public class Script : BaseUnityPlugin
{
    public static Script Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    internal static MyModConfig BoundingConfig { get; set; } = null!;
    internal static bool stableToggle;
    internal static float clampedAgentRadius;
    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        BoundingConfig = new MyModConfig(base.Config);
        stableToggle = BoundingConfig.stableMode.Value;
        clampedAgentRadius = Mathf.Clamp(BoundingConfig.agentRadiusModifier.Value, 0.1f, 1f);

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogInfo("Patching "+ MyPluginInfo.PLUGIN_NAME + " ...");

        for (int i = 0; i < 100; i++)
        {
            Logger.LogError("LOADING EXPERIMENTAL " + MyPluginInfo.PLUGIN_NAME.ToUpper() + ", DOWNLOAD NATURAL SELECTION INSTEAD!");
        }
        Harmony.PatchAll(typeof(AICollisionDetectPatch));
        Harmony.PatchAll(typeof(EnemyAIPatch));
        try
        {
            NaturalSelectionLib.NaturalSelectionLib.LibrarySetup(Logger, BoundingConfig.spammyLogs.Value, BoundingConfig.debugUnspecified.Value);
            Logger.LogInfo("Library successfully setup!");
        }
        catch
        {
            Logger.LogError("Failed to setup library!");
        }
        Harmony.PatchAll(typeof(RoundManagerPatch));
        if (BoundingConfig.loadSandworms.Value)Harmony.PatchAll(typeof(SandWormAIPatch));
        if (BoundingConfig.loadBlob.Value)Harmony.PatchAll(typeof(BlobAIPatch));
        if (BoundingConfig.loadHoardingBugs.Value)Harmony.PatchAll(typeof(HoarderBugPatch));
        if (BoundingConfig.LoadBees.Value)Harmony.PatchAll(typeof(BeeAIPatch));
        if (BoundingConfig.loadGiants.Value)Harmony.PatchAll(typeof(ForestGiantPatch));

        if (!stableToggle)
        {
        if (BoundingConfig.loadNutcrackers.Value)Harmony.PatchAll(typeof(NutcrackerAIPatch));
        if (BoundingConfig.loadSporeLizard.Value)Harmony.PatchAll(typeof(PufferAIPatch));
        if (BoundingConfig.loadSpiders.Value)Harmony.PatchAll(typeof(SandSpiderAIPatch));

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
