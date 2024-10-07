using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float refreshCDtime = 0.4f;
        static EnemyAI? closestEnemy = null;
        static EnemyAI? targetEnemy = null;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormUpdatePatch(SandWormAI __instance)
        {
            if (refreshCDtime <= 0)
            {
                enemyList = EnemyAIPatch.GetOutsideEnemyList(__instance);
                closestEnemy = EnemyAIPatch.findClosestEnemy(enemyList, closestEnemy, __instance);
            }

            if (closestEnemy is ForestGiantAI || closestEnemy is BaboonBirdAI || closestEnemy is MouthDogAI)
            {
                targetEnemy = closestEnemy;
            }

            if (targetEnemy != null)
            {
                if (__instance.roamMap.inProgress)
                {
                    __instance.StopSearch(__instance.roamMap);
                }
                __instance.SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);

                if (Vector3.Distance(targetEnemy.transform.position, __instance.transform.position) < 10f)
                {
                    __instance.StartEmergeAnimation();
                }
            }
        }
    }
}
