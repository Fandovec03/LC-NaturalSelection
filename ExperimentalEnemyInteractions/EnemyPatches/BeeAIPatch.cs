using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
//using LethalNetworkAPI;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace NaturalSelection.EnemyPatches;

class BeeValues : EnemyDataBase
{
    internal Vector3 lastKnownEnemyPosition = Vector3.zero;
    //internal int customBehaviorStateIndex = 0;
    internal Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
    internal float LostLOSOfEnemy = 0f;
    internal float delayTimer = 0.2f;
    internal PlayerControllerB? priorityPlayerTarget = null;
    internal PlayerControllerB? closestPlayer = null;
    internal EnemyAI? priorityEnemyTarget = null;
    internal Dictionary<EnemyAI, float> enemiesInLOS = new Dictionary<EnemyAI, float>();

    //internal bool movingTowardsTargetEnemy = false;
}

[HarmonyPatch(typeof(RedLocustBees))]
class BeeAIPatch
{
    static Dictionary<RedLocustBees, BeeValues> beeList = [];
    static bool logBees = Script.Bools["debugRedBees"];
    static bool debugSpam = Script.Bools["spammyLogs"];
    static bool debugTriggers = Script.Bools["debugTriggerFlags"];
    static List<string> beeBlacklist = InitializeGamePatch.beeBlacklist;

    internal static void ReadIL(IEnumerable<CodeInstruction> instructionsImport)
    {
        List<CodeInstruction> instructions = instructionsImport.ToList();

        for (int i = 0; i < instructions.ToList().Count; i++)
        {
            try
            {
                Script.Logger.LogInfo($"{i}|| " + instructions[i]);
            }
            catch
            {
                Script.Logger.LogError("Failed to read instructions");
            }
        }
        Script.Logger.LogWarning("Finished reading IL");
    }


    static void Event_OnConfigSettingChanged(string entryKey, bool value)
    {
        if (entryKey == "debugRedBees") logBees = value;
        if (entryKey == "spammyLogs") debugSpam = value;
        if (entryKey == "debugTriggerFlags") debugTriggers = value;
        //Script.LogNS(LogLevel.Message,$"Curcuit received event. logBees = {logBees}, debugSpam = {debugSpam}, debugTriggers = {debugTriggers}");
    }

    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    static void StartPatch(RedLocustBees __instance)
    {
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());

