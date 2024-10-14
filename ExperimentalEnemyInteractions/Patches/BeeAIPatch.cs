using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    class BeeValues
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public EnemyAI? closestToBeehive = null;
        public GrabbableObject? hive = null;
        public Vector3 lastKnownHivePosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public float timeSinceHittingEnemy = 0f;
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            beeList.Add(__instance, new BeeValues());
            beeList[__instance].enemyList = EnemyAIPatch.GetOutsideEnemyList(__instance);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];
            beeData.timeSinceHittingEnemy += Time.deltaTime;
            __instance.attackZapModeTimer -= Time.deltaTime;

            switch (beeData.customBehaviorStateIndex)
            {

            }

        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            switch (beeData.customBehaviorStateIndex)
            {
                case 0:
                    EnemyAI enemyAI = EnemyAIPatch.CheckLOSForEnemies(__instance, beeData.enemyList, 360f, 16, 1);

                    Script.Logger.LogDebug("case0: Checked LOS for enemies. Enemy found: " + enemyAI);

                    if (enemyAI != null && Vector3.Distance(enemyAI.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                    {
                        __instance.SetDestinationToPosition(enemyAI.transform.position, true);
                        __instance.moveTowardsDestination = true;
                        Script.Logger.LogDebug("case0: Moving towards " + enemyAI);

                        beeData.customBehaviorStateIndex = 1;
                        Script.Logger.LogDebug("case0: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                    }
                    return false;
                case 1:
                    if (beeData.targetEnemy == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {
                        if (__instance.IsHiveMissing())
                        {
                            beeData.customBehaviorStateIndex = 2;
                            Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                    }
                    return false;
                case 2:
                    if (__instance.IsHivePlacedAndInLOS())
                    {
                        Collider[] collisionArray = Physics.OverlapSphere(__instance.transform.position, __instance.defenseDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Collide);
                        EnemyAI? targetEnemy2 = null;
                        if (collisionArray != null && collisionArray.Length != 0)
                        {
                            for (int i = 0; i < collisionArray.Length; i++)
                            {
                               targetEnemy2 = collisionArray[i].gameObject.GetComponent<EnemyAI>();
                                if (targetEnemy2 != null)
                                {
                                    Script.Logger.LogDebug("case2: targetenemy2: " + targetEnemy2);
                                    break;
                                }
                            }
                        }
                        if (targetEnemy2 != null && Vector3.Distance(targetEnemy2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(targetEnemy2.transform.position, true);
                            __instance.moveTowardsDestination = true;
                            Script.Logger.LogDebug("case2: Moving towards: " + targetEnemy2);
                            beeData.customBehaviorStateIndex = 1;
                            Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        break;
                    }
                    if (beeData.targetEnemy != null)
                    {
                        __instance.agent.acceleration = 16f;
                        if (!EnemyAIPatch.CheckLOSForEnemies(__instance, beeData.enemyList, 360f, 16, 1))
                        {
                            Script.Logger.LogDebug("case2: Checking LOS for enemies.");

                            __instance.lostLOSTimer += __instance.AIIntervalTime;
                            if (__instance.lostLOSTimer > 4.5f)
                            {
                                beeData.targetEnemy = null;
                                Script.Logger.LogDebug("case2: No target found.");
                                __instance.lostLOSTimer = 0;
                            }
                        }
                        else
                        {
                            __instance.lostLOSTimer = 0;
                        }
                        break;
                    }
                    __instance.agent.acceleration = 13f;
                    {
                        if (!__instance.searchForHive.inProgress)
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                        }
                    }
                    return false;
            }
            return true;
        }
    }
}
