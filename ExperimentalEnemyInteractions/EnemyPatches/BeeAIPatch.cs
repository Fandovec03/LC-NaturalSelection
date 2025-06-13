using HarmonyLib;
using LethalNetworkAPI;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{
    class BeeValues()
    {
        internal EnemyAI? closestEnemy = null;
        internal EnemyAI? targetEnemy = null;
        internal Vector3 lastKnownEnemyPosition = Vector3.zero;
        internal int customBehaviorStateIndex = 0;
        internal Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
        internal float LostLOSOfEnemy = 0f;
        internal float delayTimer = 0.2f;
    }

    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static bool logBees = Script.Bools["debugRedBees"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool debugTriggers = Script.Bools["debugTriggerFlags"];
        static List<string> beeBlacklist = InitializeGamePatch.beeBlacklistFinal;


        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugRedBees") logBees = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") debugTriggers = value;
            //Script.Logger.Log(LogLevel.Message,$"Curcuit received event. logBees = {logBees}, debugSpam = {debugSpam}, debugTriggers = {debugTriggers}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            if (!beeList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                beeList.Add(__instance, new BeeValues());
            }
            BeeValues beeData = beeList[__instance];

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(RedLocustBees __instance)
        {
            if (!beeList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                beeList.Add(__instance, new BeeValues());
            }
            BeeValues beeData = beeList[__instance];
            if (RoundManagerPatch.RequestUpdate(__instance) == true)
            {
                List<EnemyAI> tempList = LibraryCalls.GetCompleteList(__instance);

                LibraryCalls.FilterEnemyList(ref tempList, beeBlacklist, __instance, Script.BoundingConfig.IgnoreImmortalEnemies.Value);
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, ref tempList);
            }
            foreach (KeyValuePair<EnemyAI, float> enemy in new Dictionary<EnemyAI, float>(beeData.hitRegistry))
            {
                if (enemy.Value > 1.7f)
                {
                    beeData.hitRegistry.Remove(enemy.Key); continue;
                }
                beeData.hitRegistry[enemy.Key] += Time.deltaTime;
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefixPatch(RedLocustBees __instance)
        {
            CheckDataIntegrityBees(__instance);
            BeeValues beeData = beeList[__instance];
        
        if (beeData.targetEnemy != null && __instance.movingTowardsTargetPlayer == false && beeData.customBehaviorStateIndex != 0)
        {
            if (logBees && debugSpam && debugTriggers) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Prefix triggered false");

            if (__instance.moveTowardsDestination)
            {
                __instance.agent.SetDestination(__instance.destination);
            }
            __instance.SyncPositionToClients();
            return false;
        }
        if (logBees && debugSpam && debugTriggers) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Prefix triggered true");
        return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
        {
            if (__instance.isEnemyDead) return;
            CheckDataIntegrityBees(__instance);
            BeeValues beeData = beeList[__instance];
            Type type = __instance.GetType();

            List <EnemyAI> tempList = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
            LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);

            Dictionary<EnemyAI, float> enemiesInLOS = new Dictionary<EnemyAI, float>(LibraryCalls.GetEnemiesInLOS(__instance, ref tempList, 360f, 16, 1));

            switch (__instance.currentBehaviourStateIndex)
            {
            case 0:
                {
                    if (enemiesInLOS.Count > 0)
                    {
                        beeData.targetEnemy = enemiesInLOS.Keys.First();
                        if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case0: Checked LOS for enemies. Enemy found: {LibraryCalls.DebugStringHead(beeData.targetEnemy)}");
                    }

                    if (__instance.wasInChase)
                    {
                        __instance.wasInChase = false;
                    }
                    if (Vector3.Distance(__instance.transform.position, __instance.lastKnownHivePosition) > 2f)
                    {
                        __instance.SetDestinationToPosition(__instance.lastKnownHivePosition, true);
                    }
                    if (__instance.IsHiveMissing() || __instance.hive.parentObject != null)
                    {
                        __instance.SwitchToBehaviourState(2);
                        beeData.customBehaviorStateIndex = 2;
                        if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case0: HIVE IS MISSING! CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                        break;
                    }
                    if (beeData.targetEnemy != null && Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance /*&& Vector3.Distance(__instance.targetPlayer.transform.position, __instance.hive.transform.position) < Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position)*/)
                    {
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                        if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case0: Moving towards {beeData.targetEnemy}");

                        beeData.customBehaviorStateIndex = 1;
                        __instance.SwitchToBehaviourState(1);
                        beeData.LostLOSOfEnemy = 0f;
                        if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case0: CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                    }
                    break;
                }
            case 1:
                {
                    if (__instance.targetPlayer != null && __instance.movingTowardsTargetPlayer) return;
                    if (beeData.targetEnemy == null || beeData.targetEnemy.isEnemyDead || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {
                        beeData.targetEnemy = null;
                        __instance.wasInChase = false;
                        if (__instance.IsHiveMissing() || __instance.hive.parentObject != null)
                        {
                            beeData.customBehaviorStateIndex = 2;
                            __instance.SwitchToBehaviourState(2);
                            if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case1: HIVE IS MISSING! CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourState(0);
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case1: CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                        }
                        break;
                    }
                    else if (__instance.hive.parentObject != null)
                    {
                        beeData.customBehaviorStateIndex = 2;
                        __instance.SwitchToBehaviourState(2);
                        break;
                    }

                    __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                    break;
                }
            case 2:
                {
                    if (__instance.targetPlayer != null || __instance.movingTowardsTargetPlayer)
                    {
                        if (logBees && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: target player found or moving towards target player");
                        return;
                    }

                    if (__instance.IsHivePlacedAndInLOS() && !__instance.hive.parentObject)
                    {
                        if (__instance.wasInChase)
                        {
                            __instance.wasInChase = false;
                        }
                        __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;

                        if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: IsHivePlacedAndInLOS triggered");
                        EnemyAI? enemyAI2 = null;
                        Collider[] collisionArray = Physics.OverlapSphere(__instance.hive.transform.position, __instance.defenseDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Collide);

                        if (collisionArray != null && collisionArray.Length > 0)
                        {
                            for (int i = 0; i < collisionArray.Length; i++)
                            {
                                EnemyAI enemy = collisionArray[i].gameObject.GetComponent<EnemyAI>();

                                if (enemy != null && enemy != __instance)
                                {
                                    enemyAI2 = enemy;
                                    if (logBees) Script.Logger.Log(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case2: CollisionArray triggered. Enemy found: {LibraryCalls.DebugStringHead(enemyAI2)}");
                                    break;
                                }
                            }
                        }
                        if (enemyAI2 != null && Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(enemyAI2.transform.position, true);
                            if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case2: Moving towards: {enemyAI2}");
                            beeData.customBehaviorStateIndex = 1;
                            __instance.SwitchToBehaviourState(1);
                            __instance.syncedLastKnownHivePosition = false;
                            __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                            beeData.LostLOSOfEnemy = 0f;
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourState(0);
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: CustomBehaviorStateIndex changed: {beeData.customBehaviorStateIndex}");
                        }
                        break;
                    }

                    bool flag = false;
                    EnemyAI? priorityTarget = ChaseEnemyWithPriorities(ref enemiesInLOS, __instance);

                    if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: {priorityTarget} is closest to hive.");

                    if (priorityTarget != null && beeData.targetEnemy != priorityTarget)
                    {
                        flag = true;
                        __instance.wasInChase = false;
                        beeData.targetEnemy = priorityTarget;
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                        __instance.StopSearch(__instance.searchForHive);
                        __instance.syncedLastKnownHivePosition = false;
                        beeData.LostLOSOfEnemy = 0f;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                        if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case2: Targeting {priorityTarget}. Synced hive position");
                        break;
                    }
                    if (beeData.targetEnemy != null)
                    {
                        __instance.agent.acceleration = 16f;
                        if (!flag && enemiesInLOS.Count == 0)
                        {
                            if (logBees && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost LOS of {beeData.targetEnemy}, started timer.");
                            beeData.LostLOSOfEnemy += __instance.AIIntervalTime;
                            if (beeData.LostLOSOfEnemy >= 4.5f)
                            {
                                beeData.targetEnemy = null;
                                beeData.LostLOSOfEnemy = 0f;
                                if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost LOS of {beeData.targetEnemy}, Stopped and reset timer.");
                            }
                        }
                        else
                        {
                            __instance.wasInChase = true;
                            beeData.lastKnownEnemyPosition = beeData.targetEnemy.transform.position;
                            __instance.SetDestinationToPosition(beeData.lastKnownEnemyPosition, true);
                            beeData.LostLOSOfEnemy = 0f;
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost {beeData.targetEnemy}");

                        }
                        break;
                    }
                    __instance.agent.acceleration = 13f;
                    if (!__instance.searchForHive.inProgress)
                    {
                        if (logBees) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case2: set new search for hive");
                        if (__instance.wasInChase)
                        {
                            __instance.StartSearch(beeData.lastKnownEnemyPosition, __instance.searchForHive);
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: Started search for hive.");
                        }
                        else
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                            if (logBees) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: Started search for hive.");
                        }
                    }
                    break;
                }
            }
        }

        static LNetworkVariable<float> NSSetOnFireChance(RedLocustBees instance)
        {
            string NWID = "NSSetOnFireChance" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<float>(NWID);
        }
        static LNetworkVariable<float> NSSetOnFireMaxChance(RedLocustBees instance)
        {
            string NWID = "NSSetOnFireMaxChance" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<float>(NWID);
        }

        static LNetworkEvent NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSSetGiantOnFire" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        public static void CheckDataIntegrityBees(RedLocustBees __instance)
        {
            if (!beeList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                beeList.Add(__instance, new BeeValues());
            }
        }

        public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
        {
            if (mainscript2.GetType() == typeof(RedLocustBees)) return;
            if (beeList.ContainsKey(__instance) && !beeBlacklist.Contains(mainscript2.enemyType.enemyName))
            {
                if ((!beeList[__instance].hitRegistry.ContainsKey(mainscript2) ||beeList[__instance].hitRegistry[mainscript2] > 1.7f) && __instance.currentBehaviourStateIndex > 0 && !mainscript2.isEnemyDead || (!beeList[__instance].hitRegistry.ContainsKey(mainscript2) || beeList[__instance].hitRegistry[mainscript2] > 1.2f) && __instance.currentBehaviourStateIndex == 2 && !mainscript2.isEnemyDead)
                {
                    mainscript2.HitEnemy(1, null, playHitSFX: true);

                    if (!beeList[__instance].hitRegistry.ContainsKey(mainscript2)) beeList[__instance].hitRegistry.Add(mainscript2, 0);
                    else beeList[__instance].hitRegistry[mainscript2] = 0;

                    if (mainscript2 is ForestGiantAI && mainscript2.currentBehaviourStateIndex != 2)
                    {
                        if (__instance.IsOwner)
                        {
                            NSSetOnFireChance(__instance).Value = UnityEngine.Random.Range(0f, 100f);

                            if (__instance.currentBehaviourStateIndex != 2)
                            {
                                NSSetOnFireMaxChance(__instance).Value = Script.BoundingConfig.beesSetGiantsOnFireMinChance.Value;
                            }
                            else
                            {
                                NSSetOnFireMaxChance(__instance).Value = Script.BoundingConfig.beesSetGiantsOnFireMaxChance.Value;
                            }
                            Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} OnCustomEnemyCollision: Giant hit. Chance to set on fire: {NSSetOnFireMaxChance(__instance).Value} , rolled {NSSetOnFireChance(__instance)}");;
                        }
                        else
                        {
                            if (logBees) Script.Logger.Log(LogLevel.Message,"Client not elligible to determine chance to set giant on fire");
                        }
                        if (NSSetOnFireChance(__instance).Value <= NSSetOnFireMaxChance(__instance).Value && __instance.IsOwner)
                        {
                            Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} OnCustomEnemyCollision: SET GIANT ON FIRE! Random number: {NSSetOnFireChance(__instance).Value}");
                            ForestGiantAI giant = (ForestGiantAI)mainscript2;

                            /*if (giant.IsOwner)
                            {
                            giant.timeAtStartOfBurning = Time.realtimeSinceStartup;
                            giant.SwitchToBehaviourState(2);
                            Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance) + " /BeesCustomhit/ " + "isOwner: " + __instance.IsOwner + ", Giant isOwner: " + giant.IsOwner + ", set fire to false");
                            }*/
                            NetworkSetGiantOnFire(giant).InvokeServer();
                        }
                    }
                }
            }
        }

        public static EnemyAI? ChaseEnemyWithPriorities(ref Dictionary<EnemyAI, float> enemyDictionary, RedLocustBees instance)
        {
            IVisibleThreat threatCompoment;
            EnemyAI? returnEnemy = null;

            if (enemyDictionary.Count < 1) return null;

            foreach (var enemyAI in enemyDictionary)
            {
                if (enemyAI.Key == instance)
                {
                    continue;
                }
                threatCompoment = enemyAI.Key.GetComponent<IVisibleThreat>();

                if (threatCompoment != null)
                {
                    if (threatCompoment.GetHeldObject() != null && threatCompoment.GetHeldObject() == instance.hive && !threatCompoment.IsThreatDead())
                    {
                        Script.Logger.Log(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(enemyAI.Key)} |threatComp|");
                        return enemyAI.Key;
                    }
                }

                if (instance.hive.parentObject != null && instance.hive.parentObject.gameObject.GetComponentInParent<EnemyAI>() == enemyAI.Key)
                {
                    Script.Logger.Log(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(enemyAI.Key)} |GetComponentInParent|");
                    return enemyAI.Key;
                }

                if (instance.IsHivePlacedAndInLOS())
                {
                    if (enemyAI.Key.isEnemyDead)
                    {
                        continue;
                    }
                    if (returnEnemy == null)
                    {
                        returnEnemy = enemyAI.Key;
                        continue;
                    }
                    if (Vector3.Distance(enemyAI.Key.transform.position, instance.hive.transform.position) < Vector3.Distance(returnEnemy.transform.position, instance.hive.transform.position))
                    {
                        returnEnemy = enemyAI.Key;
                    }
                }
            }
            if (!returnEnemy) returnEnemy = enemyDictionary.Keys.First();
            Script.Logger.Log(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(returnEnemy)} |returnEnemy|");
            return returnEnemy;
        }
    }
}
