using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;

namespace NaturalSelection.EnemyPatches
{
    public enum CustomEnemySize
    {
        Undefined,
        Tiny,
        Small,
        Medium,
        Large,
        Giant
    }
    class EnemyData()
    {
        internal float originalAgentRadius = 0f;
        internal Dictionary<Type, int> targetedByEnemies = new Dictionary<Type, int>();
        public CustomEnemySize customEnemySize = CustomEnemySize.Small;
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.Bools["debugUnspecified"];
        static bool debugTriggerFlags = Script.Bools["debugTriggerFlags"];
        internal static Dictionary<EnemyAI, EnemyData> enemyData = [];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)

        {
            if (entryKey == "debugUnspecified") debugUnspecified = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlags = value;
            //Script.Logger.Log(LogLevel.Message,$"EnemyAI received event. debugUnspecified = {debugUnspecified}, debugTriggerFlags = {debugTriggerFlags}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            if (!enemyData.ContainsKey(__instance))
            {
                Script.Logger.Log(BepInEx.Logging.LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                enemyData.Add(__instance, new EnemyData());
            }
            EnemyData data = enemyData[__instance];
            data.originalAgentRadius = __instance.agent.radius;

            if (InitializeGamePatch.customSizeOverrideListDictionary.ContainsKey(__instance.enemyType.enemyName))
            {
                data.customEnemySize = (CustomEnemySize)InitializeGamePatch.customSizeOverrideListDictionary[__instance.enemyType.enemyName];
            }

            Script.Logger.Log(BepInEx.Logging.LogLevel.Debug, $"Final size: {data.customEnemySize}");

            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            if (debugUnspecified && debugTriggerFlags) Script.Logger.Log(BepInEx.Logging.LogLevel.Message,$"Modified agent radius. Original: {enemyData[__instance].originalAgentRadius}, Modified: {__instance.agent.radius}");
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        static public int ReactToHit(int force = 0, EnemyAI? enemyAI = null, PlayerControllerB? player = null)
        {
            if (force > 0)
            {
                return 1;
            }
            if (force > 1)
            {
                return 2;
            }
            return 0;
        }

        [HarmonyPatch("HitEnemy")]
        [HarmonyPostfix]
        public static void HitEnemyPatch(EnemyAI __instance, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            string playerString = "unknown";
            if (playerWhoHit != null)
            {
                playerString = $"{playerWhoHit.playerUsername}(SteamID: {playerWhoHit.playerSteamId}, playerClientID: {playerWhoHit.playerClientId})";
            }
            if (debugTriggerFlags) Script.Logger.Log(BepInEx.Logging.LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.");
        }
    }

    public class ReversePatchAI
    {
        public static Action<EnemyAI> originalUpdate;

        static ReversePatchAI()
        {
            var method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Update));
            var dm = new DynamicMethod("Base.Update",null, [typeof(EnemyAI)], typeof(EnemyAI));
            var gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, method);
            gen.Emit(OpCodes.Ret);

            originalUpdate = (Action<EnemyAI>)dm.CreateDelegate(typeof(Action<EnemyAI>));
        }
    }
}
