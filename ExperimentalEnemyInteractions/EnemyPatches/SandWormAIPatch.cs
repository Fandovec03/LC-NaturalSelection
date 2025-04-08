using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using UnityEngine;
using LethalNetworkAPI;
using System.Linq;

namespace NaturalSelection.EnemyPatches
{
    class ExtendedSandWormAIData
    {
        public float refreshCDtime = 0.5f;
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        //public int targetingEntity = 0;
        public float clearEnemiesTimer = 0f;
        public List<EnemyAI> localEnemyList = new List<EnemyAI>();
    }

    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<Type> targetedTypes = new List<Type>();
        static bool debugSandworm = Script.BoundingConfig.debugSandworms.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool triggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;
        static List<string> blacklist = Script.BoundingConfig.sandwormBlacklist.Value.ToUpper().Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
        static LNetworkVariable<int> NetworkSandwormBehaviorState(SandWormAI instance)
        {
            string NWID = "NSSandwormBehaviorState" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariableInt(NWID);
        }

        static LNetworkVariable<bool> NetworkTargetingEntity(SandWormAI instance)
        {
            string NWID = "NSSandwormTargetingEntity" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariableBool(NWID);
        }

        static LNetworkVariable<bool> NetworkMovingTowardsPlayer(SandWormAI instance)
        {
            string NWID = "NSSandwormMovingTowardsPlayer" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariableBool(NWID);
        }

        static Dictionary<SandWormAI, ExtendedSandWormAIData> sandworms = [];

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void SandWormStartPatch(SandWormAI __instance)
        {
            if (!sandworms.ContainsKey(__instance))
            {
                sandworms.Add(__instance, new ExtendedSandWormAIData());

                if (targetedTypes.Count == 0 && Script.BoundingConfig.sandwormFilterTypes.Value == true)
                {
                    targetedTypes.Add(typeof(BaboonBirdAI));
                    targetedTypes.Add(typeof(ForestGiantAI));
                    targetedTypes.Add(typeof(MouthDogAI));
                    targetedTypes.Add(typeof(BushWolfEnemy));
                    targetedTypes.Add(typeof(RadMechAI));
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool SandWormPrefixUpdatePatch(SandWormAI __instance)
        {
            ExtendedSandWormAIData SandwormData = sandworms[__instance];
            Type type = __instance.GetType();

            if (__instance.IsOwner) NetworkMovingTowardsPlayer(__instance).Value = __instance.movingTowardsTargetPlayer;

            int targetingEntity = NetworkSandwormBehaviorState(__instance).Value;
            bool networkMovingTowardsPlayer = NetworkMovingTowardsPlayer(__instance).Value;

            if (SandwormData.refreshCDtime <= 0)
            {
                if (RoundManagerPatch.RequestUpdate(__instance) == true)
                {
                    List<EnemyAI> tempList = EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetCompleteList(__instance, true, 0), targetedTypes, blacklist, __instance).ToList();
                    RoundManagerPatch.ScheduleGlobalListUpdate(__instance, EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetCompleteList(__instance, true, 0), targetedTypes, blacklist, __instance));
                    //NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(__instance.GetType(), EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance, true, 0), __instance), targetedTypes, __instance));
                }
                if (__instance.IsOwner)
                {
                    SandwormData.localEnemyList = EnemyAIPatch.GetInsideOrOutsideEnemyList(NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type], __instance).ToList();
                    SandwormData.closestEnemy = EnemyAIPatch.FindClosestEnemy(NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type], SandwormData.closestEnemy, __instance);
                }
                SandwormData.refreshCDtime = 0.2f;
            }
            if (SandwormData.refreshCDtime > 0)
            {
                SandwormData.refreshCDtime -= Time.deltaTime;
            }

