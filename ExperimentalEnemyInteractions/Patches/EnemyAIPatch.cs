﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    class EnemyAIData
    {
        public EnemyAI? closestEnemy;
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float time = 0f;
        static float refreshCDtime = 1f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;

        static Dictionary<EnemyAI, EnemyAIData> enemyAIData = [];

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void EnemyAIStartPatch(EnemyAI __instance)
        {
            enemyAIData.Add(__instance, new EnemyAIData());
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfixPatch(EnemyAI __instance)
        {
            time += Time.deltaTime;
            refreshCDtime -= Time.deltaTime;
            
            if (refreshCDtime <= 0)
            {
                foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == false)
                    {
                        if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found Duplicate " + enemy.gameObject.name + ", ID: " + enemy.GetInstanceID());
                    }
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                    {
                        enemyList.Remove(enemy);
                        if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found and removed dead Enemy " + enemy.gameObject.name + ", ID:  " + enemy.GetInstanceID() + "on List.");
                    }
                    if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false && enemy.name != __instance.name)
                    {
                        enemyList.Add(enemy);
                        if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy.GetInstanceID());
                    }
                }

                for (int i = 0; i < enemyList.Count; i++)
                {
                    if (__instance != null)
                    {
                        RaycastHit hit = new RaycastHit();
                        if (enemyList[i] == null)
                        {
                            if (debugMode) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Detected null enemy in the list. Removing...");
                            enemyList.RemoveAt(i);
                        }
                        if (enemyList[i] != null)
                        {
                            if (!Physics.Linecast(__instance.gameObject.transform.position, enemyList[i].gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore))
                            {
                                if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "LOS check: Have LOS on " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID());
                            }
                        }
                    }
                }
                refreshCDtime = 1f;
            }
        }

        public static List<EnemyAI> GetCompleteList()
        {
            return enemyList;
        }

        public static List<EnemyAI> GetOutsideEnemyList(EnemyAI instance)
        {
            List<EnemyAI> outsideEnemies = new List<EnemyAI>();

            foreach (EnemyAI enemy in enemyList )
            {
                if (enemy.isOutside == true && enemy != instance)
                {
                    outsideEnemies.Add(enemy);
                }
            }

            return outsideEnemies;
        }

        public static List<EnemyAI> GetInsideEnemyList(EnemyAI instance)
        {
            List<EnemyAI> insideEnemies = new List<EnemyAI>();

            foreach (EnemyAI enemy in enemyList)
            {
                if (enemy.isOutside == false && enemy != instance)
                {
                    insideEnemies.Add(enemy);
                }
            }

            return insideEnemies;
        }

        public static EnemyAI? findClosestEnemy(List<EnemyAI> enemyList, EnemyAI? importClosestEnemy, EnemyAI __instance)
        {
            EnemyAIData enemyAIdata = enemyAIData[__instance];

            enemyAIdata.closestEnemy = importClosestEnemy;

            for (int i = 0; i < enemyList.Count; i++)
            {
                if (enemyAIdata.closestEnemy == null)
                {
                    if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "No enemy assigned. Assigning " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy.");
                    enemyAIdata.closestEnemy = enemyList[i];
                }
                if (enemyAIdata.closestEnemy == enemyList[i])
                {
                    if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " is already assigned as closestEnemy");
                }
                if (enemyList[i] != enemyAIdata.closestEnemy)
                {
                    if (Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, enemyAIdata.closestEnemy.transform.position))
                    {
                        enemyAIdata.closestEnemy = enemyList[i];
                        if (debugMode) Script.Logger.LogDebug(Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, enemyAIdata.closestEnemy.transform.position));
                        if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Assigned " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy. Distance: " + Vector3.Distance(__instance.transform.position, enemyAIdata.closestEnemy.transform.position));
                    }
                }
            }
            return enemyAIdata.closestEnemy;
        }
        public static List<EnemyAI> filterEnemyList(List<EnemyAI> enemyList, List<Type> targetTypes, EnemyAI __instance)
        {
            List<EnemyAI> filteredList = new List<EnemyAI>();

            for (int i = 0; i < enemyList.Count; i++)
                {
                    if (targetTypes.Contains(enemyList[i].GetType()))
                    {
                        if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Enemy of type " + enemyList[i].GetType() + " passed the filter.");
                        filteredList.Add(enemyList[i]);
                    }
                    else if (debugMode)
                    {
                    if (debugMode) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Caught and filtered out Enemy of type " + enemyList[i].GetType());
                    }
                }
                return filteredList;
        }

        static public EnemyAI CheckLOSForEnemies(EnemyAI instance, List<EnemyAI> enemyList, float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            if (instance.isOutside && !instance.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                //range = Mathf.Clamp(range, 0, 30);
            }
            for (int i = 0; i < enemyList.Count; i++)
            {
                Vector3 position = enemyList[i].transform.position;
                if (Vector3.Distance(position, instance.eye.position) < (float)range && !Physics.Linecast(instance.eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    Vector3 to = position - instance.eye.position;
                    if (Vector3.Angle(instance.eye.forward, to) < width || proximityAwareness != -1 && Vector3.Distance(instance.eye.position, position) < (float)proximityAwareness)
                    {
                        return enemyList[i];
                    }
                }
            }
            return null;
        }
    }
}
