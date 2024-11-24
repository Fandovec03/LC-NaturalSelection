using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExperimentalEnemyInteractions.EnemyPatches
{

    class ExtendedSandWormAIData
    {
        public float refreshCDtime = 0.5f;
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
    }

    [HarmonyPatch]
    class ReversepatchWorm
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EnemyAI), "Update")]
        public static void ReverseUpdate(SandWormAI instance)
        {
            //Script.Logger.LogInfo("Reverse patch triggered");
        }
    }
    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static List<Type> targetedTypes = new List<Type>();
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
            ExtendedSandWormAIData SandwormData = sandworms[__instance];

            if (SandwormData.refreshCDtime <= 0)
            {
                enemyList = EnemyAIPatch.filterEnemyList(EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance),__instance), targetedTypes, __instance);
                SandwormData.refreshCDtime = 0.2f;
            }
            if (SandwormData.refreshCDtime > 0)
            {
                SandwormData.refreshCDtime -= Time.deltaTime;
            }

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                {
                    return true;
                }
                case 1:
                {
                    if (sandworms[__instance].targetEnemy != null)
                    {
                        if (debugSandworm && debugSpam) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " Update: returning false");
                            ReversepatchWorm.ReverseUpdate(__instance);
                            if (__instance.updateDestinationInterval >= 0)
                            {
                                __instance.updateDestinationInterval -= Time.deltaTime;
                            }
                            else
                            {
                                __instance.updateDestinationInterval = __instance.AIIntervalTime + Random.Range(-0.015f, 0.015f);
                                __instance.DoAIInterval();
                            }
                            return false;
                    }
                    return true;
                }
            }

            if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Prefix/2/ targetEnemy: " + SandwormData.targetEnemy + ", target: " + SandwormData.targetEnemy + ", behavior state: " + __instance.currentBehaviourStateIndex);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            ExtendedSandWormAIData SandwormData = sandworms[__instance];

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;
            /*
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
            }*/

            if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Postfix targetEnemy: " + SandwormData.targetEnemy);


            switch(__instance.currentBehaviourStateIndex)
            {
                case 0:
                {
                    break;
                }
                case 1:
                {
                    if (__instance.targetPlayer == null)
                    {
                        if (__instance.stateLastFrame != __instance.currentBehaviourStateIndex)
                        {
                            __instance.stateLastFrame = __instance.currentBehaviourStateIndex;
                            __instance.chaseTimer = 0f;
                        }
                        if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                        {
                            int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                            __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                            __instance.creatureSFX.Play();
                            if (debugSandworm) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Started playing sounds");
                        }
                        if (!__instance.IsOwner)
                        {
                            break;
                        }
                        if (SandwormData.targetEnemy == null)
                        {
                            if (debugSandworm) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": TargetEnemy is null or player is closer than TargetEnemy! BehaviorState set to 0 /Trigger 1/");
                            __instance.SwitchToBehaviourState(0);
                            break;
                        }
                        if (Vector3.Distance(SandwormData.targetEnemy.transform.position, __instance.transform.position) > 22f)
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
                            SandwormData.targetEnemy = null;
                            __instance.SwitchToBehaviourState(0);
                        }
                    }
                    break;
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool SandWormDoAIIntervalPrefix(SandWormAI __instance)
        {
            if (Script.BoundingConfig.enableLeviathan.Value != true) return true;

            ExtendedSandWormAIData SandwormData = sandworms[__instance];
            if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + "DoAIInterval: checking chaseTimer: " + __instance.chaseTimer);

            /*if (!mySandwormFields.targetingEntity)
            {
                if (!__instance.emerged && !__instance.inEmergingState)
                {
                    mySandwormFields.closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, mySandwormFields.closestEnemy, __instance);

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
            }*/
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0: {
                    return true;
                }
                case 1: {
                    if (__instance.moveTowardsDestination)
                    {
                        __instance.agent.SetDestination(__instance.destination);
                    }
                    __instance.SyncPositionToClients();
                    if (SandwormData.targetEnemy != null)
                    {
                        if (debugSandworm && debugSpam) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: returning false");
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void SandWormDoAIIntervalPostfix(SandWormAI __instance)
        {
            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            ExtendedSandWormAIData SandwormData = sandworms[__instance];
            if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: checking chaseTimer: " + __instance.chaseTimer);

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                {
                    if (!__instance.emerged && !__instance.inEmergingState)
                    {
                            SandwormData.closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, SandwormData.closestEnemy, __instance);

                        __instance.agent.speed = 4f;
                        if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: assigned " + SandwormData.closestEnemy + " as closestEnemy");

                        if (SandwormData.closestEnemy != null && Vector3.Distance(__instance.transform.position, SandwormData.closestEnemy.transform.position) < 15f)
                        {
                            __instance.SetDestinationToPosition(SandwormData.closestEnemy.transform.position);
                            SandwormData.targetEnemy = SandwormData.closestEnemy;
                            __instance.SwitchToBehaviourState(1);
                            __instance.chaseTimer = 0;
                            if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: Set behaviorState to " + __instance.currentBehaviourStateIndex + " and reset chaseTimer: " + __instance.chaseTimer);
                            break;
                        }
                    }
                    break;
                }
                case 1:
                {
                    if (__instance.targetPlayer == null)
                    {
                        if (__instance.roamMap.inProgress)
                        {
                            __instance.StopSearch(__instance.roamMap);
                        }
                        if (SandwormData.targetEnemy == null || SandwormData.targetEnemy.isEnemyDead)
                        {
                            if (debugSandworm) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": targetEnemy is at null or dead. Setting targetingEntity to false /Trigger 2/");
                                SandwormData.targetEnemy = null;
                            __instance.SwitchToBehaviourState(0);
                            break;
                        }
                            SandwormData.targetEnemy = SandwormData.closestEnemy;
                        if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: Set " + SandwormData.targetEnemy + " as targetEnemy");
                        if (SandwormData.targetEnemy != null)
                        {
                            if (Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) > 19f)
                            {
                                    SandwormData.targetEnemy = null;
                                if (debugSandworm) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: TargetEnemy too far! set to null");
                                break;
                            }
                            if (!__instance.emerged && !__instance.inEmergingState)
                            {
                                __instance.SetDestinationToPosition(SandwormData.targetEnemy.transform.position, checkForPath: true);
                                if (debugSandworm && debugSpam) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: Set destitantion to " + SandwormData.targetEnemy);
                            }
                            if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                            {
                                if (debugSandworm) Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + " DoAIInterval: Emerging!");
                                __instance.StartEmergeAnimation();
                            }
                        }
                    }
                    break;
                }
            }
        }
    }
}