            switch (targetingEntity)
            {
                case 0:
                    if (__instance.inEmergingState || __instance.emerged)
                    {
                        return false;
                    }
                    break;
                case 1:
                {
                    if ((__instance.IsOwner && SandwormData.targetEnemy != null || !__instance.IsOwner && NetworkTargetingEntity(__instance).Value) && !networkMovingTowardsPlayer)
                    {
                        if (__instance.IsOwner)
                        {
                            if (SandwormData.targetEnemy != null) NetworkTargetingEntity(__instance).Value = true;
                            if (__instance.updateDestinationInterval >= 0f)
                            {
                                __instance.updateDestinationInterval -= Time.deltaTime;
                            }
                            else
                            {
                                if (debugSandworm && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " calling DoAIInterval");
                                __instance.DoAIInterval();
                                __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                            }
                        }

                        //Script.Logger.LogMessage($"{EnemyAIPatch.DebugStringHead(__instance)} Invoking originalUpdate");
                        try
                        {
                            ReversePatchAI.originalUpdate.Invoke(__instance);
                            //Script.Logger.LogMessage("Succesfully invoked originalUpdate");
                        }
                        catch (Exception e)
                        {
                            Script.Logger.LogError("failed invoking originalUpdate.");
                            Script.Logger.LogError(e);
                        }

                        return false;
                    }
                    break;
                }
            }

