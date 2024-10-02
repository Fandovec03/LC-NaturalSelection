using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;

namespace ExperimentalEnemyInteractions.Patches
{



    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static bool IsTargetEnemyNull(EnemyAI enemy)
        {
            if (enemy != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch("Update")]
        static void Postfix(SandSpiderAI __instance)
        {
            EnemyAI? targetEnemy = null;
 

                switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    if (__instance.targetPlayer == null && IsTargetEnemyNull(targetEnemy) == true)
                    {
                        foreach(EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                        {
                            RaycastHit hit;
                            Script.Logger.LogInfo(__instance.gameObject.name + " triggered listing spawned enemies. Item: " + enemy.gameObject.name + ", LOS: " + !Physics.Linecast(__instance.gameObject.transform.position, enemy.gameObject.transform.position, out hit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) + ", hit: " + hit.collider.gameObject.name);

                            if (!Physics.Linecast(__instance.gameObject.transform.position, enemy.gameObject.transform.position, out hit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                            {
                                __instance.SetDestinationToPosition(enemy.gameObject.transform.position);
                            }
                        }
                        
                    }
                    break;
            }
        }
    }
}
