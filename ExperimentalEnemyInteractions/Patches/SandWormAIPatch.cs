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
        static float refreshCDtime = 0.5f;
        static EnemyAI? closestEnemy = null;
        static EnemyAI? targetEnemy = null;
        static bool targetingEntity = false;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void SandWormUpdatePatch(SandWormAI __instance)
        {
           
                enemyList = EnemyAIPatch.GetOutsideEnemyList(__instance);

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            //if (targetingEntity == false) return;
            if (!targetingEntity)
            {
                __instance.creatureSFX.Stop();
            }
            if (targetingEntity)
            {
                if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                {
                    int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                    __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                    __instance.creatureSFX.Play();
                }
                if (!__instance.IsOwner)
                {
                    return;
                }
                if (targetEnemy != null)
                {
                    if (Vector3.Distance(targetEnemy.transform.position, __instance.transform.position) > 22f)
                    {
                        __instance.chaseTimer += Time.deltaTime;
                    }
                    targetingEntity = false;
                }
                else
                {
                    __instance.chaseTimer = 0f;
                }
                if (__instance.chaseTimer > 6f)
                {
                    targetEnemy = null;
                }
            }
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance)
        {
            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (!targetingEntity)
            {
                if (!__instance.emerged && !__instance.inEmergingState)
                {
                    closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);
                    __instance.agent.speed = 4f;
                    if (closestEnemy != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 15f)
                    {
                        __instance.SetDestinationToPosition(closestEnemy.transform.position);
                        targetingEntity = true;
                        __instance.chaseTimer = 0;
                    }
                }
            }
            if (targetingEntity)
            {
                targetEnemy = closestEnemy;

                if (Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) > 19f)
                {
                    targetEnemy = null;
                }
                if (targetEnemy == null)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Emulating SwitchState 0");
                    targetingEntity = false;
                }
                __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Emerging!");
                    __instance.StartEmergeAnimation();
                }
            }
        }
    }
}
