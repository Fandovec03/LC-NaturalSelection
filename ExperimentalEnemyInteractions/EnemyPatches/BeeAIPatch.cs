using HarmonyLib;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class BeeValues
    {
        public bool start = Script.BoundingConfig.delayScriptsOnSpawn.Value;
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public Vector3 lastKnownEnemyPosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public float timeSinceHittingEnemy = 0f;
        public float LostLOSOfEnemy = 0f;
        public List<Type> enemyTypes = new List<Type>();
        public float delayTimer = 0.2f;
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static bool logBees = Script.BoundingConfig.debugRedBees.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static float UpdateTimer = Script.BoundingConfig.delay.Value;
        static List<string> blacklist = Script.BoundingConfig.beeBlacklist.Value.ToUpper().Split(",").ToList(); 

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            if (!beeList.ContainsKey(__instance))
            {
                beeList.Add(__instance, new BeeValues());
            }
            BeeValues beeData = beeList[__instance];

            if (beeData.enemyTypes.Count < 1)
            {
                beeData.enemyTypes.Add(typeof(DocileLocustBeesAI));
                beeData.enemyTypes.Add(typeof(SandWormAI));
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];
            if (beeData.delayTimer > 0f)
            {
                beeData.delayTimer -= Time.deltaTime;
            }
            else
            {
                if (RoundManagerPatch.RequestUpdate(__instance) == true)
                {
                    RoundManagerPatch.ScheduleGlobalListUpdate(__instance, EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance), beeList[__instance].enemyTypes, blacklist, __instance, true, Script.BoundingConfig.IgnoreImmortalEnemies.Value));
                }
                if (__instance.IsOwner && NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()].Contains(__instance))
                {
                    if (logBees && debugSpam) Script.Logger.LogError(EnemyAIPatch.DebugStringHead(__instance) + " FOUND ITSELF IN THE EnemyList! Removing...");
                    NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()].Remove(__instance);
                }
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefixPatch(RedLocustBees __instance)
        {
        BeeValues beeData = beeList[__instance];
        if (true)
        {
            if (beeData.targetEnemy != null && __instance.movingTowardsTargetPlayer == false && __instance.currentBehaviourStateIndex != 0)
            {
                if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "DoAIInterval: Prefix triggered false");

                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();
                return false;
            }
        }
        if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "DoAIInterval: Prefix triggered true");
        return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
        {
            if (true)
            {
                BeeValues beeData = beeList[__instance];
                switch (__instance.currentBehaviourStateIndex)
                {
                case 0:
                    {
                        EnemyAI? LOSenemy = null;
                        if (EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1).Count > 0)
                        {
                            if (NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()].Contains(__instance))
                            {
                                if (logBees && debugSpam) Script.Logger.LogError(EnemyAIPatch.DebugStringHead(__instance) + " FOUND ITSELF IN THE EnemyList before LOSEnemy! Removing...");
                                NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()].Remove(__instance);
                            }
                            LOSenemy = EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1).Keys.First();
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case0: Checked LOS for enemies. Enemy found: " + EnemyAIPatch.DebugStringHead(LOSenemy));

                            if (logBees && debugSpam)
                            {
                                foreach (KeyValuePair<EnemyAI, float> keyPair in EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1))
                                {
                                    if (logBees && debugSpam) Script.Logger.LogError(EnemyAIPatch.DebugStringHead(__instance) + " FOUND ITSELF IN THE EnemyList before LOSEnemy! Removing...");
                                    NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()].Remove(__instance);
                                }
                                LOSenemy = EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1).Keys.First();
                                if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case0: Checked LOS for enemies. Enemy found: " + EnemyAIPatch.DebugStringHead(LOSenemy));

                                if (logBees && debugSpam)
                                {
                                    foreach (KeyValuePair<EnemyAI, float> keyPair in EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1))
                                    {
                                        Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " Checking the LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);
                                        if (keyPair.Key == __instance) Script.Logger.LogError(EnemyAIPatch.DebugStringHead(__instance) + " FOUND ITSELF IN THE LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);
                                    }
                                }
                            }
                        }
                        if (__instance.wasInChase)
                        {
                            __instance.wasInChase = false;
                        }
                        if (Vector3.Distance(__instance.transform.position, __instance.lastKnownHivePosition) > 2f)
                        {
                            __instance.SetDestinationToPosition(__instance.lastKnownHivePosition, true);
                        }
                        if (__instance.IsHiveMissing())
                        {
                            __instance.SwitchToBehaviourState(2);
                            beeData.customBehaviorStateIndex = 2;
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case0: HIVE IS MISSING! CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                            break;
                        }
                        if (LOSenemy != null && Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance /*&& Vector3.Distance(__instance.targetPlayer.transform.position, __instance.hive.transform.position) < Vector3.Distance(LOSenemy.transform.position, __instance.hive.transform.position)*/)
                        {
                            __instance.SetDestinationToPosition(LOSenemy.transform.position, true);
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case0: Moving towards " + LOSenemy);

                            beeData.customBehaviorStateIndex = 1;
                            __instance.SwitchToBehaviourState(1);
                            __instance.syncedLastKnownHivePosition = false;
                            __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                            beeData.LostLOSOfEnemy = 0f;
                            if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case0: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        break;
                    }
                case 1:
                    {
                        if (__instance.targetPlayer != null && __instance.movingTowardsTargetPlayer) return;
                        if (beeData.targetEnemy == null || beeData.targetEnemy.isEnemyDead || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                        {
                            bool flag = false;
                            Dictionary<EnemyAI, float> priorityEnemies = EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1f);
                            EnemyAI? closestToHive = null;

                            if (priorityEnemies.Count > 0)
                            {
                                closestToHive = priorityEnemies.Keys.First();
                            }

                            if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: " + closestToHive + " is closest to hive.");

                            if (closestToHive != null && beeData.targetEnemy != closestToHive)
                            {
                                flag = true;
                                __instance.wasInChase = false;
                                beeData.targetEnemy = closestToHive;
                                __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                                __instance.StopSearch(__instance.searchForHive);
                                __instance.syncedLastKnownHivePosition = false;
                                beeData.LostLOSOfEnemy = 0f;
                                __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                                if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case2: Targeting " + closestToHive + ". Synced hive position");
                                break;
                            }
                            if (beeData.targetEnemy != null)
                            {
                                __instance.agent.acceleration = 16f;
                                if (!flag && EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 2f).Count == 0)
                                    beeData.targetEnemy = null;
                                __instance.wasInChase = false;
                                if (__instance.IsHiveMissing())
                                {
                                    beeData.customBehaviorStateIndex = 2;
                                    __instance.SwitchToBehaviourState(2);
                                    if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case1: HIVE IS MISSING! CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                                }
                                else
                                {
                                    beeData.customBehaviorStateIndex = 0;
                                    __instance.SwitchToBehaviourState(0);
                                    if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                                }
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (__instance.targetPlayer != null || __instance.movingTowardsTargetPlayer)
                        {
                            if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: target player found or moving towards target player");
                            return;
                        }

                        if (__instance.IsHivePlacedAndInLOS())
                        {
                            if (__instance.wasInChase)
                            {
                                __instance.wasInChase = false;
                            }
                            __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;

                            if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: IsHivePlacedAndInLOS triggered");
                            EnemyAI? enemyAI2 = null;
                            Collider[] collisionArray = Physics.OverlapSphere(__instance.hive.transform.position, __instance.defenseDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Collide);

                            if (collisionArray != null && collisionArray.Length > 0)
                            {
                                for (int i = 0; i < collisionArray.Length; i++)
                                {
                                    if (collisionArray[i].gameObject.tag == "Enemy")
                                    {
                                        if (collisionArray[i].gameObject.GetComponent<EnemyAICollisionDetect>().mainScript == __instance) continue;
                                        enemyAI2 = collisionArray[i].gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;
                                        if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case2: CollisionArray triggered. Enemy found: " + EnemyAIPatch.DebugStringHead(enemyAI2));
                                        break;
                                    }
                                }
                            }
                            if (enemyAI2 != null && Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                            {
                                __instance.SetDestinationToPosition(enemyAI2.transform.position, true);
                                if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case2: Moving towards: " + enemyAI2);
                                beeData.customBehaviorStateIndex = 1;
                                __instance.SwitchToBehaviourState(1);
                                __instance.syncedLastKnownHivePosition = false;
                                __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                                beeData.LostLOSOfEnemy = 0f;
                                if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                            }
                            else
                            {
                                beeData.customBehaviorStateIndex = 0;
                                __instance.SwitchToBehaviourState(0);
                                if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                            }
                            break;
                        }

                        bool flag = false;
                        Dictionary<EnemyAI, float> priorityEnemies = EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 1f);
                        EnemyAI? closestToHive = null;

                        if (priorityEnemies.Count > 0)
                        {
                            closestToHive = priorityEnemies.Keys.First();
                        }

                        if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: " + closestToHive + " is closest to hive.");

                        if (closestToHive != null && beeData.targetEnemy != closestToHive)
                        {
                            flag = true;
                            __instance.wasInChase = false;
                            beeData.targetEnemy = closestToHive;
                            __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                            __instance.StopSearch(__instance.searchForHive);
                            __instance.syncedLastKnownHivePosition = false;
                            beeData.LostLOSOfEnemy = 0f;
                            __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case2: Targeting " + closestToHive + ". Synced hive position");
                            break;
                        }
                        if (beeData.targetEnemy != null)
                        {
                            __instance.agent.acceleration = 16f;
                            if (!flag && EnemyAIPatch.GetEnemiesInLOS(__instance, NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], 360f, 16, 2f).Count == 0)
                            {
                                if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: lost LOS of " + beeData.targetEnemy + ", started timer.");
                                beeData.LostLOSOfEnemy += __instance.AIIntervalTime;
                                if (beeData.LostLOSOfEnemy >= 4.5f)
                                {
                                    beeData.targetEnemy = null;
                                    beeData.LostLOSOfEnemy = 0f;
                                    if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: lost LOS of " + beeData.targetEnemy + ", Stopped and reset timer.");
                                }
                            }
                            else
                            {
                                __instance.wasInChase = true;
                                beeData.lastKnownEnemyPosition = beeData.targetEnemy.transform.position;
                                __instance.SetDestinationToPosition(beeData.lastKnownEnemyPosition, true);
                                beeData.LostLOSOfEnemy = 0f;
                                if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: lost " + beeData.targetEnemy); ;

                            }
                            break;
                        }
                        __instance.agent.acceleration = 13f;
                        if (!__instance.searchForHive.inProgress)
                        {
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case2: set new search for hive");
                            if (__instance.wasInChase)
                            {
                                __instance.StartSearch(beeData.lastKnownEnemyPosition, __instance.searchForHive);
                                if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: Started search for hive.");
                            }
                            else
                            {
                                __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                                if (logBees) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "case2: Started search for hive.");
                            }
                        }
                        break;
                    }
                }
            }
        }

        static LNetworkVariable<float> NSSetOnFireChance(RedLocustBees instance)
        {
            return LNetworkVariable<float>.Connect("NSSetOnFireChance" + instance.NetworkObjectId);
        }
        static LNetworkVariable<float> NSSetOnFireMaxChance(RedLocustBees instance)
        {
            return LNetworkVariable<float>.Connect("NSSetOnFireMaxChance" + instance.NetworkObjectId);
        }

        static LNetworkMessage<bool> NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            return LNetworkMessage<bool>.Connect("NSSetGiantOnFire" + forestGiantAI.NetworkObjectId);
        }

        public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
        {
            if (beeList.ContainsKey(__instance))
            {
                if (beeList[__instance].timeSinceHittingEnemy > 1.7f && __instance.currentBehaviourStateIndex > 0 || beeList[__instance].timeSinceHittingEnemy > 1.3f && __instance.currentBehaviourStateIndex == 2 && !mainscript2.isEnemyDead)
                {
                    mainscript2.HitEnemy(1, null, playHitSFX: true);
                    beeList[__instance].timeSinceHittingEnemy = 0f;

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
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "OnCustomEnemyCollision: Giant hit. Chance to set on fire: " + NSSetOnFireMaxChance(__instance).Value);
                        }
                        else
                        {
                            if (logBees) Script.Logger.LogMessage("Client not elligible to determine chance to set giant on fire");
                        }
                        if (NSSetOnFireChance(__instance).Value <= NSSetOnFireMaxChance(__instance).Value && __instance.IsOwner)
                        {
                            if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "OnCustomEnemyCollision: SET GIANT ON FIRE! Random number: " + NSSetOnFireMaxChance(__instance).Value);
                            ForestGiantAI giant = (ForestGiantAI)mainscript2;

                            /*if (giant.IsOwner)
                            {
                            giant.timeAtStartOfBurning = Time.realtimeSinceStartup;
                            giant.SwitchToBehaviourState(2);
                            Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + " /BeesCustomhit/ " + "isOwner: " + __instance.IsOwner + ", Giant isOwner: " + giant.IsOwner + ", set fire to false");
                            }*/
                            NetworkSetGiantOnFire(giant).SendServer(true);
                        }
                    }
                }
                else
                {
                    beeList[__instance].timeSinceHittingEnemy += Time.deltaTime;
                }
            }
        }
    }
}
