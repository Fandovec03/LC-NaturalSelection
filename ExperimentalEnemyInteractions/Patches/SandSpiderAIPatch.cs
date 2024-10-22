using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using BepInEx;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.MeshSubsetCombineUtility;
using System.Linq;
using System.Net;

namespace ExperimentalEnemyInteractions.Patches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? closestEnemyLOS = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public float LookAtEnemyTimer = 0f;
        public SortedList<EnemyAI,float> enemiesInLOSSortList = new SortedList<EnemyAI, float>();   
        public float ChaseEnemy = 0f;
    }

    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static float refreshCDtimeSpider = 1f;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool spiderHuntHoardingbug = Script.BoundingConfig.spiderHuntHoardingbug.Value;

        static float refreshLOS = 0.2f;

        static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.BoundingConfig.debugSpiders.Value;


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
           // if (!enableSpider) return true;
            SpiderData spiderData = spiderList[__instance];
            
            if (refreshLOS <=0)
            {
                refreshLOS = 0.2f;
            }
            else
            {
                refreshLOS -= Time.deltaTime;
            }

            refreshCDtimeSpider -= Time.deltaTime;

            if (refreshCDtimeSpider <= 0)
            {
                refreshCDtimeSpider = 1f;
            }

            if (spiderData.targetEnemy != null && !__instance.movingTowardsTargetPlayer)
            {
                if(__instance.updateDestinationInterval > 0)
                {
                    __instance.updateDestinationInterval -= Time.deltaTime;
                }
                else
                {
                    __instance.DoAIInterval();
                }

                return false;
            }
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfixPatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];
            if (spiderData == null)
            {
                return;
            }

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;
            if (__instance.navigateMeshTowardsPosition && spiderData.targetEnemy != null)
            {
                __instance.CalculateSpiderPathToPosition();
            }

            spiderData.enemiesInLOSSortList = EnemyAIPatch.GetEnemiesInLOS(__instance, spiderData.enemyList, 80f, 15, 2f);

            foreach(KeyValuePair<EnemyAI,float> enemy in spiderData.enemiesInLOSSortList)
            {
                if (spiderData.enemyList.Contains(enemy.Key))
                {
                    if (debugSpider) Script.Logger.LogDebug(__instance + " Update Postfix: " + enemy.Key + " is already in sortedList");
                }
                else
                {
                    if (debugSpider) Script.Logger.LogDebug(__instance + " Update Postfix: Adding " + enemy.Key + " to sortedList");
                    spiderData.enemyList.Add(enemy.Key);
                }
            }

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        spiderData.closestEnemy = EnemyAIPatch.findClosestEnemy(spiderData.enemyList, spiderData.closestEnemy, __instance);
                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f) != false)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case0/ Set " + spiderData.closestEnemy + " as TargetEnemy");
                            __instance.SwitchToBehaviourState(2);
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case0/ Set state to " + __instance.currentBehaviourStateIndex);
                            spiderData.ChaseEnemy = 12.5f;
                            __instance.watchFromDistance = Vector3.Distance(__instance.meshContainer.transform.position, spiderData.targetEnemy.transform.position) > 8f;
                        }
                        break;
                    }
                case 1:
                    if (spiderData.enemiesInLOSSortList.Count > 0)
                    {
                        foreach (EnemyAI enemy in spiderData.enemiesInLOSSortList.Keys)
                        {
                            if (enemy is HoarderBugAI)
                            {
                                spiderData.targetEnemy = enemy;
                                __instance.SwitchToBehaviourState(2);
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case1/ Switched state to " + __instance.currentBehaviourStateIndex);
                                break;
                            }
                            else
                            {
                                spiderData.targetEnemy = null;
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case1/ Enemy set to null");
                                break;
                            }
                        }
                        break;
                    }
                    break;
                case 2:
                    {
                        if(__instance.spooledPlayerBody)
                        {
                            __instance.CancelSpoolingBody();
                        }
                        if (spiderData.targetEnemy == null && __instance.targetPlayer == null)
                        {
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-0/ Stopping chasing: " + spiderData.targetEnemy);
                            __instance.StopChasing();
                        }
                        if(__instance.onWall)
                        {
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2/ onWall");
                            break;
                        }
                        if(__instance.watchFromDistance)
                        {
                            /*if (__instance.lookAtPlayerInterval < 3f)
                            {

                            }*/
                            __instance.spiderSpeed = 0f;
                            __instance.agent.speed = 0f;
                            if (Physics.Linecast(__instance.meshContainer.position, spiderData.targetEnemy.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-1/ Stopping chasing: " + spiderData.targetEnemy);
                                __instance.StopChasing();
                            }
                            else
                            {
                                __instance.watchFromDistance = false;
                            }
                            break;
                        }

                        switch(__instance.enemyHP)
                        {
                            default:
                                {
                                    __instance.agent.speed = 4.4f;
                                    __instance.spiderSpeed = 4.4f;
                                    break;
                                }
                            case 2:
                                {
                                    __instance.agent.speed = 4.58f;
                                    __instance.spiderSpeed = 4.58f;
                                    break;
                                }
                            case 1:
                                {
                                    __instance.agent.speed = 5.04f;
                                    __instance.spiderSpeed = 5.04f;
                                    break;
                                }
                        }
                        if (spiderData.targetEnemy != null && spiderData.targetEnemy.isEnemyDead)
                        {
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-2/ Stopping chasing: " + spiderData.targetEnemy);
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                        }
                        else if ((Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.homeNode.position) > 12f && Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.transform.position) > 5f))
                        {
                            spiderData.ChaseEnemy -= Time.deltaTime;
                            if (spiderData.ChaseEnemy <= 0)
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-3/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                        }
                    }
                    break;
            }

            if (refreshCDtimeSpider <= 0)
            {
                refreshCDtimeSpider = 1f;
            }
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return true;
            SpiderData spiderData = spiderList[__instance];

            if (!__instance.movingTowardsTargetPlayer && spiderData.targetEnemy != null)
            {
                return false;
            }
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
                        if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case0/ nothing");
                    }
                    break;
                case 1:
                    {
                        if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/");
                        List<EnemyAI> tempList = spiderData.enemiesInLOSSortList.Keys.ToList();
                        if (Ins.reachedWallPosition)
                        {
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 5f)
                                {
                                    ChaseEnemy(__instance, tempList[i]);
                                    if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Chasing enemy: " + tempList[i]);
                                    break;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 10f)
                                {
                                    Vector3 position = tempList[i].transform.position;
                                    float wallnumb = Vector3.Dot(position - Ins.meshContainer.position, Ins.wallNormal);
                                    Vector3 forward = position - wallnumb * Ins.wallNormal;
                                    Ins.meshContainerTargetRotation = Quaternion.LookRotation(forward, Ins.wallNormal);
                                    Ins.overrideSpiderLookRotation = true;
                                    if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Moving off-wall to enemy: " + tempList[i]);
                                    break;
                                }
                            }
                        }
                    }
                    Ins.overrideSpiderLookRotation = false;
                    break;
                case 2:
                    {
                        if (spiderData.targetEnemy != null && Ins.targetPlayer == null)
                        {
                            if (spiderData.targetEnemy.isEnemyDead)
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                Ins.StopChasing();
                            }
                            if(Ins.watchFromDistance)
                            {
                                Ins.SetDestinationToPosition(Ins.ChooseClosestNodeToPosition(spiderData.targetEnemy.transform.position, avoidLineOfSight: false, 4).transform.position);
                                if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Set destination to: " + spiderData.targetEnemy);
                            }
                        }
                    }
                    break;
            }    

        }

        static void ChaseEnemy(SandSpiderAI __instance, EnemyAI target)
        {
            SpiderData spiderData = spiderList[__instance];
            SandSpiderAI ins = __instance;
            if ((ins.currentBehaviourStateIndex != 2 && ins.watchFromDistance) || Vector3.Distance(target.transform.position, ins.homeNode.position) < 25f || Vector3.Distance(target.transform.position, target.transform.position) < 15f)
            {
                ins.watchFromDistance = false;
                spiderData.targetEnemy = target;
                ins.chaseTimer = 12.5f;
                ins.SwitchToBehaviourState(2);
            }
        }
    }
}