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
        public static List<EnemyAI> enemyList = new List<EnemyAI>();
        static int logInterval = 0;
        static int listInterval = 0;
        static int listInterval2 = 0;
        static int listInterval3 = 0;


        [HarmonyPatch("Update")]
        static void Postfix(SandSpiderAI __instance)
        {
            EnemyAI? targetEnemy = null;
            
            

              /*  switch (__instance.currentBehaviourStateIndex)
            {
                case 0:*/
                    if (/*__instance.targetPlayer == null && targetEnemy != null*/true)
                    {
                        foreach(EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                        {
                                if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false)
                                {
                                    enemyList.Add(enemy);
                                    Script.Logger.LogMessage("Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy);
                                }
                                if (enemyList.Contains(enemy) && enemy.isEnemyDead == false)
                                {
                                    if (listInterval >= 90)
                                    {
                                        Script.Logger.LogWarning("Found Duplicate " + enemy.gameObject.name + " in List. Instance: " + enemy);
                                        listInterval = 0;
                                    }
                                }
                                else listInterval++;
                                if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                                {
                                    enemyList.Remove(enemy);
                                if (listInterval2 >= 90)
                                {
                                    Script.Logger.LogInfo("Found and removeddead Enemy " + enemy.gameObject.name + " in List. Instance: " + enemy);
                                    listInterval2 = 0;
                                }
                                else listInterval2++;
                                }
                                    if (__instance != null)
                                        {
                                        RaycastHit hit = new RaycastHit();
                                        if (logInterval >= 90)
                                        {
                                        Script.Logger.LogInfo(__instance + " triggered listing spawned enemies. Item: " + enemy.gameObject.name);
                                            if (!Physics.Linecast(__instance.gameObject.transform.position, enemy.gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore))
                                            {
                                                Script.Logger.LogInfo("LOS check: True");
                                            }
                                                logInterval = 0;
                                        }
                                        else logInterval++;
                                }
                                /*else
                                {
                                    Script.Logger.LogInfo(__instance);
                                    Script.Logger.LogInfo(enemy);
                                }*/
                        }


                EnemyAI compare1;
                EnemyAI compare2;
                        for (int i = 0; i < enemyList.Count; i++)
                        {
                            if (__instance != null)
                            {
                                if (Vector3.Distance(__instance.transform.position, enemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, targetEnemy.transform.position))
                                {
                                    targetEnemy = enemyList[i];
                                    if (listInterval3 < 1)
                                    {
                                    Script.Logger.LogInfo("Assigned " + enemyList[i] + " as targetEnemy. Distance: " + Vector3.Distance(__instance.transform.position, targetEnemy.transform.position));
                                    }
                                    if (listInterval3 <= 30)
                                    {
                                        listInterval3++;
                                    }
                                    if (listInterval3 > 30)
                                    {
                                    listInterval3 = 0;
                                    }
                        }
                            }
                        }
                   /* break;
                case 4:
                    break;*/
                   
            }
        }
    }
}
