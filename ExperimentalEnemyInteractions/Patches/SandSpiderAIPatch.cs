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
                    __instance.setDestinationToHomeBase = false;
                    __instance.reachedWallPosition = false;
                    __instance.lookingForWallPosition = false;
                    __instance.waitOnWallTimer = 11f;

                    if (__instance.spoolingPlayerBody)
                    {
                        __instance.CancelSpoolingBody();
                    }

                    if (targetEnemy == null || targetEnemy.isEnemyDead)
                    {
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.StopChasing();
                    }
                    if (__instance.onWall)
                    {
                        __instance.movingTowardsTargetPlayer = false;
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
    }
}