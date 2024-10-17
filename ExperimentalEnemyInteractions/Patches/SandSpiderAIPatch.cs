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

namespace ExperimentalEnemyInteractions.Patches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public CustomBehavior CustomBehaviorState;
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
            spiderData.CustomBehaviorState = CustomBehavior.OverriddenByVanilla;
        }


        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];
            

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;

            switch(spiderData.CustomBehaviorState)
            {
                case CustomBehavior.Idle:
                break;
                case CustomBehavior.Patrol:
                break;
                case CustomBehavior.Chase:
                break;
                case CustomBehavior.OverriddenByVanilla:
                break;
            }

            if (refreshCDtimeSpider <= 0)
            {
                refreshCDtimeSpider = 1f;
            }
        }


        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return;
            SpiderData spiderData = spiderList[__instance];
            switch (spiderData.CustomBehaviorState)
            {
                case CustomBehavior.Idle:
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