﻿using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using NaturalSelection.Generics;
using static NaturalSelection.EnemyPatches.SpiderWebValues;

namespace NaturalSelection.EnemyPatches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> knownEnemy = new List<EnemyAI>();
        public List<EnemyAI> deadEnemyBodies = new List<EnemyAI>();
        public float LookAtEnemyTimer = 0f;
        public Dictionary<EnemyAI,float> enemiesInLOSDictionary = new Dictionary<EnemyAI, float>();   
    }

    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static float refreshCDtimeSpider = 1f;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool spiderHuntHoardingbug = Script.BoundingConfig.spiderHuntHoardingbug.Value;

        static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.BoundingConfig.debugSpiders.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(SandSpiderAI __instance)
        {
            if (!spiderList.ContainsKey(__instance))
            {
                spiderList.Add(__instance, new SpiderData());
                SpiderData spiderData = spiderList[__instance];
            }
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefixPatch(SandSpiderAI __instance)
        {
            if (!enableSpider) return true;

            SpiderData spiderData = spiderList[__instance];

            /* if (__instance.navigateMeshTowardsPosition && spiderData.targetEnemy != null)
             {
                 __instance.CalculateSpiderPathToPosition();
             }*/
            if (RoundManagerPatch.RequestUpdate(__instance) == true)
            {
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance));
            }
            if (__instance.IsOwner) spiderData.enemiesInLOSDictionary = EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 80f, 15, 2f);

            if (spiderData.enemiesInLOSDictionary.Count > 0)
            {
                foreach (KeyValuePair<EnemyAI, float> enemy in spiderData.enemiesInLOSDictionary)
                {
                    if (enemy.Key.isEnemyDead)
                    {
                        if (debugSpider) Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(__instance)  + " Update Postfix: " + enemy.Key + " is Dead! Checking deadEnemyBodies list and skipping...");

                        if (!spiderData.deadEnemyBodies.Contains(enemy.Key))
                        {
                            spiderData.deadEnemyBodies.Add(enemy.Key);
                            if (debugSpider) Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(__instance) + " Update Postfix: " + enemy.Key + " added to deadEnemyBodies list");
                        }
                        continue;
                    }
                    if (spiderData.knownEnemy.Contains(enemy.Key))
                    {
                        if (debugSpider && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " Update Postfix: " + enemy.Key + " is already in knownEnemyList");
                    }
                    else
                    {
                        if (debugSpider) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + " Update Postfix: Adding " + enemy.Key + " to knownEnemyList");
                        spiderData.knownEnemy.Add(enemy.Key);
                    }
                }
                for (int i = 0; i < spiderData.knownEnemy.Count; i++)
                {
                    if (spiderData.knownEnemy[i].isEnemyDead)
                    {
                        if (debugSpider) Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(__instance)  + " Update Postfix: Removed " + spiderData.knownEnemy[i] + " from knownEnemyList");
                        spiderData.knownEnemy.Remove(spiderData.knownEnemy[i]);
                    }
                }
            }
            __instance.SyncMeshContainerPositionToClients();
            __instance.CalculateMeshMovement();
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        spiderData.closestEnemy = EnemyAIPatch.FindClosestEnemy(spiderData.knownEnemy, spiderData.closestEnemy, __instance);
                              //if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case0/ " + spiderData.closestEnemy + " is Closest enemy");


                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye) != false && !spiderData.closestEnemy.isEnemyDead)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            if (debugSpider) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case0/ Set " + spiderData.closestEnemy + " as TargetEnemy");
                            __instance.SwitchToBehaviourState(2);
                            if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case0/ Set state to " + __instance.currentBehaviourStateIndex);
                            __instance.chaseTimer = 12.5f;
                            __instance.watchFromDistance = Vector3.Distance(__instance.meshContainer.transform.position, spiderData.closestEnemy.transform.position) > 8f;
                        }
                        break;
                    }
                case 2:
                    {
                        if (__instance.targetPlayer != null) break;

                        if (spiderData.targetEnemy != spiderData.closestEnemy && spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye))
                        {
                            if (spiderData.targetEnemy is HoarderBugAI && spiderData.closestEnemy is not HoarderBugAI && (Vector3.Distance(__instance.meshContainer.position, spiderData.targetEnemy.transform.position) * 1.2f < Vector3.Distance(__instance.meshContainer.position, spiderData.closestEnemy.transform.position)))
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                            else
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                        }


                        if (spiderData.targetEnemy == null)
                        {
                            if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-0/ Stopping chasing: " + spiderData.targetEnemy);
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                            break;
                        }
                        if (__instance.onWall)
                        {
                            __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position);
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                            if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2/ onWall");
                            break;
                        }
                        if (__instance.watchFromDistance && __instance.GetClosestPlayer(true) != null && Vector3.Distance(__instance.meshContainerPosition, __instance.GetClosestPlayer(true).transform.position) > Vector3.Distance(__instance.meshContainerPosition, spiderData.targetEnemy.transform.position))
                        {
                            if (spiderData.LookAtEnemyTimer <= 0f)
                            {
                                spiderData.LookAtEnemyTimer = 3f;
                                __instance.movingTowardsTargetPlayer = false;
                                __instance.overrideSpiderLookRotation = true;
                                Vector3 position = spiderData.targetEnemy.transform.position;
                                __instance.SetSpiderLookAtPosition(position);
                            }
                            else
                            {
                                spiderData.LookAtEnemyTimer -= Time.deltaTime;
                            }
                            __instance.spiderSpeed = 0f;
                            __instance.agent.speed = 0f;
                            if (Physics.Linecast(__instance.meshContainer.position, spiderData.targetEnemy.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                            {
                                if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-1/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                            else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) < 5f || __instance.stunNormalizedTimer > 0f)
                            {
                                __instance.watchFromDistance = false;
                            }
                            break;
                        }
                        __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position);
                        if (spiderData.targetEnemy == null || spiderData.targetEnemy.isEnemyDead)
                        {
                            if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-2/ Stopping chasing: " + spiderData.targetEnemy);

                            if (spiderData.targetEnemy != null)
                            {
                                try
                                {
                                    spiderData.deadEnemyBodies.Add(spiderData.targetEnemy);
                                    spiderData.knownEnemy.Remove(spiderData.targetEnemy);
                                    if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-2/ Moved dead enemy to separate list");
                                }
                                catch
                                {
                                    Script.Logger.LogError(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-2/ Enemy does not exist!");
                                }
                            }
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                        }
                        else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.homeNode.position) > 12f && Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) > 5f)
                        {
                            __instance.chaseTimer -= Time.deltaTime;
                            if (__instance.chaseTimer <= 0)
                            {
                                if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "Update Postfix: /case2-3/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                        }
                        break;
                    }
            }

            if (refreshCDtimeSpider <= 0)
            {
                if (debugSpider && debugSpam)
                {
                    Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + "watchFromDistance: " + __instance.watchFromDistance);
                    Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + "overrideSpiderLookRotation: " + __instance.overrideSpiderLookRotation);
                    Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + "moveTowardsDestination: " + __instance.moveTowardsDestination);
                    Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance)  + "movingTowardsTargetPlayer: " + __instance.movingTowardsTargetPlayer);
                }
                refreshCDtimeSpider = 0.5f;
            }
            else
            {
                refreshCDtimeSpider -= Time.deltaTime;
            }

            if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                ReversePatchAI.ReverseUpdate(__instance);
                if (__instance.updateDestinationInterval >= 0)
                {
                    __instance.updateDestinationInterval -= Time.deltaTime;
                }
                else
                {
                    __instance.updateDestinationInterval = __instance.AIIntervalTime + Random.Range(-0.015f, 0.015f);
                    __instance.DoAIInterval();
                }
                __instance.timeSinceHittingPlayer += Time.deltaTime;
                return false;
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return true;
            SpiderData spiderData = spiderList[__instance];
            SandSpiderAI Ins = __instance;

        if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();

                if (debugSpider && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Prefix: false");
                return false;
            }
            if (debugSpider && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Prefix: true");
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            SandSpiderAI Ins = __instance;
            SpiderData spiderData = spiderList[__instance];
            
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (debugSpider && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case0/ nothing");
                        break;
                    }
                case 1:
                    {
                        if (debugSpider && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case1/");
                        List<EnemyAI> tempList = spiderData.enemiesInLOSDictionary.Keys.ToList();
                        if (Ins.reachedWallPosition)
                        {
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 5f || tempList[i] is HoarderBugAI)
                                {
                                    ChaseEnemy(__instance, tempList[i]);
                                    if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case1/ Chasing enemy: " + tempList[i]);
                                    break;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 10f)
                                {
                                    Vector3 position = tempList[i].transform.position;
                                    float wallnumb = Vector3.Dot(position - Ins.meshContainer.position, Ins.wallNormal);
                                    Vector3 forward = position - wallnumb * Ins.wallNormal;
                                    Ins.meshContainerTargetRotation = Quaternion.LookRotation(forward, Ins.wallNormal);
                                    Ins.overrideSpiderLookRotation = true;
                                    if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case1/ Moving off-wall to enemy: " + tempList[i]);
                                    break;
                                }
                            }
                        }
                    }
                    Ins.overrideSpiderLookRotation = false;
                    break;
                case 2:
                    {
                        if (spiderData.targetEnemy != null)
                        {
                            if (spiderData.targetEnemy.isEnemyDead)
                            {
                                if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case2/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                Ins.StopChasing();
                                break;
                            }
                            if (Ins.watchFromDistance)
                            {
                                Ins.SetDestinationToPosition(Ins.ChooseClosestNodeToPosition(spiderData.targetEnemy.transform.position, avoidLineOfSight: false, 4).transform.position);
                                if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance)  + "DoAIInterval Postfix: /case2/ Set destination to: " + spiderData.targetEnemy);
                            }
                        }
                        break;
                    }
            }
        }

        static void ChaseEnemy(SandSpiderAI ins, EnemyAI target, SandSpiderWebTrap? triggeredWeb = null)
        {
            SpiderData spiderData = spiderList[ins];
            if ((ins.currentBehaviourStateIndex != 2 && ins.watchFromDistance) || Vector3.Distance(target.transform.position, ins.homeNode.position) < 25f || Vector3.Distance(ins.meshContainer.position, target.transform.position) < 15f)
            {
                ins.watchFromDistance = false;
                spiderData.targetEnemy = target;
                ins.chaseTimer = 12.5f;
                ins.SwitchToBehaviourState(2);
                if (debugSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(ins)  + "ChaseEnemy: Switched state to: " + ins.currentBehaviourStateIndex);
            }
        }
    }

    class SpiderWebValues
    {
        public EnemyAI? trappedEnemy = null;
    }


    [HarmonyPatch(typeof(SandSpiderWebTrap))]
    class SandSpiderWebTrapPatch
    {
        public struct EnemyInfo(EnemyAI enemy, float enterAgentSpeed, float enterAnimationSpeed)
        {
            internal EnemyAI EnemyAI { get; set; } = enemy;
            internal float EnterAgentSpeed { get; set; } = enterAgentSpeed;
            internal float EnterAnimationSpeed { get; set; } = enterAnimationSpeed;
            internal List<SandSpiderWebTrap> NumberOfTraps { get; set; } = new List<SandSpiderWebTrap>();
        }

        static Dictionary<SandSpiderWebTrap, SpiderWebValues> spiderWebs = new Dictionary<SandSpiderWebTrap, SpiderWebValues>();
        static Dictionary<EnemyAI, EnemyInfo> enemyData = new Dictionary<EnemyAI, EnemyInfo>();

       [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(SandSpiderWebTrap __instance)
        {
            if (!spiderWebs.ContainsKey(__instance))
            {
                spiderWebs.Add(__instance, new SpiderWebValues());
            }
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        static void OnTriggerStayPatch(Collider other, SandSpiderWebTrap __instance)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript) trippedEnemy = trippedEnemyCollision.mainScript;
            if (trippedEnemy == __instance.mainScript) return;

            if (trippedEnemy != null)
            {
                webData.trappedEnemy = trippedEnemy;

                if (!enemyData.ContainsKey(trippedEnemy))
                {
                    enemyData[trippedEnemy] = new EnemyInfo(trippedEnemy, trippedEnemy.agent.speed, trippedEnemy.creatureAnimator.speed);
                }
                if (!enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Add(__instance);
                    Script.Logger.LogInfo("Added instance to NumberOfWebTraps " + enemyData[trippedEnemy].NumberOfTraps.Count);
                }

                if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Collided with " + trippedEnemy);

                trippedEnemy.agent.speed = enemyData[trippedEnemy].EnterAgentSpeed / 3;
                trippedEnemy.creatureAnimator.speed = enemyData[trippedEnemy].EnterAnimationSpeed / 3;

                if (Script.BoundingConfig.debugSpiders.Value)
                {
                    Script.Logger.LogInfo(__instance + " Slowed down movement of " + trippedEnemy + " from " + enemyData[trippedEnemy].EnterAgentSpeed + " to " + trippedEnemy.agent.speed);
                    Script.Logger.LogInfo(__instance + " Slowed down animation of " + trippedEnemy + " from " + enemyData[trippedEnemy].EnterAnimationSpeed + " to " + trippedEnemy.creatureAnimator.speed);
                }
                if (!__instance.webAudio.isPlaying)
                {
                    __instance.webAudio.Play();
                    __instance.webAudio.PlayOneShot(__instance.mainScript.hitWebSFX);
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(SandSpiderWebTrap __instance, out bool __state)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            if (__instance.currentTrappedPlayer != null)
            {
                __state = false;
                return true;
            }
            else if (webData.trappedEnemy != null)
            {
                __state = true;
                return false;
            }
            __state = false;
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(SandSpiderWebTrap __instance, bool __state)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            if (!__state)
            {
                return;
            }

            if (webData.trappedEnemy != null)
            {
                __instance.leftBone.LookAt(webData.trappedEnemy.transform.position);
                __instance.rightBone.LookAt(webData.trappedEnemy.transform.position);
            }
            else
            {
                __instance.leftBone.LookAt(__instance.centerOfWeb);
                __instance.rightBone.LookAt(__instance.centerOfWeb);
            }
            __instance.transform.localScale = Vector3.Lerp(__instance.transform.localScale, new Vector3(1f, 1f, __instance.zScale), 8f * Time.deltaTime);
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        static void OnTriggerExitPatch(Collider other, SandSpiderWebTrap __instance)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript) trippedEnemy = trippedEnemyCollision.mainScript;

            if (trippedEnemy != null)
            {
                if (enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Remove(__instance);
                    Script.Logger.LogInfo("Removed instance to NumberOfWebTraps " + enemyData[trippedEnemy].NumberOfTraps.Count);
                }

                Script.Logger.LogInfo("Removing " + trippedEnemy);
                trippedEnemy.agent.speed = enemyData[trippedEnemy].EnterAgentSpeed;
                trippedEnemy.creatureAnimator.speed = enemyData[trippedEnemy].EnterAnimationSpeed;
                webData.trappedEnemy = null;
                __instance.webAudio.Stop();
            }
        }
    }
}