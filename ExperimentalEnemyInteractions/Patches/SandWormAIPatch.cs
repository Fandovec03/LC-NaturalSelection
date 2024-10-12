using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;
using GameNetcodeStuff;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float refreshCDtime = 0.4f;
        static EnemyAI? closestEnemy = null;
        static EnemyAI? targetEnemy = null;
        static float emergeTimer = 0f;
        static float maxEmergeTime = 20f;
        static float minEmergeCooldown = 30f;
        static float lastEmergeTime = -minEmergeCooldown;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormUpdatePatch(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return;

            enemyList = EnemyAIPatch.GetOutsideEnemyList(__instance);
            closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (closestEnemy != null && (closestEnemy is ForestGiantAI || closestEnemy is BaboonBirdAI || closestEnemy is MouthDogAI || closestEnemy is RadMechAI))
            {
                targetEnemy = closestEnemy;
            }

            if (targetEnemy != null)
            {
                if (__instance.roamMap.inProgress)
                {
                    __instance.StopSearch(__instance.roamMap);
                }
                __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                __instance.moveTowardsDestination = true;
                __instance.movingTowardsTargetPlayer = false;

                float distanceToTarget = Vector3.Distance(targetEnemy.transform.position, __instance.transform.position);
                if (distanceToTarget < 15f && !__instance.emerged && !__instance.inEmergingState && Time.time - lastEmergeTime > minEmergeCooldown)
                {
                    __instance.StartEmergeAnimation();
                    lastEmergeTime = Time.time;
                }

                if (__instance.emerged)
                {
                    emergeTimer += Time.deltaTime;
                    if (emergeTimer > maxEmergeTime || distanceToTarget > 20f)
                    {
                        __instance.SetInGround();
                        emergeTimer = 0f;
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: false);
                        __instance.agent.speed = 8f;
                    }
                }
            }
            else
            {
                if (!__instance.roamMap.inProgress && !__instance.emerged && !__instance.inEmergingState)
                {
                    __instance.StartSearch(__instance.transform.position, __instance.roamMap);
                }
                __instance.agent.speed = 4f;
            }
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance) 
        {
            if (__instance.isEnemyDead) return;

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    if (!__instance.emerged && !__instance.inEmergingState)
                    {
                        if (UnityEngine.Random.value < 0.1f && Time.time - lastEmergeTime > minEmergeCooldown)
                        {
                            __instance.StartEmergeAnimation();
                            lastEmergeTime = Time.time;
                        }
                        else if (closestEnemy != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 20f)
                        {
                            __instance.SwitchToBehaviourState(1);
                            __instance.chaseTimer = 0;
                        }
                    }
                    break;
                case 1:
                    if (__instance.roamMap.inProgress)
                    {
                        __instance.StopSearch(__instance.roamMap);
                    }
                    if (targetEnemy == null || Vector3.Distance(__instance.gameObject.transform.position, targetEnemy.transform.position) > 25f)
                    {
                        __instance.SwitchToBehaviourState(0);
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: !__instance.emerged);
                        if (!__instance.emerged && !__instance.inEmergingState && __instance.chaseTimer < 1.5f && 
                            Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) < 10f &&
                            Time.time - lastEmergeTime > minEmergeCooldown)
                        {
                            __instance.StartEmergeAnimation();
                            lastEmergeTime = Time.time;
                        }
                    }
                    break;
            }
        }
    }
}
