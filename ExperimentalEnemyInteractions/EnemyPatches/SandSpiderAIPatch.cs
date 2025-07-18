﻿using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using NaturalSelection.Generics;
using System;
using LethalNetworkAPI;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{
    class SpiderData()
    {
        internal EnemyAI? closestEnemy = null;
        internal EnemyAI? targetEnemy = null;
        //internal List<EnemyAI> knownEnemy = new List<EnemyAI>();
        internal List<EnemyAI> deadEnemyBodies = new List<EnemyAI>();
        internal float LookAtEnemyTimer = 0f;
        internal Dictionary<EnemyAI,float> enemiesInLOSDictionary = new Dictionary<EnemyAI, float>();
        internal SandSpiderWebTrap? investigateTrap;
        internal bool investigated = false;
        internal float investigateTrapTimer = 0f;
    }

    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static float refreshCDtimeSpider = 1f;
        static float chaseModifier = Script.BoundingConfig.chaseAfterEnemiesModifier.Value;

        static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.Bools["debugSpiders"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool debugTriggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> spiderBlacklist = InitializeGamePatch.spiderBlacklistFinal;

        static LNetworkVariable<int> NetworkSpiderBehaviorState(SandSpiderAI instance)
        {
            string NWID = "NSSpiderBehaviorState" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<int>(NWID);
        }


        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugSpiders") debugSpider = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlag = value;
            //Script.Logger.Log(LogLevel.Message,$"Bunker Spider received event. debugSpider = {debugSpider}, debugSpam = {debugSpam}, debugTriggetFlag = {debugTriggerFlag}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(SandSpiderAI __instance)
        {
            if (!spiderList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                spiderList.Add(__instance, new SpiderData());
                SpiderData spiderData = spiderList[__instance];
            }
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefixPatch(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            CheckDataIntegritySpider(__instance);
            SpiderData spiderData = spiderList[__instance];
            Type type = __instance.GetType();

            int TargetingState = NetworkSpiderBehaviorState(__instance).Value;

            if (RoundManagerPatch.RequestUpdate(__instance) == true)
            {
                List<EnemyAI> tempList = LibraryCalls.GetCompleteList(__instance);
                LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);
                LibraryCalls.FilterEnemyList(ref tempList, spiderBlacklist, __instance);
                Dictionary<EnemyAI, int> tempDict = new Dictionary<EnemyAI, int>();
                foreach (EnemyAI enemy in tempList)
                {
                    tempDict.Add(enemy, InitializeGamePatch.customSizeOverrideListDictionary[enemy.enemyType.enemyName]);
                }
                LibraryCalls.FilterEnemySizes(ref tempDict, [1, 2, 3], __instance);
                tempList = tempDict.Keys.ToList();
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, ref tempList);
            }
            if (__instance.IsOwner)
            {
                List<EnemyAI> temoList = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                spiderData.enemiesInLOSDictionary = new Dictionary<EnemyAI, float>(LibraryCalls.GetEnemiesInLOS(__instance, ref temoList, 80f, 15, 2f));
            }

            if (spiderData.enemiesInLOSDictionary.Count > 0)
            {
                foreach (KeyValuePair<EnemyAI, float> enemy in spiderData.enemiesInLOSDictionary)
                {
                    if (enemy.Key is CentipedeAI && enemy.Key.currentBehaviourStateIndex == 1)
                    {
                        continue;
                    }
                    if (enemy.Key.isEnemyDead)
                    {
                        if (debugSpider) Script.Logger.Log(LogLevel.Warning, $"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: {enemy.Key} is Dead! Checking deadEnemyBodies list and skipping...");

                        if (!spiderData.deadEnemyBodies.Contains(enemy.Key))
                        {
                            spiderData.deadEnemyBodies.Add(enemy.Key);
                            if (debugSpider) Script.Logger.Log(LogLevel.Warning, $"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: {enemy.Key} added to deadEnemyBodies list");
                        }
                        continue;
                    }
                }
            }
            if (__instance.IsOwner)
            {
                switch (__instance.currentBehaviourStateIndex)
                {
                    case 0:
                    {
                        List<EnemyAI> tempList = spiderData.enemiesInLOSDictionary.Keys.ToList();
                        spiderData.closestEnemy = LibraryCalls.FindClosestEnemy(ref tempList, spiderData.closestEnemy, __instance);


                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye) != false && !spiderData.closestEnemy.isEnemyDead)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            spiderData.investigateTrap = null;
                            if (debugSpider) Script.Logger.LogInfo($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case0/ Set {spiderData.closestEnemy} as TargetEnemy");
                            __instance.SwitchToBehaviourState(2);
                            if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case0/ Set state to {__instance.currentBehaviourStateIndex}");
                            __instance.chaseTimer = 12.5f / chaseModifier;
                            __instance.watchFromDistance = Vector3.Distance(__instance.meshContainer.transform.position, spiderData.closestEnemy.transform.position) > 5f;
                            break;
                        }

                        if (spiderData.investigateTrap != null)
                        {
                            if (__instance.CheckLineOfSightForPosition(spiderData.investigateTrap.centerOfWeb.position, 80f, 15, 2f, __instance.eye) && Vector3.Distance(__instance.transform.position, spiderData.investigateTrap.centerOfWeb.position) < 3f)
                            {
                                if (spiderData.investigateTrapTimer > 0f)
                                {
                                    spiderData.investigateTrapTimer -= Time.deltaTime;
                                    __instance.SetDestinationToPosition(__instance.meshContainer.transform.position, true);
                                    __instance.overrideSpiderLookRotation = true;
                                    Vector3 position = spiderData.investigateTrap.centerOfWeb.position;
                                    position.y = __instance.meshContainer.transform.position.y;
                                    __instance.SetSpiderLookAtPosition(position);
                                }
                                else
                                {
                                    spiderData.investigateTrap = null;
                                    __instance.overrideSpiderLookRotation = false;
                                }
                            }
                            else
                            {
                                __instance.SetDestinationToPosition(spiderData.investigateTrap.centerOfWeb.position , true);
                                if (!__instance.onWall) __instance.agent.speed = 4.25f;
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        if (spiderData.investigateTrap != null)
                        {
                            __instance.SwitchToBehaviourState(0);
                            __instance.SetDestinationToPosition(spiderData.investigateTrap.centerOfWeb.position, true);
                        }
                        break;
                    }
                case 2:
                    {
                        if (spiderData.investigateTrap != null) spiderData.investigateTrap = null;
                        if (__instance.targetPlayer != null) break;

                        if (spiderData.targetEnemy != spiderData.closestEnemy && spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye))
                        {
                            float num1 = 0f;
                            if (spiderData.targetEnemy != null) num1 = Vector3.Distance(__instance.meshContainer.position, spiderData.targetEnemy.transform.position);
                            float num2 = Vector3.Distance(__instance.meshContainer.position, spiderData.closestEnemy.transform.position);

                            if (spiderData.targetEnemy is HoarderBugAI && spiderData.closestEnemy is not HoarderBugAI && (num1 / 1.2f < num2))
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                            else if (num1 < num2)
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                        }


                        if (spiderData.targetEnemy == null)
                        {
                            if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-0/ Stopping chasing: {spiderData.targetEnemy}");
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                            break;
                        }
                        if (__instance.onWall)
                        {
                            __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true);
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                            if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2/ onWall");
                            break;
                        }
                        if (__instance.watchFromDistance && (__instance.GetClosestPlayer(true) == null || __instance.GetClosestPlayer(true) != null && Vector3.Distance(__instance.meshContainerPosition, __instance.GetClosestPlayer(true).transform.position) > Vector3.Distance(__instance.meshContainerPosition, spiderData.targetEnemy.transform.position)))
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
                                if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-1/ Stopping chasing: {spiderData.targetEnemy}");
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                            else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) < 5f || __instance.stunNormalizedTimer > 0f)
                            {
                                __instance.watchFromDistance = false;
                            }
                            break;
                        }
                        __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true);
                        __instance.agent.speed = 4.25f;
                        __instance.spiderSpeed = 4.25f;
                        if (spiderData.targetEnemy == null || spiderData.targetEnemy.isEnemyDead)
                        {
                            if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-2/ Stopping chasing: {spiderData.targetEnemy}");

                            if (spiderData.targetEnemy != null)
                            {
                                try
                                {
                                    spiderData.deadEnemyBodies.Add(spiderData.targetEnemy);
                                    if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-2/ Moved dead enemy to separate list");
                                }
                                catch
                                {
                                    Script.Logger.LogError($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-2/ Enemy does not exist!");
                                }
                            }
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                        }
                        else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.homeNode.position) > 15f && Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) > 8f)
                        {
                            __instance.chaseTimer -= Time.deltaTime;
                            if (__instance.chaseTimer <= 0)
                            {
                                if (debugSpider) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(__instance)} Update Postfix: /case2-3/ Stopping chasing: {spiderData.targetEnemy}");
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
                        Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "watchFromDistance: " + __instance.watchFromDistance);
                        Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "overrideSpiderLookRotation: " + __instance.overrideSpiderLookRotation);
                        Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "moveTowardsDestination: " + __instance.moveTowardsDestination);
                        Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "movingTowardsTargetPlayer: " + __instance.movingTowardsTargetPlayer);
                    }
                    refreshCDtimeSpider = 0.5f;
                }
                else
                {
                    refreshCDtimeSpider -= Time.deltaTime;
                }

                if ((spiderData.targetEnemy != null && __instance.currentBehaviourStateIndex == 2 || spiderData.investigateTrap != null) && !__instance.targetPlayer)
                {
                    //Script.Logger.Log(LogLevel.Message,$"{LibraryCalls.DebugStringHead(__instance)} Invoking originalUpdate");
                    try
                    {
                        ReversePatchAI.originalUpdate.Invoke(__instance);
                        //Script.Logger.Log(LogLevel.Message,"Succesfully invoked originalUpdate");
                    }
                    catch (Exception e)
                    {
                        Script.Logger.Log(LogLevel.Error,"failed invoking originalUpdate.");
                        Script.Logger.Log(LogLevel.Error,e);
                    }

                    if (__instance.updateDestinationInterval >= 0)
                    {
                        __instance.updateDestinationInterval -= Time.deltaTime;
                    }
                    else
                    {
                        __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                        __instance.DoAIInterval();
                    }
                    __instance.timeSinceHittingPlayer += Time.deltaTime;


                    switch (__instance.currentBehaviourStateIndex)
                    {
                        case 0:
                            __instance.setDestinationToHomeBase = false;
                            __instance.lookingForWallPosition = false;
                            __instance.movingTowardsTargetPlayer = false;
                            __instance.overrideSpiderLookRotation = false;
                            __instance.waitOnWallTimer = 11f;
                            break;
                        case 2:
                            __instance.setDestinationToHomeBase = false;
                            __instance.reachedWallPosition = false;
                            __instance.lookingForWallPosition = false;
                            __instance.waitOnWallTimer = 11f;
                            break;
                    }
                    __instance.SyncMeshContainerPositionToClients();
                    __instance.CalculateMeshMovement();
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            CheckDataIntegritySpider(__instance);
            SpiderData spiderData = spiderList[__instance];
            SandSpiderAI Ins = __instance;

            if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();

                if (debugSpider && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Prefix: false");
                return false;
            }
            if (debugSpider && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Prefix: true");
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead) return;
            SandSpiderAI Ins = __instance;
            CheckDataIntegritySpider(__instance);
            SpiderData spiderData = spiderList[__instance];
            
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (debugSpider && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case0/ nothing");
                        break;
                    }
                case 1:
                    {
                        if (debugSpider && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case1/");
                        List<EnemyAI> tempList = spiderData.enemiesInLOSDictionary.Keys.ToList();
                        if (Ins.reachedWallPosition)
                        {
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (tempList[i] is CentipedeAI && tempList[i].currentBehaviourStateIndex == 1)
                                {
                                    continue;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 5f /*|| tempList[i] is HoarderBugAI*/)
                                {
                                    ChaseEnemy(__instance, tempList[i]);
                                    if (debugSpider) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case1/ Chasing enemy: {tempList[i]}");
                                    break;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 10f)
                                {
                                    Vector3 position = tempList[i].transform.position;
                                    float wallnumb = Vector3.Dot(position - Ins.meshContainer.position, Ins.wallNormal);
                                    Vector3 forward = position - wallnumb * Ins.wallNormal;
                                    Ins.meshContainerTargetRotation = Quaternion.LookRotation(forward, Ins.wallNormal);
                                    Ins.overrideSpiderLookRotation = true;
                                    if (debugSpider) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case1/ Moving off-wall to enemy: {tempList[i]}");
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
                                if (debugSpider) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case2/ Stopping chasing: {spiderData.targetEnemy}");
                                spiderData.targetEnemy = null;
                                Ins.StopChasing();
                                break;
                            }
                            if (Ins.watchFromDistance)
                            {
                                Ins.SetDestinationToPosition(Ins.ChooseClosestNodeToPosition(spiderData.targetEnemy.transform.position, avoidLineOfSight: false, 4).transform.position, true);
                                if (debugSpider) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval Postfix: /case2/ Set destination to: {spiderData.targetEnemy}");
                            }
                        }
                        break;
                    }
            }
        }
        public static void OnCustomEnemyCollision(SandSpiderAI __instance, EnemyAI mainscript2)
        {
            if (mainscript2.GetType() == typeof(SandSpiderAI)) return;
            if (spiderList.ContainsKey(__instance) && __instance.currentBehaviourStateIndex == 2 && !mainscript2.isEnemyDead && !spiderBlacklist.Contains(mainscript2.enemyType.enemyName))
            {
                if (debugSpider && debugTriggerFlag) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} timeSinceHittingPlayer: {__instance.timeSinceHittingPlayer}");
                if (__instance.timeSinceHittingPlayer > 1f)
                {
                    __instance.timeSinceHittingPlayer = 0f;
                    __instance.creatureSFX.PlayOneShot(__instance.attackSFX);

                    if (mainscript2.enemyHP > 2)
                    {
                        mainscript2.HitEnemy(2, null, playHitSFX: true);

                    }
                    else if (mainscript2.enemyHP > 0)
                    {
                        mainscript2.HitEnemy(1, null, playHitSFX: true);
                    }

                    if (debugSpider && debugTriggerFlag) Script.Logger.Log(LogLevel.Message,$"{LibraryCalls.DebugStringHead(__instance)} hit {LibraryCalls.DebugStringHead(mainscript2)}");
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
                ins.chaseTimer = 12.5f / chaseModifier;
                ins.SwitchToBehaviourState(2);
            }
        }

        public static void AlertSpider(SandSpiderAI owner, SandSpiderWebTrap triggeredTrap)
        {
            if (!Script.BoundingConfig.enableSpider.Value) return;

            EnemyAI? tempEnemy = tempEnemy = SandSpiderWebTrapPatch.spiderWebs[triggeredTrap].trappedEnemy;

            if (tempEnemy == null || InitializeGamePatch.spiderBlacklistFinal.Contains(tempEnemy.enemyType.enemyName) || tempEnemy.isEnemyDead) { return; }

            CustomEnemySize customEnemySize = (CustomEnemySize)InitializeGamePatch.customSizeOverrideListDictionary[tempEnemy.enemyType.enemyName];
            SpiderData spiderData = spiderList[owner];
            if (debugSpider) Script.Logger.LogInfo($"Custom enemy size: {customEnemySize}");
            if (owner.currentBehaviourStateIndex != 2)
            {
                if (!tempEnemy.enemyType.canDie)
                {
                    return;
                }

                if (spiderData.investigateTrap != null)
                {
                    spiderData.targetEnemy = tempEnemy;
                    owner.SwitchToBehaviourState(2);
                    spiderData.investigateTrap = null;
                    spiderData.investigateTrapTimer = 0;
                    if (debugSpider || debugTriggerFlag) Script.Logger.LogInfo($"Spider trap {triggeredTrap.trapID} alerted its owner {LibraryCalls.DebugStringHead(owner)}");
                    return;
                }
                spiderData.investigateTrap = triggeredTrap;
                spiderData.investigateTrapTimer = 5f;
                if (debugSpider || debugTriggerFlag) Script.Logger.LogInfo($"Spider trap {triggeredTrap.trapID} alerted its owner {LibraryCalls.DebugStringHead(owner)}");
                return;
            }
        }

        public static void CheckDataIntegritySpider(SandSpiderAI __instance)
        {
            if (!spiderList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                spiderList.Add(__instance, new SpiderData());
            }
        }
    }
}