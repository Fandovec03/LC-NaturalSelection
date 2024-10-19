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

namespace ExperimentalEnemyInteractions.Patches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public CustomBehavior CustomBehaviorState;
        public float LookAtEnemyTimer = 0f;
        public SortedList<EnemyAI,float> enemiesInLOSSortList = new SortedList<EnemyAI, float>();
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

        static float refreshLOS = 0.2f;

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
        static void UpdatePrefixPatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];
            
            if (refreshLOS <=0)
            {
                spiderData.enemiesInLOSSortList = EnemyAIPatch.CheckLOSForEnemies(__instance, spiderData.enemyList, 80f, 15, 2f);
                refreshLOS = 0.2f;
            }
            else
            {
                refreshLOS -= Time.deltaTime;
            }

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;



            if (refreshCDtimeSpider <= 0)
            {
                refreshCDtimeSpider = 1f;
            }
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfixPatch(SandSpiderAI __instance)
        {
            SpiderData spiderData = spiderList[__instance];
            

            refreshCDtimeSpider -= Time.deltaTime;

            if (!enableSpider) return;

            switch(spiderData.CustomBehaviorState)
            {
                case CustomBehavior.Idle:
                    break;
                case CustomBehavior.Patrol:
                    if (spiderData.enemiesInLOSSortList.Count > 0)
                    {
                        if (spiderData.enemiesInLOSSortList.Keys.First() is HoarderBugAI)
                        {
                            spiderData.targetEnemy = spiderData.enemiesInLOSSortList.Keys.First();
                            SwitchCustomState(__instance, CustomBehavior.Chase);
                            break;
                        }
                        else
                        {
                            spiderData.targetEnemy = null;
                            break;
                        }
                    }
                    break;
                case CustomBehavior.Chase:
                    {
                        if (spiderData.targetEnemy != null)
                        {
                            __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, checkForPath: true);
                        }
                        else
                        {
                            SwitchCustomState(__instance, CustomBehavior.Patrol);
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
        static void DoAIIntervalPrefix(SandSpiderAI __instance)
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

                }
                break;
                case CustomBehavior.Chase:
                {

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

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {

        }
    }
}