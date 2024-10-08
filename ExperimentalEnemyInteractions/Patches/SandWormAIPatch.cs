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

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormUpdatePatch(SandWormAI __instance)
        {
            enemyList = EnemyAIPatch.GetOutsideEnemyList(__instance);
            closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (closestEnemy is ForestGiantAI || closestEnemy is BaboonBirdAI || closestEnemy is MouthDogAI || closestEnemy is BaboonBirdAI && closestEnemy is RadMechAI)
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

                if (Vector3.Distance(targetEnemy.transform.position, __instance.transform.position) < 10f)
                {
                    __instance.StartEmergeAnimation();
                }

                if (__instance.emerged)
                {
                    __instance.moveTowardsDestination = false;
                    __instance.movingTowardsTargetPlayer = false;
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance) 
        {
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    if (!__instance.emerged && !__instance.inEmergingState)
                    {
                        if (!__instance.roamMap.inProgress)
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.roamMap);
                        }
                    __instance.agent.speed = 4f;
                    if (closestEnemy != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 15f)
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
                    targetEnemy = closestEnemy;
                    if (Vector3.Distance(__instance.gameObject.transform.position, targetEnemy.transform.position) > 19f)
                    {
                        targetEnemy = null;
                    }
                    if (targetEnemy == null)
                    {
                        __instance.SwitchToBehaviourState(0);
                        break;
                    }
                    __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                    if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position,targetEnemy.transform.position) < 4f &&  !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f && UnityEngine.Random.Range(0,100) < 17))
                    {
                        __instance.StartEmergeAnimation();
                    }
                    break;
            }
        }
    }
}
