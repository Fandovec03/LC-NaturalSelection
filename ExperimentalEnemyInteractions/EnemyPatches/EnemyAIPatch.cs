using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{

    class EnemyData
    {
        public float originalAgentRadius = 0f;
        public SphereCollider sphereCollider = null;
        public Dictionary<Type, int> targetedByEnemies = new Dictionary<Type, int>();
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugTriggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;
        static Dictionary<EnemyAI, EnemyData> enemyData = [];

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {

            if (!enemyData.ContainsKey(__instance)) enemyData.Add(__instance, new EnemyData());
            enemyData[__instance].originalAgentRadius = __instance.agent.radius;
            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            if (debugUnspecified) Script.Logger.LogMessage($"Modified agent radius. Original: {enemyData[__instance].originalAgentRadius}, Modified: {__instance.agent.radius}");
        }
        public static string DebugStringHead(EnemyAI? instance)
        {
            //if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance);
        }
        public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetCompleteList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
        }

        public static List<EnemyAI> GetInsideOrOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetInsideOrOutsideEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(importEnemyList, instance);
        }

        public static EnemyAI? FindClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library findClosestEnemy!");
            return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(importEnemyList, importClosestEnemy, instance, includeTheDead);
        }
        public static List<EnemyAI> FilterEnemyList(List<EnemyAI> importEnemyList, List<Type>? targetTypes, List<string>? blacklist, EnemyAI instance, bool inverseToggle = false, bool filterOutImmortal = true)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library filterEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(importEnemyList, targetTypes, blacklist, instance, inverseToggle, filterOutImmortal);
        }


        static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
            return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, importEnemyList, width, importRange, proximityAwareness);
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
            Script.Logger.LogInfo($"{DebugStringHead(__instance)} registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.");
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
