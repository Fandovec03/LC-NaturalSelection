using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using NaturalSelection.Generics;
using System;
using LethalNetworkAPI;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{
    class SpiderData : EnemyDataBase
    {
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

        //static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.Bools["debugSpiders"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool debugTriggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> spiderBlacklist = InitializeGamePatch.spiderBlacklist;

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugSpiders") debugSpider = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlag = value;
            //Script.LogNS(LogLevel.Message,$"Bunker Spider received event. debugSpider = {debugSpider}, debugSpam = {debugSpam}, debugTriggetFlag = {debugTriggerFlag}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(SandSpiderAI __instance)
        {
            SpiderData data = (SpiderData)EnemyAIPatch.GetEnemyData(__instance, new SpiderData());
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;

            NaturalSelectionLib.NaturalSelectionLib.ReturnOwnerResultPairDelegate += getClosestEnemyResult;
            void getClosestEnemyResult(int id, EnemyAI? closestEnemy)
            {
                //Script.LogNS(LogLevel.Info, "Received action delegate", __instance);
                if (__instance == null)
                {
                    Script.LogNS(LogLevel.Error, "No longer exists. Unsubscribing.", __instance);
                    NaturalSelectionLib.NaturalSelectionLib.ReturnOwnerResultPairDelegate -= getClosestEnemyResult;
                }
                else if (id == __instance.NetworkBehaviourId)
                {
                    Script.LogNS(LogLevel.Info, $"Set {closestEnemy} as closestEnemy", __instance);
                    data.closestEnemy = closestEnemy;
                }
            }
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefixPatch(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            SpiderData spiderData = (SpiderData)EnemyAIPatch.GetEnemyData(__instance, new SpiderData());
            Type type = __instance.GetType();

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
                        Script.LogNS(LogLevel.Warning, $"Update Postfix: {enemy.Key} is Dead! Checking deadEnemyBodies list and skipping...", __instance, debugSpider);

                        if (!spiderData.deadEnemyBodies.Contains(enemy.Key))
                        {
                            spiderData.deadEnemyBodies.Add(enemy.Key);
                            Script.LogNS(LogLevel.Warning, $"Update Postfix: {enemy.Key} added to deadEnemyBodies list", __instance, debugSpider);
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
                        //__instance.StartCoroutine(NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemyCoroutine(tempList, spiderData.closestEnemy, __instance, usePathLengthAsDistance: true));

                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye) != false && !spiderData.closestEnemy.isEnemyDead)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            spiderData.investigateTrap = null;
                            Script.LogNS(LogLevel.Info,$"Update Postfix: /case0/ Set {spiderData.closestEnemy} as TargetEnemy", __instance, debugSpider);
                            __instance.SwitchToBehaviourState(2);
                            Script.LogNS(LogLevel.Debug,$"Update Postfix: /case0/ Set state to {__instance.currentBehaviourStateIndex}", __instance, debugSpider);
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
                            Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2-0/ Stopping chasing: {spiderData.targetEnemy}", __instance, debugSpider);
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                            break;
                        }
                        if (__instance.onWall)
                        {
                            __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true);
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                            Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2/ onWall", __instance, debugSpider);
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
                                Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2-1/ Stopping chasing: {spiderData.targetEnemy}", __instance, debugSpider);
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
                            Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2-2/ Stopping chasing: {spiderData.targetEnemy}", __instance, debugSpider);

                            if (spiderData.targetEnemy != null)
                            {
                                try
                                {
                                    spiderData.deadEnemyBodies.Add(spiderData.targetEnemy);
                                    Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2-2/ Moved dead enemy to separate list", __instance, debugSpider);
                                }
                                catch
                                {
                                    Script.LogNS(LogLevel.Error,$"Update Postfix: /case2-2/ Enemy does not exist!", __instance);
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
                                Script.LogNS(LogLevel.Debug,$"Update Postfix: /case2-3/ Stopping chasing: {spiderData.targetEnemy}", __instance, debugSpider);
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
                        Script.LogNS(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "watchFromDistance: " + __instance.watchFromDistance);
                        Script.LogNS(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "overrideSpiderLookRotation: " + __instance.overrideSpiderLookRotation);
                        Script.LogNS(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "moveTowardsDestination: " + __instance.moveTowardsDestination);
                        Script.LogNS(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + "movingTowardsTargetPlayer: " + __instance.movingTowardsTargetPlayer);
                    }
                    refreshCDtimeSpider = 0.5f;
                }
                else
                {
                    refreshCDtimeSpider -= Time.deltaTime;
                }

                if ((spiderData.targetEnemy != null && __instance.currentBehaviourStateIndex == 2 || spiderData.investigateTrap != null) && !__instance.targetPlayer)
                {
                    //Script.LogNS(LogLevel.Message,$"Invoking originalUpdate");
                    try
                    {
                        ReversePatchAI.originalUpdate.Invoke(__instance);
                        //Script.LogNS(LogLevel.Message,"Succesfully invoked originalUpdate");
                    }
                    catch (Exception e)
                    {
                        Script.LogNS(LogLevel.Error,"failed invoking originalUpdate.", __instance);
                        Script.LogNS(LogLevel.Error,e.ToString());
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
            SpiderData spiderData = (SpiderData)EnemyAIPatch.GetEnemyData(__instance, new SpiderData()); ;
            SandSpiderAI Ins = __instance;

            if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();

                Script.LogNS(LogLevel.Debug,$"DoAIInterval Prefix: false", __instance, debugSpider && debugSpam);
                return false;
            }
            Script.LogNS(LogLevel.Debug,$"DoAIInterval Prefix: true", __instance, debugSpider && debugSpam);
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (__instance.isEnemyDead) return;
            SandSpiderAI Ins = __instance;
            SpiderData spiderData = (SpiderData)EnemyAIPatch.GetEnemyData(__instance, new SpiderData()); ;
            
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case0/ nothing", __instance, debugSpider && debugSpam);
                        break;
                    }
                case 1:
                    {
                        Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case1/", __instance, debugSpider && debugSpam);
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
                                    Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case1/ Chasing enemy: {tempList[i]}", __instance, debugSpider);
                                    break;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 10f)
                                {
                                    Vector3 position = tempList[i].transform.position;
                                    float wallnumb = Vector3.Dot(position - Ins.meshContainer.position, Ins.wallNormal);
                                    Vector3 forward = position - wallnumb * Ins.wallNormal;
                                    Ins.meshContainerTargetRotation = Quaternion.LookRotation(forward, Ins.wallNormal);
                                    Ins.overrideSpiderLookRotation = true;
                                    Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case1/ Moving off-wall to enemy: {tempList[i]}", __instance, debugSpider);
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
                                Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case2/ Stopping chasing: {spiderData.targetEnemy}", __instance, debugSpider);
                                spiderData.targetEnemy = null;
                                Ins.StopChasing();
                                break;
                            }
                            if (Ins.watchFromDistance)
                            {
                                Ins.SetDestinationToPosition(Ins.ChooseClosestNodeToPosition(spiderData.targetEnemy.transform.position, avoidLineOfSight: false, 4).transform.position, true);
                                Script.LogNS(LogLevel.Debug,$"DoAIInterval Postfix: /case2/ Set destination to: {spiderData.targetEnemy}", __instance, debugSpider);
                            }
                        }
                        break;
                    }
            }
        }
        public static void OnCustomEnemyCollision(SandSpiderAI __instance, EnemyAI mainscript2)
        {
            if (mainscript2.GetType() == typeof(SandSpiderAI)) return;
            if (EnemyAIPatch.enemyDataDict.ContainsKey(__instance.enemyType.enemyName + __instance.NetworkBehaviourId) && __instance.currentBehaviourStateIndex == 2 && !mainscript2.isEnemyDead && !spiderBlacklist.Contains(mainscript2.enemyType.enemyName))
            {
                Script.LogNS(LogLevel.Debug,$"timeSinceHittingPlayer: {__instance.timeSinceHittingPlayer}", __instance, debugSpider && debugTriggerFlag);
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

                    Script.LogNS(LogLevel.Message,$"hit {LibraryCalls.DebugStringHead(mainscript2)}", __instance);
                }
            }
        }

        static void ChaseEnemy(SandSpiderAI ins, EnemyAI target, SandSpiderWebTrap? triggeredWeb = null)
        {
            SpiderData spiderData = (SpiderData)EnemyAIPatch.GetEnemyData(ins, new SpiderData());
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


            SpiderWebValues webData = (SpiderWebValues)EnemyAIPatch.GetEnemyData(triggeredTrap, new SpiderWebValues());
            EnemyAI? tempEnemy = webData.trappedEnemy;

            if (tempEnemy == null || InitializeGamePatch.spiderBlacklist.Contains(tempEnemy.enemyType.enemyName) || tempEnemy.isEnemyDead) { return; }

            CustomEnemySize customEnemySize = (CustomEnemySize)InitializeGamePatch.customSizeOverrideListDictionary[tempEnemy.enemyType.enemyName];
            SpiderData spiderData = (SpiderData)EnemyAIPatch.GetEnemyData(owner, new SpiderData());
            Script.LogNS(LogLevel.Info,$"Custom enemy size: {customEnemySize}", triggeredTrap, debugSpider);
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
                    if (debugSpider || debugTriggerFlag) Script.LogNS(LogLevel.Info,$"alerted its owner {LibraryCalls.DebugStringHead(owner)}", triggeredTrap);
                    return;
                }
                spiderData.investigateTrap = triggeredTrap;
                spiderData.investigateTrapTimer = 5f;
                if (debugSpider || debugTriggerFlag) Script.LogNS(LogLevel.Info,$"alerted its owner {LibraryCalls.DebugStringHead(owner)}",triggeredTrap);
                return;
            }
        }
    }
}