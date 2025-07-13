using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using Steamworks.ServerList;
using BepInEx.Logging;
using System.Linq;

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

    public class EnemyDataBase
    {
        public EnemyAI? closestEnemy;
        public EnemyAI? targetEnemy;
        public CustomEnemySize customEnemySize = CustomEnemySize.Small;

    }

    class EnemyData : EnemyDataBase
    {
        internal float originalAgentRadius = 0f;
        //internal Dictionary<Type, int> targetedByEnemies = new Dictionary<Type, int>();
        //CustomEnemySize customEnemySize = CustomEnemySize.Small;
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.Bools["debugUnspecified"];
        static bool debugTriggerFlags = Script.Bools["debugTriggerFlags"];
        public static Dictionary<object, EnemyDataBase> enemyDataDict = [];
        public static Dictionary<EnemyAI, EnemyData> enemyDataDict2 = [];
        public static Dictionary<SandSpiderWebTrap, SpiderWebValues> enemyDataDict3 = [];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)

        {
            if (entryKey == "debugUnspecified") debugUnspecified = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlags = value;
            //Script.LogNS(LogLevel.Message,$"EnemyAI received event. debugUnspecified = {debugUnspecified}, debugTriggerFlags = {debugTriggerFlags}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            EnemyData data = (EnemyData)GetEnemyData(__instance, new EnemyData());

            data.originalAgentRadius = __instance.agent.radius;

            if (InitializeGamePatch.customSizeOverrideListDictionary.ContainsKey(__instance.enemyType.enemyName))
            {
                data.customEnemySize = (CustomEnemySize)InitializeGamePatch.customSizeOverrideListDictionary[__instance.enemyType.enemyName];
            }

            Script.LogNS(BepInEx.Logging.LogLevel.Debug, $"Final size: {data.customEnemySize}", __instance);

            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            Script.LogNS(BepInEx.Logging.LogLevel.Message, $"Modified agent radius. Original: {data.originalAgentRadius}, Modified: {__instance.agent.radius}", toggle: debugUnspecified && debugTriggerFlags);
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
            Script.LogNS(BepInEx.Logging.LogLevel.Info, $"registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.", __instance, debugTriggerFlags);
        }

        public static EnemyDataBase GetEnemyData(object __instance, EnemyDataBase enemyData)
        {
            if (!enemyDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                enemyDataDict.Add(__instance, enemyData);
            }
            return enemyDataDict[__instance];
        }

        public static EnemyData GetEnemyData(EnemyAI __instance, EnemyData enemyData)
        {
            if (!enemyDataDict2.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                enemyDataDict2.Add(__instance, enemyData);
            }
            return enemyDataDict2[__instance];
        }

        public static SpiderWebValues GetEnemyData(SandSpiderWebTrap __instance, SpiderWebValues enemyData)
        {
            if (!SandSpiderWebTrapPatch.spiderWebs.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                SandSpiderWebTrapPatch.spiderWebs.Add(__instance, enemyData);
            }
            return SandSpiderWebTrapPatch.spiderWebs[__instance];
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
