using HarmonyLib;
using LethalNetworkAPI;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class BeeValues
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public Vector3 lastKnownEnemyPosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
        public float LostLOSOfEnemy = 0f;
        public List<Type> enemyTypes = new List<Type>();
        public float delayTimer = 0.2f;
        public List<EnemyAI> localEnemyList = new List<EnemyAI>();
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static bool logBees = Script.BoundingConfig.debugRedBees.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
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
            if (RoundManagerPatch.RequestUpdate(__instance) == true)
            {
                List<EnemyAI> tempList = EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetCompleteList(__instance), beeList[__instance].enemyTypes, blacklist, __instance, true, Script.BoundingConfig.IgnoreImmortalEnemies.Value).ToList();
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, tempList);
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
        BeeValues beeData = beeList[__instance];
        
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
        if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + "DoAIInterval: Prefix triggered true");
        return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];
            Type type = __instance.GetType();

            beeData.localEnemyList = new List<EnemyAI>(EnemyAIPatch.GetInsideOrOutsideEnemyList(NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type], __instance));
            Dictionary<EnemyAI, float> enemiesInLOS = new Dictionary<EnemyAI, float>(EnemyAIPatch.GetEnemiesInLOS(__instance, beeData.localEnemyList, 360f, 16, 1));

            switch (__instance.currentBehaviourStateIndex)
            {
            case 0:
                {
                    EnemyAI? LOSenemy = null;
                    if (enemiesInLOS.Count > 0)
                    {
                        foreach (KeyValuePair<EnemyAI, float> keyPair in new Dictionary<EnemyAI, float>(enemiesInLOS))
                        {
                            if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " Checking the LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);

                            if (keyPair.Key == __instance)
                            {
                                if (logBees && debugSpam) Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(__instance) + " FOUND ITSELF IN THE LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);
                                enemiesInLOS.Remove(keyPair.Key);
                            }
                            if (keyPair.Key.GetType() == __instance.GetType())
                            {
                                if (logBees && debugSpam) Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(__instance) + " Found its type in THE LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);
                                enemiesInLOS.Remove(keyPair.Key);
                            }
                                if (keyPair.Key.isEnemyDead)
                            {

                                if (logBees && debugSpam) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(__instance) + " Found dead enemy in LOSList: " + EnemyAIPatch.DebugStringHead(keyPair.Key) + ", Distance: " + keyPair.Value);
                                enemiesInLOS.Remove(keyPair.Key);
                            }
                        }
                        if (enemiesInLOS.Count > 0) LOSenemy = enemiesInLOS.Keys.First();
                        if (logBees) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "case0: Checked LOS for enemies. Enemy found: " + EnemyAIPatch.DebugStringHead(LOSenemy));
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
                        EnemyAI? closestToHive = null;

                        if (enemiesInLOS.Count > 0)
                        {
                            closestToHive = enemiesInLOS.Keys.First();
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
                            if (!flag && enemiesInLOS.Count == 0)
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
                    EnemyAI? closestToHive = null;

                    if (enemiesInLOS.Count > 0)
                    {
                        closestToHive = enemiesInLOS.Keys.First();
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
                        if (!flag && enemiesInLOS.Count == 0)
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

        static LNetworkVariable<float> NSSetOnFireChance(RedLocustBees instance)
        {
            string NWID = "NSSetOnFireChance" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariableFloat(NWID);
        }
        static LNetworkVariable<float> NSSetOnFireMaxChance(RedLocustBees instance)
        {
            string NWID = "NSSetOnFireMaxChance" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariableFloat(NWID);
        }

        static LNetworkEvent NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSSetGiantOnFire" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
        {
            if (beeList.ContainsKey(__instance))
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
                            Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "OnCustomEnemyCollision: Giant hit. Chance to set on fire: " + NSSetOnFireMaxChance(__instance).Value + ", rolled " + NSSetOnFireChance(__instance));;
                        }
                        else
                        {
                            if (logBees) Script.Logger.LogMessage("Client not elligible to determine chance to set giant on fire");
                        }
                        if (NSSetOnFireChance(__instance).Value <= NSSetOnFireMaxChance(__instance).Value && __instance.IsOwner)
                        {
                            Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + "OnCustomEnemyCollision: SET GIANT ON FIRE! Random number: " + NSSetOnFireChance(__instance).Value);
                            ForestGiantAI giant = (ForestGiantAI)mainscript2;

                            /*if (giant.IsOwner)
                            {
                            giant.timeAtStartOfBurning = Time.realtimeSinceStartup;
                            giant.SwitchToBehaviourState(2);
                            Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + " /BeesCustomhit/ " + "isOwner: " + __instance.IsOwner + ", Giant isOwner: " + giant.IsOwner + ", set fire to false");
                            }*/
                            NetworkSetGiantOnFire(giant).InvokeServer();
                        }
                    }
                }
            }
        }
    }
}
