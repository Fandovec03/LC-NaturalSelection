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
        static float time = 0;
        static float refreshCDtime = (float)0.2;
        static EnemyAI? closestEnemy;
        static EnemyAI? targetEnemy;
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(SandSpiderAI __instance)
        {
            time += Time.deltaTime;
            refreshCDtime -= Time.deltaTime;

            if (true && refreshCDtime <= 0)
            {
                foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == false && refreshCDtime <= 0)
                    {
                        Script.Logger.LogWarning("Found Duplicate " + enemy.gameObject.name + ", ID: " + enemy.GetInstanceID() + ". Returning. Time:" + time);
                        refreshCDtime = (float)0.2;
                        return;
                    }
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                    {
                        enemyList.Remove(enemy);
                        Script.Logger.LogInfo("Found and removed dead Enemy " + enemy.gameObject.name + ", ID:  " + enemy.GetInstanceID() + "on List.");
                        return;
                    }
                    if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false && enemy.GetInstanceID() != __instance.GetInstanceID())
                    {
                        enemyList.Add(enemy);
                        Script.Logger.LogMessage("Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy.GetInstanceID());
                        return;
                    }
                    if (__instance != null)
                    {
                    }
                }

                for (int i = 0; i < enemyList.Count; i++)
                {
                    if (__instance != null)
                    {
                        RaycastHit hit = new RaycastHit();
                        if (enemyList[i] == null)
                        {
                            Script.Logger.LogError("Detected null enemy in the list. Removing...");
                            enemyList.RemoveAt(i);
                            return;
                        }
                        if (!Physics.Linecast(__instance.gameObject.transform.position, enemyList[i].gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore) && time >= 1)
                        {
                            Script.Logger.LogMessage("LOS check: " + __instance + "Has line of sight on " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID());
                            return;
                        }
                        if (closestEnemy == null)
                        {
                            Script.Logger.LogInfo("No enemy assigned. Assigning " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy.");
                            closestEnemy = enemyList[i];
                            return;
                        }
                        if (closestEnemy == enemyList[i] && time >= 1)
                        {
                            Script.Logger.LogWarning(enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " is already assigned as closestEnemy");
                            return;
                        }
                        if (enemyList[i] != closestEnemy)
                        {
                            if (Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, closestEnemy.transform.position))
                            {
                                closestEnemy = enemyList[i];
                                Script.Logger.LogDebug(Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, closestEnemy.transform.position));
                                if (time >= 1) Script.Logger.LogInfo("Assigned " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID() + " as new closestEnemy. Distance: " + Vector3.Distance(__instance.transform.position, closestEnemy.transform.position));
                            }
                            return;
                        }
                    }
                }
                time = 0;
                refreshCDtime = (float)0.2;
            }
            
            if (closestEnemy != null && __instance != null && Vector3.Distance(__instance.transform.position, closestEnemy.transform.position) < 100f)
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

                    if (targetEnemy == null)
                        {
                            __instance.StopChasing();
                        }
                    if (__instance.onWall)
                        {
                            __instance.movingTowardsTargetPlayer = true;
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                        }
                    }
                }
        }

        
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            if (targetEnemy != null)
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
        }
        
    }
}