using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static  float time = 0;
        static float forEachCDTimer = 0;
        static EnemyAI? targetEnemy;

        [HarmonyPatch("Update")]
        static void Postfix(SandSpiderAI __instance)
        { 
            time += Time.deltaTime;
                    if (true)
                     {
                        foreach(EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                        {
                                if (enemyList.Contains(enemy) && enemy.isEnemyDead == false && forEachCDTimer <= 0)
                                {
                                Script.Logger.LogWarning("Found Duplicate " + enemy.gameObject.name + ", ID: " + enemy.GetInstanceID() + ". Returning");
                                    //return; 
                                }
                                if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                                {
                                    enemyList.Remove(enemy); 
                                    Script.Logger.LogInfo("Found and removed dead Enemy " + enemy.gameObject.name + ", ID:  " + enemy.GetInstanceID() + "on List.");
                                }
                                if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false)
                                {
                                    enemyList.Add(enemy);
                                    Script.Logger.LogMessage("Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy.GetInstanceID());
                                }
                                if (__instance != null)
                                {
                                }
                        }
                        forEachCDTimer -= Time.deltaTime;
                        if (forEachCDTimer <= 0) forEachCDTimer = 2;

                    for (int i = 0; i < enemyList.Count; i++)
                        {
                            if (__instance != null)
                            {
                                RaycastHit hit = new RaycastHit();
                                if (enemyList[i] == null)
                                    {
                                        Script.Logger.LogError("Detected null enemy in the list. Removing...");
                                        enemyList.RemoveAt(i);
                                    }
                                if (!Physics.Linecast(__instance.gameObject.transform.position, enemyList[i].gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore) && time >= 1)
                                {
                                    Script.Logger.LogMessage("LOS check: " + __instance + "Has line of sight on " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID());
                                }
                                if (targetEnemy == null)
                                {
                                    Script.Logger.LogInfo("No enemy assigned. Assigning " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new targetEnemy.");
                                    targetEnemy = enemyList[i];
                                }
                                if (targetEnemy == enemyList[i] && time >= 1)
                                {
                                    Script.Logger.LogWarning( enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " is already assigned as targetEnemy");
                                }
                                if (enemyList[i] != targetEnemy)
                                {
                                    if (Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, targetEnemy.transform.position))
                                    { 
                                        targetEnemy = enemyList[i];
                                        Script.Logger.LogInfo("Assigned " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new targetEnemy. Distance: " + Vector3.Distance(__instance.transform.position, targetEnemy.transform.position));
                                    }        
                                }
                            }
                        }
                if (time > 1)
                {
                    time = 0;
                    Script.Logger.LogInfo("Reset time.");
                }
            }
        }

    }
}