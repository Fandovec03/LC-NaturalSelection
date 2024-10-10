using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float time = 0f;
        static float refreshCDtime = 1f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        static EnemyAI? closestEnemy;

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
                        if (debugMode) Script.Logger.LogWarning(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found Duplicate " + enemy.gameObject.name + ", ID: " + enemy.GetInstanceID() + ". Returning.");
                    }
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                    {
                        enemyList.Remove(enemy);
                        Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found and removed dead Enemy " + enemy.gameObject.name + ", ID:  " + enemy.GetInstanceID() + "on List.");
                    }
                    if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false && enemy.name != __instance.name)
                    {
                        enemyList.Add(enemy);
                        Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy.GetInstanceID());
                    }
                }

                for (int i = 0; i < enemyList.Count; i++)
                {
                    if (__instance != null)
                    {
                        RaycastHit hit = new RaycastHit();
                        if (enemyList[i] == null)
                        {
                            Script.Logger.LogWarning(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Detected null enemy in the list. Removing...");
                            enemyList.RemoveAt(i);
                        }
                        if (enemyList[i] != null)
                        {
                            if (!Physics.Linecast(__instance.gameObject.transform.position, enemyList[i].gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore))
                            {
                                if (debugMode) Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "LOS check: Have LOS on " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID());
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
            closestEnemy = importClosestEnemy;

            for (int i = 0; i < enemyList.Count; i++)
            {
                if (closestEnemy == null)
                {
                    Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "No enemy assigned. Assigning " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy.");
                    closestEnemy = enemyList[i];
                }
                if (closestEnemy == enemyList[i])
                {
                    if (debugMode) Script.Logger.LogWarning(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " is already assigned as closestEnemy");
                }
                if (enemyList[i] != closestEnemy)
                {
                    if (Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, closestEnemy.transform.position))
                    {
                        closestEnemy = enemyList[i];
                        Script.Logger.LogDebug(Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, closestEnemy.transform.position));
                        if (debugMode) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Assigned " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy. Distance: " + Vector3.Distance(__instance.transform.position, closestEnemy.transform.position));
                    }
                }
            }
            return closestEnemy;
        }
        public static List<EnemyAI> filterEnemyList(List<EnemyAI> enemyList, List<Type> targetTypes, EnemyAI __instance)
        {
            List<EnemyAI> filteredList = new List<EnemyAI>();

            for (int i = 0; i < enemyList.Count; i++)
                {
                    if (targetTypes.Contains(enemyList[i].GetType()))
                    {
                        if (debugMode) Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Enemy of type " + enemyList[i].GetType() + " passed the filter.");
                        filteredList.Add(enemyList[i]);
                    }
                    else if (debugMode)
                    {
                        Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Caught and filtered out Enemy of type " + enemyList[i].GetType());
                    }
                }
                return filteredList;
        }
    }
}
