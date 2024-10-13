using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;
using GameNetcodeStuff;
using System.Diagnostics;
using Mono.Cecil.Cil;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static List<Type> targetedTypes = new List<Type>();
        static float refreshCDtime = 0.5f;
        static EnemyAI? closestEnemy = null;
        static EnemyAI? targetEnemy = null;
        static bool targetingEntity = false;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void SandWormStartPatch()
        {
            targetedTypes.Add(typeof(BaboonBirdAI));
            targetedTypes.Add(typeof(ForestGiantAI));
            targetedTypes.Add(typeof(MouthDogAI));
            targetedTypes.Add(typeof(BushWolfEnemy));
            targetedTypes.Add(typeof(RadMechAI));
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool SandWormPrefixUpdatePatch(SandWormAI __instance)
        {
            if (refreshCDtime <= 0)
            {
                enemyList = EnemyAIPatch.filterEnemyList(EnemyAIPatch.GetOutsideEnemyList(__instance), targetedTypes, __instance);
                refreshCDtime = 0.5f;
            }
            if (refreshCDtime > 0)
            {
                refreshCDtime -= Time.deltaTime;
            }
            //if (Script.BoundingConfig.enableLeviathan.Value != true) return;
            if (targetingEntity)
            {
                Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Prefix/1/ targetingEntity: " + targetingEntity + ", target: " + targetEnemy);
                return false;
            }
            Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Prefix/2/ targetingEntity: " + targetingEntity + ", target: " + targetEnemy);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            if (Script.BoundingConfig.enableLeviathan.Value != true) return;
            Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Postfix targetingEntity: " + targetingEntity);
            if (targetingEntity == false) return;
            if (!targetingEntity && !__instance.movingTowardsTargetPlayer)
            {
                if (__instance.creatureSFX.isPlaying)
                {
                    __instance.creatureSFX.Stop();
                    Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": stopping Sounds");
                }
            }
            if (targetingEntity)
            {
                if (!__instance.IsOwner)
                {
                    return;
                }
                if (__instance != null)
                {
                    if (__instance.updateDestinationInterval >= 0f)
                    {
                        __instance.updateDestinationInterval -= Time.deltaTime;
                    }
                    else
                    {                        
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": calling DoAIInterval");
                        __instance.DoAIInterval();
                        __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                    }
                }
                if (__instance != null)
                {
                    if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                    {
                        int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                        __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                        __instance.creatureSFX.Play();
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Started playing sounds");

                    }
                    if (targetEnemy == null)
                    {
                        Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": TargetEnemy is null! TargetingEntity set to false /Trigger 1/");
                        targetingEntity = false;
                        return;
                    }
                    if (Vector3.Distance(targetEnemy.transform.position, __instance.transform.position) > 22f)
                    {
                        __instance.chaseTimer += Time.deltaTime;
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": updated chaseTimer: " + __instance.chaseTimer);
                    }
                    else
                    {
                        __instance.chaseTimer = 0f;
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Reset chasetimer");
                    }
                    if (__instance.chaseTimer > 6f)
                    {
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Chasing for too long. targetEnemy set to null");
                        targetEnemy = null;
                    }
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance)
        {
            //if (Script.BoundingConfig.enableLeviathan.Value != true) return true;

            Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: checking chaseTimer: " + __instance.chaseTimer);

            if (!targetingEntity)
            {
                if (!__instance.emerged && !__instance.inEmergingState)
                {
                    closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);
                    __instance.agent.speed = 4f;
                    if (debugMode) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: assigned " + closestEnemy + " as closestEnemy");

                    if (closestEnemy != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 15f)
                    {
                        __instance.SetDestinationToPosition(closestEnemy.transform.position);
                        targetingEntity = true;
                        targetEnemy = closestEnemy;
                        __instance.chaseTimer = 0;
                        if (debugMode) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set targetingEntity to " + targetingEntity + " and reset chaseTimer: " + __instance.chaseTimer);
                        return;
                    }
                }
            }
            if (targetingEntity)
            {
                if (targetEnemy == null)
                {
                    Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": targetEnemy is at null. Setting targetingEntity to false /Trigger 2/");
                    targetingEntity = false;
                    return;
                }
                targetEnemy = closestEnemy;
                if (debugMode) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set " + targetEnemy + " as targetEnemy");
                if (targetEnemy != null)
                {
                    if (Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) > 19f)
                    {
                        targetEnemy = null;
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: TargetEnemy too far! set to null");
                        return;
                    }
                    __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                    Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set destitantion to " + targetEnemy);

                    if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                    {
                        Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Emerging!");
                        __instance.StartEmergeAnimation();
                    }
                }
            }
        }
    }
}
