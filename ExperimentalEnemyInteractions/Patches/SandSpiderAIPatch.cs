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

namespace ExperimentalEnemyInteractions.Patches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public CustomBehavior CustomBehaviorState;
        public float LookAtEnemyTimer = 0f;
    }
    public enum CustomBehavior
        {
            Idle,
            Patrol,
            Chase,
            OverriddenByVanilla
        }

    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static float refreshCDtimeSpider = 1f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool spiderHuntHoardingbug = Script.BoundingConfig.spiderHuntHoardingbug.Value;

        static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.BoundingConfig.debugUnspecified.Value;


        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(SandSpiderAI __instance)
        {
            spiderList.Add(__instance, new SpiderData());
            SpiderData spiderData = spiderList[__instance];
            spiderData.CustomBehaviorState = CustomBehavior.Idle;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void UpdatePatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];
            

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;

            switch(spiderData.CustomBehaviorState)
            {
                case CustomBehavior.Idle:
                    {
                        spiderData.closestEnemy = EnemyAIPatch.findClosestEnemy(spiderData.enemyList, spiderData.closestEnemy, __instance);
                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f))
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            SwitchCustomState(__instance, CustomBehavior.Chase);
                            __instance.chaseTimer = 12.5f;
                        }
                    }
                    break;
                case CustomBehavior.Patrol:
                    {
                        if (__instance.waitOnWallTimer <= 0f)
                        {
                            SwitchCustomState(__instance, CustomBehavior.Idle);
                        }
                    }
                    break;
                case CustomBehavior.Chase:
                    {
                        __instance.setDestinationToHomeBase = false;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.lookingForWallPosition = false;
                        __instance.waitOnWallTimer = 11f;
                        if (__instance.spoolingPlayerBody)
                        {
                            __instance.CancelSpoolingBody();
                        }
                        if (spiderData.targetEnemy == null && __instance.targetPlayer == null)
                        {
                            break;
                        }
                        if (__instance.onWall)
                        {
                            if (Physics.Linecast(__instance.meshContainer.position, spiderData.targetEnemy.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                            {
                                
                            }
                            else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.transform.position) < 5f || __instance.stunNormalizedTimer > 0f)
                            {
                                __instance.watchFromDistance = false;
                            }
                            break;
                        }
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.moveTowardsDestination = true;
                        __instance.overrideSpiderLookRotation = false;
                        if (spiderData.targetEnemy.isEnemyDead || spiderData.targetEnemy == null)
                        {
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
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
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return;
            SpiderData spiderData = spiderList[__instance];
            switch (spiderData.CustomBehaviorState)
            {
                case CustomBehavior.Idle:
                {
                    
                }
                break;
                case CustomBehavior.Patrol:
                {
                    if (spiderData.closestEnemy != null && !Physics.Linecast(__instance.transform.position, spiderData.closestEnemy.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && Vector3.Distance(__instance.transform.position, spiderData.closestEnemy.transform.position) < 16f)
                    {
                        if (spiderData.closestEnemy is HoarderBugAI)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            __instance.StopSearch(__instance.patrolHomeBase);
                            SwitchCustomState(__instance, CustomBehavior.Chase);
                            break;
                        }
                        else if (Vector3.Distance(__instance.transform.position,spiderData.closestEnemy.transform.position) < 8f)
                        {
                            __instance.moveTowardsDestination = false;
                            __instance.overrideSpiderLookRotation = true;

                            __instance.SetSpiderLookAtPosition(spiderData.closestEnemy.transform.position);
                            if (Vector3.Distance(__instance.transform.position,spiderData.closestEnemy.transform.position) < 5f)
                            {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            __instance.StopSearch(__instance.patrolHomeBase);
                            SwitchCustomState(__instance, CustomBehavior.Chase);
                            break;
                            }
                        } 
                    }
                    else
                    {
                        __instance.moveTowardsDestination = true;
                        break;
                    }
                }
                break;
                case CustomBehavior.Chase:
                {
                    if (spiderData.targetEnemy != null)
                    {
                        if(Vector3.Distance(spiderData.targetEnemy.transform.position, spiderData.targetEnemy.transform.position) < 16f)
                        {
                        __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, checkForPath: true);
                        __instance.moveTowardsDestination = true;
                        }
                        else
                        {
                            __instance.agent.ResetPath();
                            SwitchCustomState(__instance,CustomBehavior.Patrol);
                        }
                    }
                    else
                    {
                        __instance.StartSearch(__instance.homeNode.transform.position ,__instance.patrolHomeBase);
                        SwitchCustomState(__instance, CustomBehavior.Patrol);
                    }
                }
                break;
                case CustomBehavior.OverriddenByVanilla:
                {

                }
                break;
            }
        }
        static public void SwitchCustomState(SandSpiderAI instance, CustomBehavior state)
        {
            SpiderData spiderData = spiderList[instance];
            spiderData.CustomBehaviorState = state;
        }
    }
}