using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.EnemyPatches
{

    class ExtendedSandWormAIData
    {
        public float refreshCDtime = 0.5f;
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public bool targetingEntity = false;
    }

    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static List<Type> targetedTypes = new List<Type>();
        //static float refreshCDtime = 0.5f;
        //static EnemyAI? closestEnemy = null;
        //static EnemyAI? targetEnemy = null;
        //static bool targetingEntity = false;
        static bool debugSandworm = Script.BoundingConfig.debugSandworms.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;

        static Dictionary<SandWormAI, ExtendedSandWormAIData> sandworms = [];

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void SandWormStartPatch(SandWormAI __instance)
        {
            if (!sandworms.ContainsKey(__instance))
            {
                sandworms.Add(__instance, new ExtendedSandWormAIData());

                if (targetedTypes.Count != 0) return;

                targetedTypes.Add(typeof(BaboonBirdAI));
                targetedTypes.Add(typeof(ForestGiantAI));
                targetedTypes.Add(typeof(MouthDogAI));
                targetedTypes.Add(typeof(BushWolfEnemy));
                targetedTypes.Add(typeof(RadMechAI));
            }
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool SandWormPrefixUpdatePatch(SandWormAI __instance)
        {
            ExtendedSandWormAIData mySandwormFields = sandworms[__instance];

            if (mySandwormFields.refreshCDtime <= 0)
            {
                enemyList = EnemyAIPatch.filterEnemyList(EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance),__instance), targetedTypes, __instance);
                mySandwormFields.refreshCDtime = 0.2f;
            }
            if (mySandwormFields.refreshCDtime > 0)
            {
                mySandwormFields.refreshCDtime -= Time.deltaTime;
            }
            //if (Script.BoundingConfig.enableLeviathan.Value != true) return;
            if (mySandwormFields.targetingEntity && !__instance.emerged && !__instance.inEmergingState)
            {
                if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Prefix/1/ targetingEntity: " + mySandwormFields.targetingEntity + ", target: " + mySandwormFields.targetEnemy);
                return false;
            }
            if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Prefix/2/ targetingEntity: " + mySandwormFields.targetingEntity + ", target: " + mySandwormFields.targetEnemy);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            ExtendedSandWormAIData mySandwormFields = sandworms[__instance];

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Postfix targetingEntity: " + mySandwormFields.targetingEntity);
            if (!mySandwormFields.targetingEntity && !__instance.movingTowardsTargetPlayer)
            {
                if (__instance.creatureSFX.isPlaying)
                {
                    __instance.creatureSFX.Stop();
                    if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": stopping Sounds");
                }
            }
            if (mySandwormFields.targetingEntity)
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
                        if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": calling DoAIInterval");
                        __instance.DoAIInterval();
                        __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                    }
                }
                if (__instance != null)
                {
                    if (!__instance.creatureSFX.isPlaying)
                    {
                        int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                        __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                        __instance.creatureSFX.Play();
                        if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Started playing sounds");

                    }
                    if (!mySandwormFields.targetEnemy)
                    {
                        if (debugSandworm) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": TargetEnemy is null! TargetingEntity set to false /Trigger 1/");
                        mySandwormFields.targetingEntity = false;
                        return;
                    }
                    if (Vector3.Distance(mySandwormFields.targetEnemy.transform.position, __instance.transform.position) > 22f)
                    {
                        __instance.chaseTimer += Time.deltaTime;
                        if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": updated chaseTimer: " + __instance.chaseTimer);
                    }
                    else
                    {
                        __instance.chaseTimer = 0f;
                        if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Reset chasetimer");
                    }
                    if (__instance.chaseTimer > 6f)
                    {
                        if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Chasing for too long. targetEnemy set to null");
                        mySandwormFields.targetEnemy = null;
                    }
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static void SandWormDoAIIntervalPatch(SandWormAI __instance)
        {
            //if (Script.BoundingConfig.enableLeviathan.Value != true) return true;

            ExtendedSandWormAIData mySandwormFields = sandworms[__instance];
            if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: checking chaseTimer: " + __instance.chaseTimer);

            if (!mySandwormFields.targetingEntity)
            {
                if (!__instance.emerged && !__instance.inEmergingState)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    mySandwormFields.closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, mySandwormFields.closestEnemy, __instance);
#pragma warning restore CS8604 // Possible null reference argument.

                    __instance.agent.speed = 4f;
                    if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: assigned " + mySandwormFields.closestEnemy + " as closestEnemy");

                    if (mySandwormFields.closestEnemy != null && Vector3.Distance(__instance.transform.position, mySandwormFields.closestEnemy.transform.position) < 15f)
                    {
                        __instance.SetDestinationToPosition(mySandwormFields.closestEnemy.transform.position);
                        mySandwormFields.targetingEntity = true;
                        mySandwormFields.targetEnemy = mySandwormFields.closestEnemy;
                        __instance.chaseTimer = 0;
                        if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set targetingEntity to " + mySandwormFields.targetingEntity + " and reset chaseTimer: " + __instance.chaseTimer);
                        return;
                    }
                }
            }
            if (mySandwormFields.targetingEntity)
            {
                if (mySandwormFields.targetEnemy == null || mySandwormFields.targetEnemy.isEnemyDead)
                {
                    if (debugSandworm) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": targetEnemy is at null or dead. Setting targetingEntity to false /Trigger 2/");
                    mySandwormFields.targetEnemy = null;
                    mySandwormFields.targetingEntity = false;
                    return;
                }
                mySandwormFields.targetEnemy = mySandwormFields.closestEnemy;
                if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set " + mySandwormFields.targetEnemy + " as targetEnemy");
                if (mySandwormFields.targetEnemy != null)
                {
                    if (Vector3.Distance(__instance.transform.position, mySandwormFields.targetEnemy.transform.position) > 19f)
                    {
                        mySandwormFields.targetEnemy = null;
                        if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: TargetEnemy too far! set to null");
                        return;
                    }
                    if (!__instance.emerged && !__instance.inEmergingState)
                    {
                        __instance.SetDestinationToPosition(mySandwormFields.targetEnemy.transform.position, checkForPath: true);
                        if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Set destitantion to " + mySandwormFields.targetEnemy);
                    }
                    if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, mySandwormFields.targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                    {
                        if (debugSandworm) Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: Emerging!");
                        mySandwormFields.targetingEntity = false;
                        __instance.StartEmergeAnimation();
                    }
                }
            }
        }
    }
}
