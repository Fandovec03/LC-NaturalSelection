using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using NaturalSelection.Generics;
using BepInEx.Logging;
using System.Linq;

namespace NaturalSelection.EnemyPatches
{
    class NutcrackerData : EnemyDataBase
    {
        internal bool SeeMovingEnemy = false;
        internal Vector3 lastSeenEnemyPosition = UnityEngine.Vector3.zero;
        internal float TimeSinceSeeingMonster = 0f;
        internal float TimeSinceHittingMonster = 0f;
        internal Dictionary<EnemyAI, float> enemiesInLOS = new Dictionary<EnemyAI, float>();
        internal List<EnemyAI> movingEnemies = new List<EnemyAI>();
    }

    [HarmonyPatch(typeof(NutcrackerEnemyAI))]
    class NutcrackerAIPatch
    {
        //static Dictionary<NutcrackerEnemyAI, NutcrackerData> NutcrackerData = [];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool debugNutcrackers = Script.Bools["debugNutcrackers"];
        static bool debugTriggerFlags = Script.Bools["debugTriggerFlags"];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugNutcrackers") debugNutcrackers = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlags = value;
            //Script.Logger.Log(LogLevel.Message,$"Nutcracker received event. debugNutcrackers = {debugNutcrackers}, debugSpam = {debugSpam}, debugTriggerFlags = {debugTriggerFlags},");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void UpdatePatch(NutcrackerEnemyAI __instance)
        {
            NutcrackerData data = (NutcrackerData)Utilities.GetEnemyData(__instance, new NutcrackerData());
            data.SetOwner(__instance);
            data.Subscribe();
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;

            data.ChangeClosestEnemyAction += getClosestEnemyResult;
            void getClosestEnemyResult(EnemyAI? closestEnemy)
            {
                Script.LogNS(LogLevel.Info, $"Set {closestEnemy} as closestEnemy", __instance);
                string tempStringID = __instance.enemyType.enemyName + __instance.NetworkBehaviourId;
                Utilities.enemyDataDict[tempStringID].closestEnemy = closestEnemy;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void NutcrackerUpdatePostfix(NutcrackerEnemyAI __instance)
        {
            if (__instance.isEnemyDead) return;
            NutcrackerData data = (NutcrackerData)Utilities.GetEnemyData(__instance, new NutcrackerData());

            data.enemiesInLOS = new Dictionary<EnemyAI, float>(LibraryCalls.GetEnemiesInLOS(__instance, 360, 30).Where(x => x.Key.agent.velocity.normalized.magnitude > 0));
            List<EnemyAI> enemyList = data.enemiesInLOS.Keys.ToList();
            LibraryCalls.GetInsideOrOutsideEnemyList(ref enemyList, __instance);
            //data.movingEnemies = enemyList;

            if (Script.useCoroutines)
            {
                if (data.coroutineTimer < Time.realtimeSinceStartup) { __instance.StartCoroutine(LibraryCalls.FindClosestEnemyEnumerator(data.ChangeClosestEnemyAction, enemyList, data.closestEnemy, __instance, usePathLenghtAsDistance: true)); data.coroutineTimer = Time.realtimeSinceStartup + 0.2f; }
            }
            else
            {
                data.closestEnemy = LibraryCalls.FindClosestEnemy(ref enemyList, data.closestEnemy, __instance, usePathLenghtAsDistance: Script.usePathToFindClosestEnemy);
            }
            //data.closestEnemy = LibraryCalls.FindClosestEnemy(ref enemyList, data.closestEnemy, __instance);

            if (__instance.currentBehaviourStateIndex == 1)
            {
                if (__instance.isInspecting && enemyList.Count > 0)
                {
                    __instance.isInspecting = false;
                    data.SeeMovingEnemy = true;
                    data.targetEnemy = data.closestEnemy;
                    if (data.targetEnemy != null) __instance.lastSeenPlayerPos = data.targetEnemy.transform.position;
                }
                return;
            }
            if (__instance.currentBehaviourStateIndex == 2)
            {
                if (data.SeeMovingEnemy)
                {
                    __instance.StopInspection();
                }
                if (data.closestEnemy != null) data.targetEnemy = data.closestEnemy;
                if (data.targetEnemy != null) __instance.lastSeenPlayerPos = data.targetEnemy.transform.position;

                __instance.SwitchToBehaviourState(2);

                if (__instance.lostPlayerInChase)
                {
                    __instance.targetTorsoDegrees = 0;
                }
                else
                {
                    __instance.SetTargetDegreesToPosition(data.lastSeenEnemyPosition - __instance.transform.position);
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
                            __instance.AimGunServerRpc(__instance.transform.position - __instance.transform.position);
                        }
                        if (__instance.lostPlayerInChase)
                        {
                            __instance.SetLostPlayerInChaseServerRpc(false);
                        }
                        data.TimeSinceSeeingMonster = 0f;
                        data.lastSeenEnemyPosition = data.targetEnemy.transform.position;
                        data.targetEnemy = null;
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
            NutcrackerData data = (NutcrackerData)Utilities.GetEnemyData(__instance, new NutcrackerData());

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
                        data.targetEnemy = null;
                    }
                }
            }
        }
    }
}
