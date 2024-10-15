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
    }


    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
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
        }


        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;

            if (refreshCDtimeSpider <= 0)
            {
                enemyList = EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(),__instance);
                spiderData.closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, spiderData.closestEnemy, __instance);
            }

            if (spiderHuntHoardingbug && spiderData.closestEnemy != null && __instance != null && Vector3.Distance(__instance.transform.position, spiderData.closestEnemy.transform.position) < 80f && refreshCDtimeSpider <= 0)
            {
                if (spiderData.closestEnemy is HoarderBugAI)
                {
                    spiderData.targetEnemy = spiderData.closestEnemy;
                    /*__instance.setDestinationToHomeBase = false;
                    __instance.reachedWallPosition = false;
                    __instance.lookingForWallPosition = false;
                    __instance.waitOnWallTimer = 11f;*/

                    if (__instance.spoolingPlayerBody)
                    {
                        __instance.CancelSpoolingBody();
                    }

                    if (spiderData.targetEnemy == null || spiderData.targetEnemy.isEnemyDead)
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
        }


        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return;
            SpiderData spiderData = spiderList[__instance];

#pragma warning disable CS8602 // Přístup přes ukazatel k možnému odkazu s hodnotou null
            if (spiderData.targetEnemy != null || spiderData.targetEnemy.isEnemyDead)
            {
                if (__instance.patrolHomeBase.inProgress)
                {
                    __instance.StopSearch(__instance.patrolHomeBase);
                }
                if (spiderData.targetEnemy.isEnemyDead || !__instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true))  
                {
                    spiderData.targetEnemy = null;
                    __instance.StopChasing();
                }
                __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true);
            }
#pragma warning restore CS8602 // Přístup přes ukazatel k možnému odkazu s hodnotou null
        }
    }
}