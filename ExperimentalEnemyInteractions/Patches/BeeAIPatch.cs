using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    class BeeValues
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public Vector3 lastKnownEnemyPosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public float timeSinceHittingEnemy = 0f;
        public float LostLOSOfEnemy = 0f;
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static bool logBees = Script.BoundingConfig.debugRedBees.Value;
        static float UpdateTimer;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            if (!beeList.ContainsKey(__instance))
            {
                beeList.Add(__instance, new BeeValues());
            }
            BeeValues beeData = beeList[__instance];
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void PrefixUpdatePatch(RedLocustBees __instance)
        {

        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(RedLocustBees __instance)
        {
            if (UpdateTimer <= 0f)
            {
                enemyList = EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance);
                UpdateTimer = 0.20f;
            }
            else
            {
                UpdateTimer -= Time.deltaTime;
            }
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        __instance.SetBeeParticleMode(0);
                    }
                    break;
                case 1:
                    {
                        __instance.SetBeeParticleMode(1);
                    }
                    break;
                case 2:
                    {
                        __instance.SetBeeParticleMode(2);
                    }
                    break;
            }
        }
            [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            if (beeData.targetEnemy != null && __instance.movingTowardsTargetPlayer)
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

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    EnemyAI? LOSenemy = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1).Keys[0];
                    if (logBees) Script.Logger.LogDebug("case0: Checked LOS for enemies. Enemy found: " + LOSenemy);

                    if (__instance.wasInChase)
                    {
                        __instance.wasInChase = false;
                    }
                    if (Vector3.Distance(__instance.transform.position, __instance.lastKnownHivePosition) > 2f)
                    {
                        __instance.SetDestinationToPosition(__instance.lastKnownHivePosition);
                    }
                    if (__instance.IsHiveMissing())
                    {
                        __instance.SwitchToBehaviourState(2);
                        beeData.customBehaviorStateIndex = 2;
                        if (logBees) Script.Logger.LogDebug("case0: HIVE IS MISSING! CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        break;
                    }
                    if (LOSenemy != null && Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance /*&& Vector3.Distance(__instance.targetPlayer.transform.position, __instance.hive.transform.position) < Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position)*/)
                    {
                        __instance.SetDestinationToPosition(LOSenemy.transform.position, true);
                        if (logBees) Script.Logger.LogDebug("case0: Moving towards " + LOSenemy);

                        beeData.customBehaviorStateIndex = 1;
                        __instance.SwitchToBehaviourState(1);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                        if (logBees) Script.Logger.LogDebug("case0: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                    }
                    break;
                case 1:
                    if (__instance.targetPlayer != null && __instance.movingTowardsTargetPlayer) return;
                    if (beeData.targetEnemy == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {
                        beeData.targetEnemy = null;
                        __instance.wasInChase = false;
                        if (__instance.IsHiveMissing())
                        {
                            beeData.customBehaviorStateIndex = 2;
                            __instance.SwitchToBehaviourState(2);
                            if (logBees) Script.Logger.LogDebug("case1: HIVE IS MISSING! CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourState(0);
                            if (logBees) Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                    }
                    break;
                case 2: // Currently whenever bees go to state 2 they will ignore players and stop reporting into logs. Disabled for now
                    if (__instance.targetPlayer != null && __instance.movingTowardsTargetPlayer) return;
                    if (__instance.IsHivePlacedAndInLOS())
                    {
                        if (__instance.wasInChase)
                        {
                            __instance.wasInChase = false;
                        }
                        __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;

                        if (logBees) Script.Logger.LogDebug("case2: IsHivePlacedAndInLOS triggered");
                        EnemyAI? enemyAI2 = null;
                        Collider[] collisionArray = Physics.OverlapSphere(__instance.hive.transform.position, (float)__instance.defenseDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Collide);

                        if (collisionArray != null && collisionArray.Length != 0)
                        {
                            for (int i = 0; i < collisionArray.Length; i++)
                            {
                                if (collisionArray[i].gameObject.tag == "Enemy")
                                {
                                    enemyAI2 = collisionArray[i].GetComponent<EnemyAI>();
                                    if (logBees) Script.Logger.LogDebug("case2: CollisionArray triggered. Enemy found");
                                    break;
                                }
                            }
                        }
                        if (logBees) Script.Logger.LogDebug("case2: checked LOS for enemies");

                        if (enemyAI2 != null && Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(enemyAI2.transform.position, true);
                            if (logBees) Script.Logger.LogDebug("case2: Moving towards: " + enemyAI2);
                            beeData.customBehaviorStateIndex = 1;
                            __instance.SwitchToBehaviourState(1);
                            __instance.syncedLastKnownHivePosition = false;
                            __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                            if (logBees) Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourState(0);
                            if (logBees) Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        break;
                    }

                    
                    bool flag = false;
                    SortedList<EnemyAI, float> priorityEnemies = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1f);
                    KeyValuePair<EnemyAI, float> closestToHive = new KeyValuePair<EnemyAI, float>();
                    foreach (KeyValuePair<EnemyAI, float> enemyPair in priorityEnemies)
                    {
                        if (closestToHive.Key == null)
                        {
                            closestToHive = enemyPair;
                        }
                        if (enemyPair.Value > closestToHive.Value)
                        {
                            closestToHive = enemyPair;
                        }
                    }
                    if (closestToHive.Key != null && closestToHive.Value < Vector3.Distance(closestToHive.Key.transform.position, __instance.hive.transform.position))
                    {
                        flag = true;
                        __instance.wasInChase = false;
                        beeData.targetEnemy = closestToHive.Key;
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position);
                        __instance.StopSearch(__instance.searchForHive);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                        break;
                    }
                    if (beeData.targetEnemy != null)
                    {
                        if (!flag && EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 2f).Keys.First() != null)
                        {
                            beeData.LostLOSOfEnemy += Time.deltaTime;
                            if (beeData.LostLOSOfEnemy >= 4.5f)
                            {
                                beeData.targetEnemy = null;
                                beeData.LostLOSOfEnemy = 0f;
                            }
                        }
                        else
                        {
                            __instance.wasInChase = true;
                            beeData.lastKnownEnemyPosition = beeData.targetEnemy.transform.position;
                            beeData.LostLOSOfEnemy = 0f;
                        }
                        break;
                    }
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
            if (beeList.ContainsKey(__instance))
            {
                if (beeList[__instance].timeSinceHittingEnemy > 1.6f && __instance.currentBehaviourStateIndex > 0)
                {
                    mainscript2.HitEnemy(1, null, playHitSFX: true);
                    beeList[__instance].timeSinceHittingEnemy = 0f;
                }
                else
                {
                    beeList[__instance].timeSinceHittingEnemy += Time.deltaTime;
                }
            }
        }
    }
}