        EnemyBehaviourState dummyState = new EnemyBehaviourState();
        dummyState.name = "NaturalSelectionDummyState";
        EnemyBehaviourState[] behaviorStates = new EnemyBehaviourState[5];
        __instance.enemyBehaviourStates.CopyTo(behaviorStates, 0);
        behaviorStates[3] = dummyState;
        behaviorStates[4] = dummyState;
        __instance.enemyBehaviourStates = behaviorStates;


        Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    static void UpdatePatch(RedLocustBees __instance)
    {
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());
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
        float num = Time.deltaTime * 0.7f;
        switch(__instance.currentBehaviourStateIndex)
        {
            case 3:
            {
                if (__instance.previousBehaviourStateIndex != __instance.currentBehaviourStateIndex)
                {
                    __instance.previousState = __instance.currentBehaviourStateIndex;
                    __instance.ResetBeeZapTimer();
                    __instance.SetBeeParticleMode(1);
                    if (!__instance.overrideBeeParticleTarget)
                    {
                            __instance.beeParticlesTarget.position = __instance.transform.position + Vector3.up * 1.5f;
                    }
                }
                if (__instance.attackZapModeTimer > 3f)
                {
                    __instance.beesZappingMode = 1;
                    __instance.ResetBeeZapTimer();
                }
                __instance.agent.speed = 6f;
                __instance.agent.acceleration = 13f;
                __instance.beesIdle.volume = Mathf.Max(__instance.beesIdle.volume - num, 0f);
                if (__instance.beesIdle.isPlaying && __instance.beesIdle.volume <= 0f)
                {
                    __instance.beesIdle.Stop();
                }
                __instance.beesDefensive.volume = Mathf.Min(__instance.beesDefensive.volume + num, 1f);
                if (!__instance.beesDefensive.isPlaying)
                {
                    __instance.beesDefensive.Play();
                }
                __instance.beesAngry.volume = Mathf.Max(__instance.beesAngry.volume - num, 0f);
                if (__instance.beesAngry.isPlaying && __instance.beesAngry.volume <= 0f)
                {
                    __instance.beesAngry.Stop();
                }
                break;
            }
            case 4:
            {
                if (__instance.previousBehaviourStateIndex != __instance.currentBehaviourStateIndex)
                {
                    __instance.previousState = __instance.currentBehaviourStateIndex;
                    __instance.ResetBeeZapTimer();
                    __instance.SetBeeParticleMode(2);
                    if (!__instance.overrideBeeParticleTarget)
                    {
                        __instance.beeParticlesTarget.position = __instance.transform.position + Vector3.up * 1.5f;
                    }
                }
                __instance.beesZappingMode = 2;
                __instance.agent.speed = 10.3f;
                __instance.agent.acceleration = 13f;
                __instance.beesIdle.volume = Mathf.Max(__instance.beesIdle.volume - num, 0f);
                if (__instance.beesIdle.isPlaying && __instance.beesIdle.volume <= 0f)
                {
                    __instance.beesIdle.Stop();
                }
                __instance.beesDefensive.volume = Mathf.Min(__instance.beesDefensive.volume - num, 0f);
                if (__instance.beesDefensive.isPlaying && __instance.beesDefensive.volume <= 0f)
                {
                    __instance.beesDefensive.Stop();
                }
                __instance.beesAngry.volume = Mathf.Max(__instance.beesAngry.volume + num, 1f);
                if (!__instance.beesAngry.isPlaying)
                {
                    __instance.beesAngry.Play();
                }
                break;
            }
        }
        if (__instance.currentBehaviourStateIndex > 2)
        {
            __instance.BeesZapOnTimer();
            if ((__instance.stunNormalizedTimer > 0f || __instance.overrideBeeParticleTarget))
            {
                __instance.SetBeeParticleMode(2);
                __instance.agent.speed = 0f;
            }
        }
    }

    /*[HarmonyPatch("ChaseWithPriorities")]
    [HarmonyPostfix]
    static void ChaseWithPrioritiesPostfix(RedLocustBees __instance, PlayerControllerB __result)
    {
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());
        beeData.priorityPlayerTarget = __result;
    }*/
    
    /*[HarmonyPatch("DoAIInterval")]
    [HarmonyPrefix]
    static void DoAIIntervalPrefixPatch(RedLocustBees __instance)
    {
        if (__instance.isEnemyDead) return;
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());
        Type type = __instance.GetType();
        List<EnemyAI> tempList = LibraryCalls.GetEnemyList(type);
        LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);

        beeData.enemiesInLOS = new Dictionary<EnemyAI, float>(LibraryCalls.GetEnemiesInLOS(__instance, ref tempList, 360f, 16, 1));

        if (beeData.enemiesInLOS.Count > 0)
        {
            beeData.targetEnemy = beeData.enemiesInLOS.Keys.First();
            Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case0: Checked LOS for enemies. Enemy found: {LibraryCalls.DebugStringHead(beeData.targetEnemy)}", __instance, logBees);
        }
        //if (__instance.movingTowardsTargetPlayer) beeData.movingTowardsTargetEnemy = false;
    }*/

    /*[HarmonyPatch("DoAIInterval")]
    [HarmonyPostfix]
    static void DoAIIntervalPostfixPatch2(RedLocustBees __instance)
    {
        if (__instance.isEnemyDead) return;
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());

        //if (!__instance.movingTowardsTargetPlayer && beeData.targetEnemy != null) __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
    }*/
        
    [HarmonyPatch("DoAIInterval")]
    [HarmonyPrefix]

    static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
    {
        //if (__instance.isEnemyDead) return;
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());
        Type type = __instance.GetType();
        List <EnemyAI> tempList = LibraryCalls.GetEnemyList(type);
        LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);
        beeData.enemiesInLOS  = new Dictionary<EnemyAI, float>(LibraryCalls.GetEnemiesInLOS(__instance,ref tempList, 360f, 16, 1, importEyePosition: __instance.eye.position + (Vector3.up * 0.5f)));
        if (beeData.enemiesInLOS.Count > 0)
        {
            beeData.targetEnemy = beeData.enemiesInLOS.Keys.First();
            Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case0: Checked LOS for enemies. Enemy found: {LibraryCalls.DebugStringHead(beeData.targetEnemy)}", __instance, logBees);
        }
        else
        {
            beeData.targetEnemy = null;
        }
        beeData.closestPlayer = __instance.CheckLineOfSightForPlayer(360, 16, 1);


        switch (__instance.currentBehaviourStateIndex)
        {
            case 0:
            {
                if (beeData.targetEnemy != null && Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) < __instance.defenseDistance)
                {
                    if (beeData.closestPlayer == null || Vector3.Distance(__instance.transform.position, beeData.targetEnemy.transform.position) < Vector3.Distance(__instance.transform.position, beeData.closestPlayer.transform.position))
                    {
                        __instance.targetPlayer = null;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                        __instance.SwitchToBehaviourState(3);
                    }
                    beeData.LostLOSOfEnemy = 0f;
                    Script.LogNS(LogLevel.Debug, $"{LibraryCalls.DebugStringHead(__instance)} case0: CustomBehaviorStateIndex changed: {0}", __instance, logBees);
                }
                break;
            }
            case 1:
            {
                if (beeData.targetEnemy != null && Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) < __instance.defenseDistance)
                {
                    if (beeData.closestPlayer == null || Vector3.Distance(__instance.transform.position, beeData.targetEnemy.transform.position) < Vector3.Distance(__instance.transform.position, beeData.closestPlayer.transform.position))
                    {
                        __instance.targetPlayer = null;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                        __instance.SwitchToBehaviourState(3);
                    }
                }
                break;
            }
            case 2:
            {
                beeData.priorityPlayerTarget = __instance.ChaseWithPriorities();

                if (beeData.targetEnemy != null && (beeData.priorityPlayerTarget == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) < Vector3.Distance(beeData.priorityPlayerTarget.transform.position, __instance.hive.transform.position)))
                {
                        __instance.targetPlayer = null;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.SwitchToBehaviourState(4);
                }
                break;
            }
            case 3:
            {
                if (beeData.targetEnemy == null || beeData.targetEnemy.isEnemyDead || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > __instance.defenseDistance + 5f)
                {
                    beeData.targetEnemy = null;
                    if (beeData.closestPlayer != null && __instance.PlayerIsTargetable(beeData.closestPlayer) && beeData.closestPlayer.isUnderwater && Vector3.Distance(beeData.closestPlayer.transform.position, __instance.hive.transform.position) < __instance.defenseDistance + 5f)
                    {
                        __instance.SetMovingTowardsTargetPlayer(beeData.closestPlayer);
                        __instance.SwitchToBehaviourState(1);
                        __instance.SwitchOwnershipOfBeesToClient(beeData.closestPlayer);
                        break;
                    }
                    __instance.wasInChase = false;
                    if (__instance.IsHiveMissing())
                    {
                        beeData.targetEnemy = null;
                        __instance.SwitchToBehaviourState(2);
                        Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case1: HIVE IS MISSING! CustomBehaviorStateIndex changed: {0}", __instance, logBees);
                    }
                    else
                    {
                        beeData.targetEnemy = null;
                        __instance.SwitchToBehaviourState(0);
                        Script.LogNS(LogLevel.Debug, $"{LibraryCalls.DebugStringHead(__instance)} case1: CustomBehaviorStateIndex changed: {0}", __instance, logBees);
                    }
                    break;
                }
                else if (__instance.hive.isHeldByEnemy)
                {
                    __instance.targetPlayer = null;
                    __instance.movingTowardsTargetPlayer = false;
                    __instance.SwitchToBehaviourState(4);
                    break;
                }
                else if (beeData.closestPlayer != null && beeData.closestPlayer.currentlyHeldObjectServer == __instance.hive)
                {
                    beeData.targetEnemy = null;
                    __instance.SetMovingTowardsTargetPlayer(beeData.closestPlayer);
                    __instance.SwitchToBehaviourState(2);
                    __instance.SwitchOwnershipOfBeesToClient(beeData.closestPlayer);
                    //beeData.movingTowardsTargetEnemy = false;
                    break;
                }
                __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                break;
            }
            case 4:
            {
                if (__instance.IsHivePlacedAndInLOS())
                {
                    if (__instance.wasInChase)
                    {
                        __instance.wasInChase = false;
                    }
                    __instance.lastKnownHivePosition = __instance.hive.transform.position + Vector3.up * 0.5f;
                    Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: IsHivePlacedAndInLOS triggered", __instance, logBees);
                    EnemyAI? enemyAI2 = null;
                    PlayerControllerB? playerControllerB2 = null;
                    Collider[] collisionArray = Physics.OverlapSphere(__instance.hive.transform.position, __instance.defenseDistance, StartOfRound.Instance.playersMask + LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

                    if (collisionArray != null && collisionArray.Length > 0)
                    {
                        for (int i = 0; i < collisionArray.Length; i++)
                        {
                            EnemyAICollisionDetect enemyCollisionDettect = collisionArray[i].gameObject.GetComponent<EnemyAICollisionDetect>();
                            if (playerControllerB2 == null) playerControllerB2 = collisionArray[i].gameObject.GetComponent<PlayerControllerB>();
                            if (enemyCollisionDettect != null)
                            {
                                EnemyAI enemy = enemyCollisionDettect.mainScript;
                                if (enemy != null && enemyAI2 == null)
                                {
                                    enemyAI2 = enemy;
                                    Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case2: CollisionArray triggered. Enemy found: {LibraryCalls.DebugStringHead(enemyAI2)}", __instance, logBees);
                                }
                            }
                            if (enemyAI2 != null && playerControllerB2 != null) break;
                        }
                    }
                    if (enemyAI2 != null && Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance &&
                            (playerControllerB2 == null || Vector3.Distance(enemyAI2.transform.position, __instance.hive.transform.position) < Vector3.Distance(playerControllerB2.transform.position, __instance.hive.transform.position)))
                    {
                        __instance.targetPlayer = null;
                        __instance.SetDestinationToPosition(enemyAI2.transform.position, true);
                        Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case2: Moving towards: {enemyAI2}", __instance,logBees);
                        //beeData.customBehaviorStateIndex = 1;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.SwitchToBehaviourState(3);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                        beeData.LostLOSOfEnemy = 0f;
                        Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: CustomBehaviorStateIndex changed: {0}", __instance,logBees);
                    }
                    else
                    {
                        beeData.targetEnemy = null;
                        if (playerControllerB2 == null) __instance.SwitchToBehaviourState(0);
                        else
                        {
                                __instance.SwitchToBehaviourState(2);
                        }
                        Script.LogNS(LogLevel.Debug, $"{LibraryCalls.DebugStringHead(__instance)} case2: CustomBehaviorStateIndex changed: {0}", __instance, logBees);
                    }//
                    break;
                }

                bool flag = false;
                beeData.priorityEnemyTarget = ChaseEnemyWithPriorities(ref beeData.enemiesInLOS, __instance);
                beeData.priorityPlayerTarget = __instance.ChaseWithPriorities();
                Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: {beeData.priorityEnemyTarget} is closest to hive.", __instance,logBees);

                if (beeData.priorityEnemyTarget != null && beeData.targetEnemy != beeData.priorityEnemyTarget && (beeData.priorityPlayerTarget == null || Vector3.Distance(beeData.priorityEnemyTarget.transform.position, __instance.hive.transform.position) < Vector3.Distance(beeData.priorityPlayerTarget.transform.position, __instance.hive.transform.position)))
                {
                    flag = true;
                    __instance.wasInChase = false;
                    beeData.targetEnemy = beeData.priorityEnemyTarget;
                    __instance.movingTowardsTargetPlayer = false;
                    __instance.SetDestinationToPosition(beeData.targetEnemy.transform.position, true);
                    __instance.StopSearch(__instance.searchForHive);
                    __instance.syncedLastKnownHivePosition = false;
                    beeData.LostLOSOfEnemy = 0f;
                    __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                    Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} case2: Targeting {beeData.priorityEnemyTarget}. Synced hive position", __instance,logBees);
                }
                if (beeData.targetEnemy != null && (beeData.priorityPlayerTarget == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) < Vector3.Distance(beeData.priorityPlayerTarget.transform.position, __instance.hive.transform.position)))
                {
                    __instance.agent.acceleration = 16f;
                    if (!flag && beeData.enemiesInLOS.Count == 0)
                    {
                        Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost LOS of {beeData.targetEnemy}, started timer.", __instance,logBees && debugSpam);
                        beeData.LostLOSOfEnemy += __instance.AIIntervalTime;
                        if (beeData.LostLOSOfEnemy >= 4.5f)
                        {
                            beeData.targetEnemy = null;
                            beeData.priorityEnemyTarget = null;
                            beeData.LostLOSOfEnemy = 0f;
                            Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost LOS of {beeData.targetEnemy}, Stopped and reset timer.", __instance,logBees);
                        }
                    }
                    else
                    {
                        __instance.wasInChase = true;
                        beeData.lastKnownEnemyPosition = beeData.targetEnemy.transform.position;
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.SetDestinationToPosition(beeData.lastKnownEnemyPosition, true);
                        beeData.LostLOSOfEnemy = 0f;
                        Script.LogNS(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} case2: lost {beeData.targetEnemy}", __instance,logBees);

                    }
                    break;
                }
                if (beeData.priorityPlayerTarget == null)
                {
                    __instance.agent.acceleration = 13f;
                    if (!__instance.searchForHive.inProgress)
                    {
                        Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(__instance)} case2: set new search for hive", __instance, logBees);
                        if (__instance.wasInChase)
                        {
                            __instance.StartSearch(beeData.lastKnownEnemyPosition, __instance.searchForHive);
                            Script.LogNS(LogLevel.Debug, $"{LibraryCalls.DebugStringHead(__instance)} case2: Started search for hive.", __instance, logBees);
                        }
                        else
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                            Script.LogNS(LogLevel.Debug, $"{LibraryCalls.DebugStringHead(__instance)} case2: Started search for hive.", __instance, logBees);
                        }
                    }
                }
                else
                {
                    beeData.targetEnemy = null;
                    __instance.SwitchToBehaviourState(2);
                }
                break;
            }
        }
    }
        
    /*static LNetworkVariable<float> NSSetOnFireChance(RedLocustBees instance)
    {
        string NWID = "NSSetOnFireChance" + instance.NetworkObjectId;
        return Generics.Networking.NSEnemyNetworkVariable<float>(NWID);
    }
    static LNetworkVariable<float> NSSetOnFireMaxChance(RedLocustBees instance)
    {
        string NWID = "NSSetOnFireMaxChance" + instance.NetworkObjectId;
        return Generics.Networking.NSEnemyNetworkVariable<float>(NWID);
    }

    static LNetworkEvent NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
    {
        string NWID = "NSSetGiantOnFire" + forestGiantAI.NetworkObjectId;
        return Generics.Networking.NSEnemyNetworkEvent(NWID);
    }
    */
    public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
    {
        if (mainscript2.GetType() == typeof(RedLocustBees)) return;
        BeeValues beeData = (BeeValues)Utilities.GetEnemyData(__instance, new BeeValues());
        if (/*Utilities.enemyDataDict.ContainsKey(beeData.enemyID) &&*/ !beeBlacklist.Contains(mainscript2.enemyType.enemyName))
        {
            if ((!beeData.hitRegistry.ContainsKey(mainscript2) || beeData.hitRegistry[mainscript2] > 1.7f) && __instance.currentBehaviourStateIndex > 0 && !mainscript2.isEnemyDead || (!beeData.hitRegistry.ContainsKey(mainscript2) || beeData.hitRegistry[mainscript2] > 1.2f) && __instance.currentBehaviourStateIndex == 2 && !mainscript2.isEnemyDead)
            {
                mainscript2.HitEnemy(1, null, playHitSFX: true);

                if (!beeData.hitRegistry.ContainsKey(mainscript2)) beeData.hitRegistry.Add(mainscript2, 0);
                else beeData.hitRegistry[mainscript2] = 0;

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
                        Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} OnCustomEnemyCollision: Giant hit. Chance to set on fire: {NSSetOnFireMaxChance(__instance).Value} , rolled {NSSetOnFireChance(__instance)}", __instance);;
                    }
                    else
                    {
                        Script.LogNS(LogLevel.Message,"Client not elligible to determine chance to set giant on fire", __instance,logBees);
                    }
                    if (NSSetOnFireChance(__instance).Value <= NSSetOnFireMaxChance(__instance).Value && __instance.IsOwner)
                    {
                        Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} OnCustomEnemyCollision: SET GIANT ON FIRE! Random number: {NSSetOnFireChance(__instance).Value}", __instance);
                        ForestGiantAI giant = (ForestGiantAI)mainscript2;

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
            if (enemyAI.Key.isEnemyDead)
            {
                continue;
            }
            threatCompoment = enemyAI.Key.GetComponent<IVisibleThreat>();

            if (threatCompoment != null)
            {
                if (threatCompoment.GetHeldObject() != null && threatCompoment.GetHeldObject() == instance.hive && !threatCompoment.IsThreatDead())
                {
                    if (debugTriggers) Script.LogNS(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(enemyAI.Key)} |threatComp|", instance);
                    return enemyAI.Key;
                }
            }

            if (instance.hive.parentObject != null && instance.hive.parentObject.gameObject.GetComponentInParent<EnemyAI>() == enemyAI.Key)
            {
                if (debugTriggers) Script.LogNS(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(enemyAI.Key)} |GetComponentInParent|", instance);
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
        if (debugTriggers) Script.LogNS(LogLevel.Info, $"ChaseEnemiesWithPriorities: Returning {LibraryCalls.DebugStringHead(returnEnemy)} |returnEnemy|", instance);
        return returnEnemy;
    }
}
