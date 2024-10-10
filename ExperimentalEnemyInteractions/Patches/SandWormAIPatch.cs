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
        static bool OverrideSounds = false;

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
        [HarmonyTranspiler]

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundCode = false;
            var startIndex = -1;
            var endIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
            }
        }


        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormUpdatePatch(SandWormAI __instance)
        {
            if (refreshCDtime <= 0)
            {
                enemyList = EnemyAIPatch.filterEnemyList(EnemyAIPatch.GetOutsideEnemyList(__instance), targetedTypes, __instance);
                refreshCDtime = 0.5f;
            }
            if (refreshCDtime > 0)
            {
                refreshCDtime-= Time.deltaTime;
            }
            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            //if (targetingEntity == false) return;
            if (!targetingEntity && !__instance.movingTowardsTargetPlayer)
            {
                if (__instance.creatureSFX.isPlaying)
                {
                    __instance.creatureSFX.Stop();
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": stopping Sounds");
                }
            }
            if (targetingEntity)
            {
                if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                {
                    int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                    __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                    __instance.creatureSFX.Play();
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Started playing sounds");
                    
                }
                if (__instance.inEmergingState)
                {
                    
                }
                if (targetEnemy == null)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": TargetEnemy is null! TargetingEntity set to false /Trigger 1/");
                    targetingEntity = false;
                }
                if (Vector3.Distance(targetEnemy.transform.position, __instance.transform.position) > 22f)
                {
                    __instance.chaseTimer += Time.deltaTime;
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": updated chaseTimer: " + __instance.chaseTimer);
                }
                else
                {
                    __instance.chaseTimer = 0f;
                    //Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Reset chasetimer");
                }
                if (__instance.chaseTimer > 6f)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Chasing for too long. targetEnemy set to null");
                    targetEnemy = null;
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance)
        {
            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": checking chaseTimer: " + __instance.chaseTimer);

            if (!targetingEntity)
            {
                if (!__instance.emerged && !__instance.inEmergingState)
                {
                    closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);
                    __instance.agent.speed = 4f;
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": assigned " + closestEnemy + " as closestEnemy");

                    if (closestEnemy != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 15f)
                    {
                        __instance.SetDestinationToPosition(closestEnemy.transform.position);
                        targetingEntity = true;
                        __instance.chaseTimer = 0;
                        Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Set targetingEntity to " + targetingEntity + " and reset chaseTimer: " + __instance.chaseTimer);
                    }
                }
            }
            if (targetingEntity)
            {
                targetEnemy = closestEnemy;
                 Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Set " + targetEnemy + " as targetEnemy");

                if (Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) > 19f)
                {
                    targetEnemy = null;
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": TargetEnemy too far! set to null");
                }
                if (targetEnemy == null)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": targetEnemy is at null. Setting targetingEntity to false /Trigger 2/");
                    targetingEntity = false;
                }
                __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Set destitantion to " + targetEnemy);

                if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Emerging!");
                    __instance.StartEmergeAnimation();
                }
            }
        }
    }
}
