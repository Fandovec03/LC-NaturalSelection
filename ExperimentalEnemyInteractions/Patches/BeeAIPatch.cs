using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    class BeeValues
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public EnemyAI? closestToBeehive = null;
        public GrabbableObject? hive = null;
        public Vector3 lastKnownEnemyPosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public float timeSinceHittingEnemy = 0f;
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static bool logBees = Script.BoundingConfig.debugRedBees.Value;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            beeList.Add(__instance, new BeeValues());

            BeeValues beeData = beeList[__instance];
            beeData.hive = __instance.hive;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            if (__instance.currentBehaviourStateIndex == 2 || __instance.targetPlayer != null)
            {
                return true;
            }
            else if (beeData.targetEnemy != null)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            enemyList = EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance);
            /*
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    if (__instance.IsHiveMissing())
                    {
                        beeData.customBehaviorStateIndex = 2;
                        __instance.SwitchToBehaviourServerRpc(2);
                        if (logBees) Script.Logger.LogDebug("case0: HIVE IS MISSING! CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        break;
                    }
                    EnemyAI LOSenemy = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1);
                    if (logBees)Script.Logger.LogDebug("case0: Checked LOS for enemies. Enemy found: " + LOSenemy);

                    if (LOSenemy != null && Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                    {
                        __instance.SetDestinationToPosition(LOSenemy.transform.position, true);
                        __instance.moveTowardsDestination = true;
                        if (logBees)Script.Logger.LogDebug("case0: Moving towards " + LOSenemy);

                        beeData.customBehaviorStateIndex = 1;
                        __instance.SwitchToBehaviourServerRpc(1);
                        if (logBees)Script.Logger.LogDebug("case0: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                    }
                    break;
                case 1:
                    if (beeData.targetEnemy == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {
                        if (__instance.IsHiveMissing())
                        {
                            beeData.customBehaviorStateIndex = 2;
                            __instance.SwitchToBehaviourServerRpc(2);
                            if (logBees)Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourServerRpc(0);
                            if (logBees)Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                    }
                    break;
                /*case 2: // Currently whenever bees go to state 2 they will ignore players and stop reporting into logs. Disabled for now
                    if (__instance.IsHivePlacedAndInLOS())
                    {
                        if (logBees) Script.Logger.LogDebug("case2: IsHivePlacedAndInLOS triggered");
                        if (__instance.wasInChase)
                        {
                            if (logBees) Script.Logger.LogDebug("case2: set wasInChase to false");
                            __instance.wasInChase = false;
                        }
                        __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;
                        EnemyAI enemyAI2 = null;
                        EnemyAIPatch.findClosestEnemy(enemyList, enemyAI2, __instance);

                        //enemyAI = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1);

                        if (logBees)Script.Logger.LogDebug("case2: checked LOS for enemies");
                        if (enemyAI2 != null && Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(enemyAI2.transform.position, true);
                            __instance.moveTowardsDestination = true;
                            if (logBees)Script.Logger.LogDebug("case2: Moving towards: " + enemyAI2);
                            beeData.customBehaviorStateIndex = 1;
                            __instance.SwitchToBehaviourServerRpc(1);
                            if (logBees)Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourServerRpc(0);
                            if (logBees)Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        break;
                    }
                    beeData.targetEnemy = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1);
                    if (logBees) Script.Logger.LogDebug("case2: targetEnemy is: " + beeData.targetEnemy);
                    if (beeData.targetEnemy != null)
                    {
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.agent.acceleration = 16f;
                        if (!EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1))
                        {
                            if (logBees)Script.Logger.LogDebug("case2: Checking LOS for enemies.");

                            __instance.lostLOSTimer += __instance.AIIntervalTime;
                            if (__instance.lostLOSTimer > 4.5f)
                            {
                                beeData.targetEnemy = null;
                                if (logBees)Script.Logger.LogDebug("case2: No target found.");
                                __instance.lostLOSTimer = 0;
                            }
                        }
                        else
                        {
                            __instance.wasInChase = true;
                            beeData.lastKnownEnemyPosition = beeData.targetEnemy.transform.position;
                            __instance.lostLOSTimer = 0;
                        }
                    }
                    __instance.agent.acceleration = 13f;
                    if (!__instance.searchForHive.inProgress)
                    {
                        if (logBees) Script.Logger.LogDebug("case2: set new search for hive");
                        if (__instance.wasInChase)
                        {
                            __instance.StartSearch(beeData.lastKnownEnemyPosition, __instance.searchForHive);
                            if (logBees) Script.Logger.LogDebug("case2: Started search for hive.");
                        }
                        else
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                            if (logBees) Script.Logger.LogDebug("case2: Started search for hive.");
                        }
                    }
                    break;
            }
        }

        public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
        {
            if (beeList[__instance].timeSinceHittingEnemy > 1.6f)
            {
                mainscript2.HitEnemy(1, null, playHitSFX: true);
                beeList[__instance].timeSinceHittingEnemy = 0f;
            }
            else
            {
                beeList[__instance].timeSinceHittingEnemy += Time.deltaTime;*/
            }
        
    }
}
