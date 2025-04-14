using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using NaturalSelection.Generics;

namespace NaturalSelection.EnemyPatches
{
    class NutcrackerData()
    {
        internal EnemyAI? closestEnemy = null;
        internal EnemyAI? targetEnemy = null;
        internal bool SeeMovingEnemy = false;
        internal Vector3 lastSeenEnemyPosition = UnityEngine.Vector3.zero;
        internal float TimeSinceSeeingMonster = 0f;
        internal float TimeSinceHittingMonster = 0f;
    }

    [HarmonyPatch(typeof(NutcrackerEnemyAI))]
    class NutcrackerAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static Dictionary<NutcrackerEnemyAI, NutcrackerData> NutcrackerData = [];
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugNutcrackers = Script.BoundingConfig.debugNutcrackers.Value;
        static bool debugTriggerFlags = Script.debugTriggerFlags;

        static public bool CheckLOSForMonsters(Vector3 monsterPosition, NutcrackerEnemyAI __instance, float width = 45f, int range = 60, int proximityAwareness = 60)
        {
            if (Vector3.Distance(monsterPosition, __instance.eye.position) < (float)range && !Physics.Linecast(__instance.eye.position, monsterPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                Vector3 to = monsterPosition - __instance.eye.position;
                if (Vector3.Angle(__instance.eye.forward, to) < width || (proximityAwareness != -1 && UnityEngine.Vector3.Distance(__instance.eye.position, monsterPosition) < (float)proximityAwareness))
                {
                    return true;
                }
            }
            return false;
        }



        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void UpdatePatch(NutcrackerEnemyAI __instance)
        {
            if (!NutcrackerData.ContainsKey(__instance))
            {
                NutcrackerData.Add(__instance, new NutcrackerData());
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void NutcrackerUpdatePostfix(NutcrackerEnemyAI __instance)
        {
            NutcrackerData data = NutcrackerData[__instance];

            enemyList = LibraryCalls.GetInsideOrOutsideEnemyList(LibraryCalls.GetCompleteList(__instance),__instance);

            data.closestEnemy = LibraryCalls.FindClosestEnemy(enemyList, data.closestEnemy, __instance);


            if (__instance.currentBehaviourStateIndex == 1)
            {
                if (data.closestEnemy != null)
                {
                    if (__instance.isInspecting && CheckLOSForMonsters(data.closestEnemy.transform.position, __instance, 70f, 60, 1) && data.closestEnemy.agent.velocity.magnitude > 0)
                    {
                        __instance.isInspecting = false;
                        data.SeeMovingEnemy = true;
                        __instance.lastSeenPlayerPos = data.closestEnemy.transform.position;
                    }
                    return;
                }
            }
            if (__instance.currentBehaviourStateIndex == 2)
            {
                if (data.SeeMovingEnemy)
                {
                    __instance.StopInspection();
                }
                __instance.SwitchToBehaviourState(2);
                if (__instance.lostPlayerInChase)
                {
                    __instance.targetTorsoDegrees = 0;
                }
                else
                {
                    __instance.SetTargetDegreesToPosition(data.lastSeenEnemyPosition);
                }
                if (data.targetEnemy != null)
                {
                    if (__instance.CheckLineOfSightForPosition(data.targetEnemy.transform.position, 70f, 60, 1f))
                    {
                        data.TimeSinceSeeingMonster = 0f;
                        data.lastSeenEnemyPosition = data.targetEnemy.transform.position;
                        __instance.creatureAnimator.SetBool("AimDown", Vector3.Distance(data.lastSeenEnemyPosition, __instance.transform.position) < 2f && data.lastSeenEnemyPosition.y < 1f);
                    }
                    if (!__instance.CheckLineOfSightForPosition(data.targetEnemy.transform.position, 70f, 25, 1))
                    {
                        return;
                    }
                    if (data.targetEnemy && data.TimeSinceSeeingMonster < 8f && __instance.timeSinceSeeingTarget < 8f)
                    {
                        if (__instance.timeSinceFiringGun > 0.75f && !__instance.reloadingGun && !__instance.aimingGun && data.TimeSinceHittingMonster > 1f && Vector3.Angle(__instance.gun.shotgunRayPoint.forward, data.targetEnemy.transform.position - __instance.gun.shotgunRayPoint.position) < 30f)
                        {
                            __instance.timeSinceFiringGun = 0f;
                            __instance.agent.speed = 0f;
                            __instance.AimGunServerRpc(__instance.transform.position);
                        }
                        if (__instance.lostPlayerInChase)
                        {
                            __instance.SetLostPlayerInChaseServerRpc(false);
                        }
                        data.TimeSinceSeeingMonster = 0f;
                        data.lastSeenEnemyPosition = data.targetEnemy.transform.position;
                    }
                    else if (data.targetEnemy.agent.velocity.magnitude > 0)
                    {
                        data.TimeSinceSeeingMonster = 0f;
                    }
                    return;
                }
            }

        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPatch(NutcrackerEnemyAI __instance)
        {
            NutcrackerData data = NutcrackerData[__instance];

            if (__instance.currentBehaviourStateIndex == 2)
            {
                if (data.targetEnemy != null)
                {
                    if (data.TimeSinceSeeingMonster < 0.5f && __instance.timeSinceSeeingTarget < 0.5f)
                    {
                        if (__instance.attackSearch.inProgress)
                        {
                            __instance.StopSearch(__instance.attackSearch);
                        }
                        __instance.reachedStrafePosition = false;
                        __instance.SetDestinationToPosition(data.targetEnemy.transform.position);
                        __instance.agent.stoppingDistance = 1f;
                        __instance.moveTowardsDestination = true;

                    }
                    if (data.TimeSinceSeeingMonster > 12f && __instance.timeSinceSeeingTarget > 12f)
                    {
                        __instance.SwitchToBehaviourState(1);
                    }
                }
            }
        }
    }
}
