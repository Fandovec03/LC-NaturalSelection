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
    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float timeSpider = 0;
        static float refreshCDtimeSpider = 1f;
        static EnemyAI? closestEnemy;
        static EnemyAI? targetEnemy;

        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool spiderHuntHoardingbug = Script.BoundingConfig.spiderHuntHoardingbug.Value;

        static bool isInWallState = false;
        static float returningFromWallState = 0f;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(SandSpiderAI __instance)
        {
            refreshCDtimeSpider -= Time.deltaTime;
            timeSpider += Time.deltaTime;

            if (!enableSpider) return;

            if (refreshCDtimeSpider <= 0)
            {
                enemyList = EnemyAIPatch.GetInsideEnemyList(__instance);
                closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);
            }

            if (spiderHuntHoardingbug && closestEnemy != null && __instance != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 80f && refreshCDtimeSpider <= 0)
            {
                if (closestEnemy is HoarderBugAI)
                {
                    targetEnemy = closestEnemy;
                    /*__instance.setDestinationToHomeBase = false;
                    __instance.reachedWallPosition = false;
                    __instance.lookingForWallPosition = false;
                    __instance.waitOnWallTimer = 11f;*/

                    if (__instance.spoolingPlayerBody)
                    {
                        __instance.CancelSpoolingBody();
                    }

                    if (targetEnemy == null || targetEnemy.isEnemyDead)
                    {
                        __instance.StopChasing();
                    }
                    if (__instance.onWall)
                    {
                        __instance.agent.speed = 4.25f;
                        __instance.spiderSpeed = 4.25f;
                    }
                }
            }

            if (refreshCDtimeSpider <= 0)
            {
                refreshCDtimeSpider = 1f;
            }
            if (timeSpider > 1)
            {
                timeSpider = 0;
            }
        }


        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return;

#pragma warning disable CS8602 // Přístup přes ukazatel k možnému odkazu s hodnotou null
            if (targetEnemy != null || targetEnemy.isEnemyDead)
            {
                if (__instance.patrolHomeBase.inProgress)
                {
                    __instance.StopSearch(__instance.patrolHomeBase);
                }
                if (targetEnemy.isEnemyDead || !__instance.SetDestinationToPosition(targetEnemy.transform.position, true))  
                {
                    targetEnemy = null;
                    __instance.StopChasing();
                }
                __instance.SetDestinationToPosition(targetEnemy.transform.position, true);
            }
#pragma warning restore CS8602 // Přístup přes ukazatel k možnému odkazu s hodnotou null
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        static void MeshContainerPositionFix(SandSpiderAI __instance)
        {
            Script.Logger.LogInfo(Vector3.Distance(__instance.meshContainerPosition, __instance.transform.position));

            if (!__instance.lookingForWallPosition && !__instance.gotWallPositionInLOS && !isInWallState)
            {
                if (Vector3.Distance(__instance.meshContainerPosition, __instance.transform.position - __instance.meshContainerPosition) > 2.5f)
                {
                    __instance.meshContainerPosition = Vector3.Lerp(__instance.transform.position, __instance.meshContainerPosition, Distance(Vector3.Distance(__instance.meshContainerPosition, __instance.transform.position - __instance.meshContainerPosition), 1.5f) * Time.deltaTime);
                }
            }
            if (__instance.lookingForWallPosition && __instance.gotWallPositionInLOS && !isInWallState)
            {
                isInWallState = true;
            }
            if (__instance.lookingForWallPosition && __instance.gotWallPositionInLOS && isInWallState)
            {
                returningFromWallState += Time.deltaTime;

                if (isInWallState && Vector3.Distance(__instance.meshContainerPosition, __instance.transform.position - __instance.meshContainerPosition) < 2f || returningFromWallState > 10f)
                {
                    isInWallState = false;
                    returningFromWallState = 0f;
                }
            }
        }

        static float Distance(float distance, float speed)
        {
            float ratio = speed / distance;
            return ratio;
        }
    }
}