            if (debugSandworm && triggerFlag) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " Prefix/2/ targetEnemy: " + SandwormData.targetEnemy + ", target: " + SandwormData.targetEnemy);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            ExtendedSandWormAIData SandwormData = sandworms[__instance];
            int targetingEntity = NetworkSandwormBehaviorState(__instance).Value;
            bool networkMovingTowardsPlayer = NetworkMovingTowardsPlayer(__instance).Value;

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (debugSandworm && debugSpam) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} Postfix targetingEntity: {targetingEntity}");

            switch(targetingEntity)
            {
                case 0:
                {
                    if (!networkMovingTowardsPlayer && !__instance.inEmergingState && !__instance.emerged)
                    {
                        if (__instance.creatureSFX.isPlaying)
                        {
                            __instance.creatureSFX.Stop();
                            if (debugSandworm) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} stopping Sounds");
                        }
                    }
                }
                    break;
                case 1:
                    {
                        if (debugSandworm && SandwormData.closestEnemy != null && __instance.IsOwner) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} closestEnemy is {EnemyAIPatch.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /3/");
                        if (networkMovingTowardsPlayer) break;
                        if (!networkMovingTowardsPlayer)
                        {
                            if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                            {
                                int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                                __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                                __instance.creatureSFX.Play();
                                if (debugSandworm) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} Started playing sounds");

                            }
                            if (!__instance.IsOwner)    
                            {
                                break;
                            }
                            if (SandwormData.targetEnemy == null)
                            {
                                if (debugSandworm) Script.Logger.LogError($"{EnemyAIPatch.DebugStringHead(__instance)} TargetEnemy is null! TargetingEntity set to false /Trigger 1/");
                                NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                break;
                            }
                            if (Vector3.Distance(SandwormData.targetEnemy.transform.position, __instance.transform.position) > 22f)
                            {
                                __instance.chaseTimer += Time.deltaTime;
                                if (debugSandworm && debugSpam) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} updated chaseTimer: {__instance.chaseTimer}");
                            }
                            else
                            {
                                __instance.chaseTimer = 0f;
                                if (debugSandworm) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} Reset chasetimer");
                            }
                            if (__instance.chaseTimer > 6f)
                            {
                                if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} Chasing for too long. targetEnemy set to null");
                                NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                SandwormData.targetEnemy = null;
                                NetworkTargetingEntity(__instance).Value = false;
                            }
                        }
                    }
                    break;
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool SandWormDoAIIntervalPrefix(SandWormAI __instance)
        {
            //if (Script.BoundingConfig.enableLeviathan.Value != true) return true;
            ExtendedSandWormAIData SandwormData = sandworms[__instance];
            Type type = __instance.GetType();
            int targetingEntity = NetworkSandwormBehaviorState(__instance).Value;
            if (debugSandworm && debugSpam && triggerFlag) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: checking chaseTimer: {__instance.chaseTimer}");

            switch (targetingEntity)
            {
                case 0:
                    {
                        if (!__instance.emerged && !__instance.inEmergingState)
                        {
                            SandwormData.closestEnemy = EnemyAIPatch.FindClosestEnemy(SandwormData.localEnemyList, SandwormData.closestEnemy, __instance);
                            __instance.agent.speed = 4f;
                            if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: assigned {SandwormData.closestEnemy} as closestEnemy");
                            if (SandwormData.closestEnemy != null && Vector3.Distance(__instance.transform.position, SandwormData.closestEnemy.transform.position) < 15f)
                            {
                                if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} closestEnemy is {EnemyAIPatch.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /1/");
                                __instance.SetDestinationToPosition(SandwormData.closestEnemy.transform.position);
                                NetworkSandwormBehaviorState(__instance).Value = 1;
                                //__instance.SwitchToBehaviourState(1);
                                SandwormData.targetEnemy = SandwormData.closestEnemy;
                                NetworkTargetingEntity(__instance).Value = true;
                                __instance.chaseTimer = 0;
                                if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: Set targetingEntity to {targetingEntity} and reset chaseTimer: {__instance.chaseTimer}");
                                break;
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (debugSandworm && SandwormData.closestEnemy != null) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} closestEnemy is {EnemyAIPatch.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /2/");
                        if (__instance.movingTowardsTargetPlayer) break;
                        if (SandwormData.targetEnemy == null || SandwormData.targetEnemy.isEnemyDead)
                        {
                            if (debugSandworm) Script.Logger.LogError($"{EnemyAIPatch.DebugStringHead(__instance)}: targetEnemy is at null or dead. Setting targetingEntity to false /Trigger 2/");
                            SandwormData.targetEnemy = null;
                            NetworkTargetingEntity(__instance).Value = false;
                            NetworkSandwormBehaviorState(__instance).Value = 0;
                            //__instance.SwitchToBehaviourState(0);
                            break;
                        }
                        SandwormData.targetEnemy = SandwormData.closestEnemy;
                        if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: Set {SandwormData.targetEnemy} as targetEnemy");
                        if (SandwormData.targetEnemy != null)
                        {
                            if (Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) > 19f)
                            {
                                SandwormData.targetEnemy = null;
                                NetworkTargetingEntity(__instance).Value = false;
                                if (debugSandworm) Script.Logger.LogInfo($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: TargetEnemy too far! set to null");
                                break;
                            }
                            if (!__instance.emerged && !__instance.inEmergingState)
                            {
                                __instance.SetDestinationToPosition(SandwormData.targetEnemy.transform.position, checkForPath: true);
                                if (debugSandworm && debugSpam) Script.Logger.LogDebug($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: Set destitantion to {SandwormData.targetEnemy}");
                            }
                            if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                            {
                                Script.Logger.LogMessage($"{EnemyAIPatch.DebugStringHead(__instance)} DoAIInterval: Emerging!");
                                NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                __instance.StartEmergeAnimation();
                            }
                        }
                    }
                break;
            }

            if (!__instance.movingTowardsTargetPlayer && __instance.currentBehaviourStateIndex == 1)
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();
                return false;
            }
            return true;
        }
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool OnCollideWithPlayeryPatch(SandWormAI __instance, Collider other)
        { 
            if (other != null)
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (__instance.emerged && !player.isInHangarShipRoom && !StartOfRound.Instance.shipIsLeaving && Script.BoundingConfig.sandwormDoNotEatPlayersInsideLeavingShip.Value)
                {
                    Script.Logger.LogInfo("Prevented sandworm from eating player inside ship");
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        static bool OnCollideWithEnemyPatch(SandWormAI __instance, Collider other, EnemyAI? enemyScript)
        {
            if (Script.BoundingConfig.sandwormCollisionOverride.Value)
            {
                if (__instance.IsOwner && __instance.emerged)
                {
                    if (enemyScript != null && enemyScript.thisNetworkObject.IsSpawned)
                    {
                        enemyScript.thisNetworkObject.Despawn();
                    }
                }
                return false;
            }
            return true;
        }
    }
}
