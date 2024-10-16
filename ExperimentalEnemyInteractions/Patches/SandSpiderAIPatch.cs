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
                spiderData.enemyList = EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(__instance),__instance);
                spiderData.closestEnemy = EnemyAIPatch.findClosestEnemy(spiderData.enemyList, spiderData.closestEnemy, __instance);
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

            if (spiderData.targetEnemy == null || spiderData.targetEnemy.isEnemyDead)
            {
                //__instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, true);
            }
        }
    }
}