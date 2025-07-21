using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace NaturalSelection.EnemyPatches
{
    class SandWormAIData : EnemyDataBase
    {
        internal float refreshCDtime = 0.5f;
        //public int targetingEntity = 0;
        internal float clearEnemiesTimer = 0f;
        internal bool movingTowardsTargetPlayer = false;
        internal bool MovingTowardsTargetEntity = false;
        internal int NetworkSandwormBehaviorState = 0;
        internal int CacheNetworkSandwormBehaviorState = 0;
        internal bool CacheMovingTowardsTargetEntity = false;
    }

    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch
    {
        static bool debugSandworm = Script.Bools["debugSandworms"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool triggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> sandwormBlacklist = InitializeGamePatch.sandwormBlacklist;
        static LNetworkVariable<int> NetworkSandwormBehaviorState(SandWormAI instance)
        {
            string NWID = "NSSandwormBehaviorState" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<int>(NWID);
        }

        static LNetworkVariable<bool> NetworkTargetingEntity(SandWormAI instance)
        {
            string NWID = "NSSandwormTargetingEntity" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<bool>(NWID);
        }

        static LNetworkVariable<bool> NetworkMovingTowardsPlayer(SandWormAI instance)
        {
            string NWID = "NSSandwormMovingTowardsPlayer" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<bool>(NWID);
        }

        ////////////////////////////////////////////

        //static Dictionary<SandWormAI, ExtendedSandWormAIData> sandworms = [];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugSandworms") debugSandworm = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") triggerFlag = value;
            //Script.LogNS(LogLevel.Message,$"Earth Leviathan received event. debugSandworm = {debugSandworm}, debugSpam = {debugSpam}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void SandWormStartPatch(SandWormAI __instance)
        {
            SandWormAIData data = (SandWormAIData)Utilities.GetEnemyData(__instance, new SandWormAIData());
            data.SetOwner(__instance);
            data.Subscribe();
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;

            NetworkMovingTowardsPlayer(__instance).OnValueChanged += ChangeMovingTowardsPlayer;
            NetworkTargetingEntity(__instance).OnValueChanged += ChangeMovingTowardsEntity;
            NetworkSandwormBehaviorState(__instance).OnValueChanged += ChangeNetworkBehaviorState;

            void ChangeMovingTowardsPlayer(bool oldValue, bool newValue)
            {
                if (oldValue != newValue) data.movingTowardsTargetPlayer = newValue;
            }

            void ChangeMovingTowardsEntity(bool oldValue, bool newValue)
            {
                if (oldValue != newValue) data.MovingTowardsTargetEntity = newValue;
            }

            void ChangeNetworkBehaviorState(int oldValue, int newValue)
            {
                if (oldValue != newValue) data.NetworkSandwormBehaviorState = newValue;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool SandWormPrefixUpdatePatch(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            SandWormAIData SandwormData = (SandWormAIData)Utilities.GetEnemyData(__instance, new SandWormAIData());
            Type type = __instance.GetType();

            if (__instance.IsOwner)
            {
                if (__instance.movingTowardsTargetPlayer != SandwormData.movingTowardsTargetPlayer)
                {
                    SandwormData.movingTowardsTargetPlayer = __instance.movingTowardsTargetPlayer;
                    NetworkMovingTowardsPlayer(__instance).Value = __instance.movingTowardsTargetPlayer;
                }

                if (SandwormData.NetworkSandwormBehaviorState != SandwormData.CacheNetworkSandwormBehaviorState)
                {
                    SandwormData.CacheNetworkSandwormBehaviorState = SandwormData.NetworkSandwormBehaviorState;
                    NetworkSandwormBehaviorState(__instance).Value = SandwormData.NetworkSandwormBehaviorState;
                }

                if (SandwormData.MovingTowardsTargetEntity != SandwormData.CacheMovingTowardsTargetEntity)
                {
                    SandwormData.CacheMovingTowardsTargetEntity = SandwormData.MovingTowardsTargetEntity;
                    NetworkTargetingEntity(__instance).Value = SandwormData.MovingTowardsTargetEntity;
                }
            }



            if (SandwormData.refreshCDtime <= 0)
            {
                if (RoundManagerPatch.RequestUpdate(__instance) == true)
                {
                    List<EnemyAI> tempList = LibraryCalls.GetCompleteList(__instance, true, 0);
                    Dictionary<EnemyAI, int> tempDict = new Dictionary<EnemyAI, int>();
                    LibraryCalls.FilterEnemyList(ref tempList, sandwormBlacklist, __instance, filterOutImmortal: false,filterTheSameType: true);
                    foreach (var enemy in tempList)
                    {
                        tempDict.Add(enemy, InitializeGamePatch.customSizeOverrideListDictionary[enemy.enemyType.enemyName]);
                    }
                    LibraryCalls.FilterEnemySizes(ref tempDict, [2, 3, 4, 5], __instance, false);
                    tempList = tempDict.Keys.ToList();
                    RoundManagerPatch.ScheduleGlobalListUpdate(__instance, ref tempList);
                    //NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(__instance.GetType(), LibraryCalls.FilterEnemyList(LibraryCalls.GetOutsideEnemyList(LibraryCalls.GetCompleteList(__instance, true, 0), __instance), targetedTypes, __instance));
                }
                if (__instance.IsOwner)
                {
                    List<EnemyAI> tempList = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                    LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);
                    SandwormData.closestEnemy = LibraryCalls.FindClosestEnemy(ref tempList, SandwormData.closestEnemy, __instance, usePathLenghtAsDistance: Script.usePathToFindClosestEnemy);
                    if (SandwormData.coroutineTimer < Time.realtimeSinceStartup) { __instance.StartCoroutine(LibraryCalls.FindClosestEnemyEnumerator(SandwormData.ChangeClosestEnemyAction, tempList, SandwormData.closestEnemy, __instance, usePathLenghtAsDistance: true)); SandwormData.coroutineTimer = Time.realtimeSinceStartup + 0.2f; }
                }
                SandwormData.refreshCDtime = 0.2f;
            }
            if (SandwormData.refreshCDtime > 0)
            {
                SandwormData.refreshCDtime -= Time.deltaTime;
            }

            
            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;

            switch (targetingEntity)
            {
                case 0:
                    if (__instance.inEmergingState || __instance.emerged)
                    {
                        return false;
                    }
                    break;
                case 1:
                {
                    if ((__instance.IsOwner && SandwormData.targetEnemy != null || !__instance.IsOwner && SandwormData.MovingTowardsTargetEntity) && !__instance.movingTowardsTargetPlayer)
                    {
                        if (__instance.IsOwner)
                        {
                            if (SandwormData.targetEnemy != null) SandwormData.MovingTowardsTargetEntity = true;
                            if (__instance.updateDestinationInterval >= 0f)
                            {
                                __instance.updateDestinationInterval -= Time.deltaTime;
                            }
                            else
                            {
                                Script.LogNS(LogLevel.Debug," calling DoAIInterval", __instance ,debugSandworm && debugSpam);
                                __instance.DoAIInterval();
                                __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                            }
                        }

                        //Script.LogNS(LogLevel.Message,$"Invoking originalUpdate");
                        try
                        {
                            ReversePatchAI.originalUpdate.Invoke(__instance);
                            //Script.LogNS(LogLevel.Message,"Succesfully invoked originalUpdate");
                        }
                        catch (Exception e)
                        {
                            Script.LogNS(LogLevel.Error,"failed invoking originalUpdate.", __instance);
                            Script.LogNS(LogLevel.Error,e.ToString());
                        }

                        return false;
                    }
                    break;
                }
            }
            
            Script.LogNS(LogLevel.Debug,"Prefix/2/ targetEnemy: " + SandwormData.targetEnemy + ", target: " + SandwormData.targetEnemy, __instance,debugSandworm && triggerFlag);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return;
            SandWormAIData SandwormData = (SandWormAIData)Utilities.GetEnemyData(__instance, new SandWormAIData());
            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;
            bool networkMovingTowardsPlayer = SandwormData.movingTowardsTargetPlayer;

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            Script.LogNS(LogLevel.Debug,$"Postfix targetingEntity: {targetingEntity}", __instance,debugSandworm && debugSpam);
            
            switch(targetingEntity)
            {
                case 0:
                {
                    if (!networkMovingTowardsPlayer && !__instance.inEmergingState && !__instance.emerged)
                    {
                        if (__instance.creatureSFX.isPlaying)
                        {
                            __instance.creatureSFX.Stop();
                            Script.LogNS(LogLevel.Debug,$"stopping Sounds", __instance,debugSandworm);
                        }
                    }
                }
                    break;
                case 1:
                    {
                        if (debugSandworm && SandwormData.closestEnemy != null && __instance.IsOwner) Script.LogNS(LogLevel.Info,$"closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /3/", __instance);
                        if (networkMovingTowardsPlayer) break;
                        if (!networkMovingTowardsPlayer)
                        {
                            if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                            {
                                int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                                __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                                __instance.creatureSFX.Play();
                                Script.LogNS(LogLevel.Debug,$"Started playing sounds", __instance,debugSandworm,debugSandworm);

                            }
                            if (!__instance.IsOwner)    
                            {
                                break;
                            }
                            if (SandwormData.targetEnemy == null)
                            {
                                Script.LogNS(LogLevel.Error,$"TargetEnemy is null! TargetingEntity set to false /Trigger 1/", __instance,debugSandworm);
                                //SandwormData.MovingTowardsTargetEntity = false;
                                SandwormData.NetworkSandwormBehaviorState = 0;
                                //__instance.SwitchToBehaviourState(0);
                                break;
                            }
                            if (Vector3.Distance(SandwormData.targetEnemy.transform.position, __instance.transform.position) > 22f)
                            {
                                __instance.chaseTimer += Time.deltaTime;
                                if (debugSandworm && debugSpam && triggerFlag) Script.LogNS(LogLevel.Debug,$"updated chaseTimer: {__instance.chaseTimer}", __instance);
                            }
                            else
                            {
                                __instance.chaseTimer = 0f;
                                Script.LogNS(LogLevel.Debug,$"Reset chasetimer", __instance,debugSandworm && triggerFlag);
                            }
                            if (__instance.chaseTimer > 6f)
                            {
                                Script.LogNS(LogLevel.Info,$"Chasing for too long. targetEnemy set to null", __instance,debugSandworm);
                                SandwormData.NetworkSandwormBehaviorState = 0;
                                //__instance.SwitchToBehaviourState(0);
                                SandwormData.targetEnemy = null;
                                __instance.chaseTimer = 0f;
                                SandwormData.MovingTowardsTargetEntity = false;
                            }
                        }
                    }
                    break;
            }
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool SandWormDoAIIntervalPrefix(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            SandWormAIData SandwormData = (SandWormAIData)Utilities.GetEnemyData(__instance, new SandWormAIData());
            Type type = __instance.GetType();
            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;
            Script.LogNS(LogLevel.Debug,$"DoAIInterval: checking chaseTimer: {__instance.chaseTimer}", __instance,debugSandworm && debugSpam && triggerFlag);


            
            switch (targetingEntity)
            {
                case 0:
                    {
                        if (!__instance.emerged && !__instance.inEmergingState)
                        {
                            List<EnemyAI> tempList = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                            LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);
                            if (Script.BoundingConfig.useExperimentalCoroutines.Value)
                            {
                                if (SandwormData.coroutineTimer < Time.realtimeSinceStartup) { __instance.StartCoroutine(LibraryCalls.FindClosestEnemyEnumerator(SandwormData.ChangeClosestEnemyAction, tempList, SandwormData.closestEnemy, __instance, usePathLenghtAsDistance: true)); SandwormData.coroutineTimer = Time.realtimeSinceStartup + 0.2f; }
                            }
                            else SandwormData.closestEnemy = LibraryCalls.FindClosestEnemy(ref tempList, SandwormData.closestEnemy, __instance, usePathLenghtAsDistance: Script.usePathToFindClosestEnemy);
                            __instance.agent.speed = 4f;
                            Script.LogNS(LogLevel.Info,$"DoAIInterval: assigned {SandwormData.closestEnemy} as closestEnemy",debugSandworm);
                            if (SandwormData.closestEnemy != null && Vector3.Distance(__instance.transform.position, SandwormData.closestEnemy.transform.position) < 15f)
                            {
                                Script.LogNS(LogLevel.Info,$"closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /1/", __instance,debugSandworm && triggerFlag);
                                __instance.SetDestinationToPosition(SandwormData.closestEnemy.transform.position);
                                SandwormData.NetworkSandwormBehaviorState = 1;
                                //__instance.SwitchToBehaviourState(1);
                                SandwormData.targetEnemy = SandwormData.closestEnemy;
                                SandwormData.MovingTowardsTargetEntity = true;
                                __instance.chaseTimer = 0;
                                Script.LogNS(LogLevel.Info,$"DoAIInterval: Set targetingEntity to {targetingEntity} and reset chaseTimer: {__instance.chaseTimer}", __instance,debugSandworm);
                                break;
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (SandwormData.closestEnemy != null) Script.LogNS(LogLevel.Info,$"closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /2/", __instance,debugSandworm && triggerFlag);
                        if (SandwormData.movingTowardsTargetPlayer) break;
                        if (SandwormData.targetEnemy == null || SandwormData.targetEnemy.isEnemyDead)
                        {
                            Script.LogNS(LogLevel.Error,$": targetEnemy is at null or dead. Setting targetingEntity to false /Trigger 2/", __instance,debugSandworm);
                            SandwormData.targetEnemy = null;
                            SandwormData.MovingTowardsTargetEntity = false;
                            SandwormData.NetworkSandwormBehaviorState = 0;
                            //__instance.SwitchToBehaviourState(0);
                            break;
                        }
                        SandwormData.targetEnemy = SandwormData.closestEnemy;
                        Script.LogNS(LogLevel.Info,$"DoAIInterval: Set {SandwormData.targetEnemy} as targetEnemy", __instance,debugSandworm);
                        if (SandwormData.targetEnemy != null)
                        {
                            if (Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) > 19f)
                            {
                                SandwormData.targetEnemy = null;
                                SandwormData.MovingTowardsTargetEntity = false;
                                Script.LogNS(LogLevel.Info,$"DoAIInterval: TargetEnemy too far! set to null", __instance,debugSandworm);
                                break;
                            }
                            if (!__instance.emerged && !__instance.inEmergingState)
                            {
                                __instance.SetDestinationToPosition(SandwormData.targetEnemy.transform.position, checkForPath: true);
                                if (debugSandworm && debugSpam) Script.LogNS(LogLevel.Debug,$"DoAIInterval: Set destitantion to {SandwormData.targetEnemy}", __instance);
                            }
                            if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                            {
                                Script.LogNS(LogLevel.Message,$"DoAIInterval: Emerging!", __instance);
                                //SandwormData.MovingTowardsTargetEntity = false;
                                SandwormData.NetworkSandwormBehaviorState = 0;
                                //__instance.SwitchToBehaviourState(0);
                                __instance.StartEmergeAnimation();
                            }
                        }
                    }
                break;
            }

            PlayerControllerB closestPlayer = __instance.GetClosestPlayer(false, true, true);
            float closestPlayerDistance = -1f;
            if (closestPlayer != null)
            {
                closestPlayerDistance = Vector3.Distance(__instance.GetClosestPlayer(false, true, true).transform.position, __instance.transform.position);
            }

            if (SandwormData.targetEnemy != null && closestPlayerDistance > Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position))
            {
                if (__instance.currentBehaviourStateIndex == 1)
                {
                    __instance.targetPlayer = null;
                    __instance.movingTowardsTargetPlayer = false;
                    __instance.SwitchToBehaviourState(0);
                }

                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();
                return false;
            }
            return true;
        }
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool OnCollideWithPlayeryPatch(SandWormAI __instance, Collider other)
        { 
            if (other != null)
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (__instance.emerged && !player.isInHangarShipRoom && !StartOfRound.Instance.shipIsLeaving && Script.BoundingConfig.sandwormDoNotEatPlayersInsideLeavingShip.Value)
                {
                    Script.LogNS(LogLevel.Info,"Prevented sandworm from eating player inside ship");
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        static bool OnCollideWithEnemyPatch(SandWormAI __instance, Collider other, EnemyAI? enemyScript)
        {
            if (Script.BoundingConfig.sandwormCollisionOverride.Value)
            {
                if (__instance.IsOwner && __instance.emerged)
                {
                    if (enemyScript != null && enemyScript.thisNetworkObject.IsSpawned)
                    {
                        enemyScript.thisNetworkObject.Despawn();
                    }
                }
                return false;
            }
            return true;
        }
    }
}
