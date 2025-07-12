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
        public static Dictionary<EnemyAI, EnemyData> enemyDataDict = [];
        public static Dictionary<EnemyAI, BeeValues> beeDataDict = [];
        public static Dictionary<EnemyAI, BlobData> blobDataDict = [];
        public static Dictionary<EnemyAI, GiantData> forestGiantDataDict = [];
        public static Dictionary<EnemyAI, HoarderBugValues> hoarderBugDataDict = [];
        public static Dictionary<EnemyAI, NutcrackerData> nutcrackerDataDict = [];
        public static Dictionary<EnemyAI, PufferData> pufferDataDict = [];
        public static Dictionary<EnemyAI, SpiderData> spiderDataDict = [];
        public static Dictionary<EnemyAI, ExtendedSandWormAIData> sandwormDataDict = [];

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
            EnemyData data = GetEnemyData(__instance, new EnemyData());

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

        public static EnemyData GetEnemyData(EnemyAI __instance, EnemyData enemyData)
        {
            if (!enemyDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                enemyDataDict.Add(__instance, enemyData);
            }
            return enemyDataDict[__instance];
        }

        public static BeeValues GetEnemyData(EnemyAI __instance, BeeValues enemyData)
        {
            if (!beeDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                beeDataDict.Add(__instance, enemyData);
            }
            return beeDataDict[__instance];
        }

        public static BlobData GetEnemyData(EnemyAI __instance, BlobData enemyData)
        {
            if (!blobDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                blobDataDict.Add(__instance, enemyData);
            }
            return blobDataDict[__instance];
        }

        public static GiantData GetEnemyData(EnemyAI __instance, GiantData enemyData)
        {
            if (forestGiantDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                forestGiantDataDict.Add(__instance, enemyData);
            }
            return forestGiantDataDict[__instance];
        }

        public static HoarderBugValues GetEnemyData(EnemyAI __instance, HoarderBugValues enemyData)
        {
            if (!hoarderBugDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                hoarderBugDataDict.Add(__instance, enemyData);
            }
            return hoarderBugDataDict[__instance];
        }

        public static NutcrackerData GetEnemyData(EnemyAI __instance, NutcrackerData enemyData)
        {
            if (!nutcrackerDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                nutcrackerDataDict.Add(__instance, enemyData);
            }
            return nutcrackerDataDict[__instance];
        }

        public static PufferData GetEnemyData(EnemyAI __instance, PufferData enemyData)
        {
            if (!pufferDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                pufferDataDict.Add(__instance, enemyData);
            }
            return pufferDataDict[__instance];
        }

        public static SpiderData GetEnemyData(EnemyAI __instance, SpiderData enemyData)
        {
            if (!spiderDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                spiderDataDict.Add(__instance, enemyData);
            }
            return spiderDataDict[__instance];
        }

        public static ExtendedSandWormAIData GetEnemyData(EnemyAI __instance, ExtendedSandWormAIData enemyData)
        {
            if (!sandwormDataDict.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Warning, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                sandwormDataDict.Add(__instance, enemyData);
            }
            return sandwormDataDict[__instance];
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
