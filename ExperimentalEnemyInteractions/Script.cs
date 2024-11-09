using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ExperimentalEnemyInteractions.Generics;
using ExperimentalEnemyInteractions.EnemyPatches;
using UnityEngine;

namespace ExperimentalEnemyInteractions
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
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

            Logger.LogInfo("Patching EEI...");

            Harmony.PatchAll(typeof(AICollisionDetectPatch));
            Harmony.PatchAll(typeof(EnemyAIPatch));
            Harmony.PatchAll(typeof(SandWormAIPatch));
            Harmony.PatchAll(typeof(BlobAIPatch));
            Harmony.PatchAll(typeof(HoarderBugPatch));
            Harmony.PatchAll(typeof(BeeAIPatch));
            Harmony.PatchAll(typeof(ForestGiantPatch));

            if (!stableToggle)
            {
            Harmony.PatchAll(typeof(NutcrackerAIPatch));
            Harmony.PatchAll(typeof(PufferAIPatch));
            Harmony.PatchAll(typeof(SandSpiderAIPatch));

            Logger.LogInfo("Stable mode off. Loaded all patches.");
            }
            else
            {
            Logger.LogInfo("Stable mode on. Excluded unstable and WIP patches from loading.");
            }
            Logger.LogInfo("Finished patching EEI!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
