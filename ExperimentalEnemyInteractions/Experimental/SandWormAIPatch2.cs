using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using UnityEngine;
using LethalNetworkAPI;
using System.Linq;
using BepInEx.Logging;
using Unity.Netcode;
using LogLevel = BepInEx.Logging.LogLevel;

namespace NaturalSelection.EnemyPatches
{
    class SandwormNetworking2 : NetworkBehaviour
    {

        public static SandwormNetworking2 Instance { get; private set; }

        [ServerRpc(RequireOwnership = true)]
        public void MovingTowardsPlayerServerRpc(ushort id, bool value)
        {
            Script.Logger.LogDebug($"Server RPC MovingTowardsTargetPlayer = {value}");
            MovingTowardsPlayerClientRpc(id,value);
        }

        [ClientRpc]
        public void MovingTowardsPlayerClientRpc(ushort id, bool value)
        {
            Script.Logger.LogDebug($"Client RPC MovingTowardsTargetPlayer = {value}");
            SandWormAI __instance = GetComponent<SandWormAI>();

            if (__instance.NetworkBehaviourId == id)
            {
                SandWormAIPatch2.NetworkMovingTowardsPlayer(__instance, value);
            }
            else
            {
                Script.Logger.LogWarning($"Wrong ID. Received {id} Expected: {__instance.NetworkBehaviourId}");
            }
        }

        [ServerRpc(RequireOwnership = true)]
        public void MovingTowardsEnemyServerRpc(ushort id, bool value)
        {
            Script.Logger.LogDebug($"Server RPC MovingTowardsTargetEntity = {value}");
            MovingTowardsEnemyClientRpc(id,value);

        }

        [ClientRpc]
        public void MovingTowardsEnemyClientRpc(ushort id, bool value)
        {
            Script.Logger.LogDebug($"Client RPC MovingTowardsTargetEntity = {value}");
            SandWormAI __instance = GetComponent<SandWormAI>();

            if (__instance.NetworkBehaviourId == id)
            {
                SandWormAIPatch2.NetworkMovingTowardsEnemy(__instance, value);
            }
            else
            {
                Script.Logger.LogWarning($"Wrong ID. Received {id} Expected: {__instance.NetworkBehaviourId}");
            }
        }

        [ServerRpc(RequireOwnership = true)]
        public void NetworkBehaviorStateServerRpc(ushort id, int value)
        {
            Script.Logger.LogDebug($"Server RPC NetworkSandwormBehaviorState = {value}");
            NetworkBehaviorStateClientRpc(id,value);
        }

        [ClientRpc]
        public void NetworkBehaviorStateClientRpc(ushort id, int value)
        {
            Script.Logger.LogDebug($"Client RPC NetworkSandwormBehaviorState = {value}");
            SandWormAI __instance = GetComponent<SandWormAI>();

            if (__instance.NetworkBehaviourId == id)
            {
                SandWormAIPatch2.NetworkNetworkBehaviorState(__instance, value);
            }
            else
            {
                Script.Logger.LogWarning($"Wrong ID. Received {id} Expected: {__instance.NetworkBehaviourId}");
            }
        }
    }

    class ExtendedSandWormAIData2()
    {
        internal float refreshCDtime = 0.5f;
        internal EnemyAI? closestEnemy = null;
        internal EnemyAI? targetEnemy = null;
        //public int targetingEntity = 0;
        internal float clearEnemiesTimer = 0f;
        internal bool MovingTowardsTargetPlayer = false;
        internal bool MovingTowardsTargetEntity = false;
        internal int NetworkSandwormBehaviorState = 0;
    }

    [HarmonyPatch(typeof(SandWormAI))]
    class SandWormAIPatch2
    {
        static bool debugSandworm = Script.Bools["debugSandworms"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static bool triggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> sandwormBlacklist = InitializeGamePatch.sandwormBlacklistFinal;

        /*
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
        */

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void NetworkMovingTowardsPlayer(SandWormAI __instance, bool value)
        {
            sandworms[__instance].MovingTowardsTargetPlayer = value;
        }

        public static void NetworkMovingTowardsEnemy(SandWormAI __instance, bool value)
        {
            sandworms[__instance].MovingTowardsTargetEntity = value;
        }

        public static void NetworkNetworkBehaviorState(SandWormAI __instance, int value)
        {
            sandworms[__instance].NetworkSandwormBehaviorState = value;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        static Dictionary<SandWormAI, ExtendedSandWormAIData2> sandworms = [];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugSandworms") debugSandworm = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            if (entryKey == "debugTriggerFlags") triggerFlag = value;
            //Script.Logger.Log(LogLevel.Message,$"Earth Leviathan received event. debugSandworm = {debugSandworm}, debugSpam = {debugSpam}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void SandWormStartPatch(SandWormAI __instance)
        {

            if (!sandworms.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                sandworms.Add(__instance, new ExtendedSandWormAIData2());
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool SandWormPrefixUpdatePatch(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            CheckDataIntegrityWorm(__instance);
            ExtendedSandWormAIData2 SandwormData = sandworms[__instance];
            Type type = __instance.GetType();

            if (__instance.IsOwner)
            {
                SandwormNetworking2.Instance.MovingTowardsPlayerServerRpc(__instance.NetworkBehaviourId, __instance.movingTowardsTargetPlayer);
                //NetworkMovingTowardsPlayer(__instance).Value = __instance.movingTowardsTargetPlayer;
            }

            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;
            bool networkMovingTowardsPlayer = SandwormData.MovingTowardsTargetPlayer;

            if (SandwormData.refreshCDtime <= 0)
            {
                if (RoundManagerPatch.RequestUpdate(__instance) == true)
                {
                    List<EnemyAI> tempList = LibraryCalls.GetCompleteList(__instance, true, 0);
                    Dictionary<EnemyAI, int> tempDict = new Dictionary<EnemyAI, int>();
                    LibraryCalls.FilterEnemyList(ref tempList, sandwormBlacklist, __instance, true);
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
                    SandwormData.closestEnemy = LibraryCalls.FindClosestEnemy(ref tempList, SandwormData.closestEnemy, __instance);
                }
                SandwormData.refreshCDtime = 0.2f;
            }
            if (SandwormData.refreshCDtime > 0)
            {
                SandwormData.refreshCDtime -= Time.deltaTime;
            }

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
                    if ((__instance.IsOwner && SandwormData.targetEnemy != null || !__instance.IsOwner && SandwormData.MovingTowardsTargetEntity) && !networkMovingTowardsPlayer)
                    {
                        if (__instance.IsOwner)
                        {
                            if (SandwormData.targetEnemy != null)
                            {
                                SandwormNetworking2.Instance.MovingTowardsEnemyServerRpc(__instance.NetworkBehaviourId, true);
                                //NetworkTargetingEntity(__instance).Value = true;
                            }
                            if (__instance.updateDestinationInterval >= 0f)
                            {
                                __instance.updateDestinationInterval -= Time.deltaTime;
                            }
                            else
                            {
                                if (debugSandworm && debugSpam) Script.Logger.Log(LogLevel.Debug,LibraryCalls.DebugStringHead(__instance) + " calling DoAIInterval");
                                __instance.DoAIInterval();
                                __instance.updateDestinationInterval = __instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
                            }
                        }

                        //Script.Logger.Log(LogLevel.Message,$"{LibraryCalls.DebugStringHead(__instance)} Invoking originalUpdate");
                        try
                        {
                            ReversePatchAI.originalUpdate.Invoke(__instance);
                            //Script.Logger.Log(LogLevel.Message,"Succesfully invoked originalUpdate");
                        }
                        catch (Exception e)
                        {
                            Script.Logger.Log(LogLevel.Error,"failed invoking originalUpdate.");
                            Script.Logger.Log(LogLevel.Error,e);
                        }

                        return false;
                    }
                    break;
                }
            }

            if (debugSandworm && triggerFlag) Script.Logger.Log(LogLevel.Debug,LibraryCalls.DebugStringHead(__instance) + " Prefix/2/ targetEnemy: " + SandwormData.targetEnemy + ", target: " + SandwormData.targetEnemy);
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SandWormPostfixUpdatePatch(SandWormAI __instance)
        {
            if (__instance.isEnemyDead) return;
            CheckDataIntegrityWorm(__instance);
            ExtendedSandWormAIData2 SandwormData = sandworms[__instance];
            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;
            bool networkMovingTowardsPlayer = SandwormData.MovingTowardsTargetPlayer;

            if (Script.BoundingConfig.enableLeviathan.Value != true) return;

            if (debugSandworm && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} Postfix targetingEntity: {targetingEntity}");

            switch(targetingEntity)
            {
                case 0:
                {
                    if (!networkMovingTowardsPlayer && !__instance.inEmergingState && !__instance.emerged)
                    {
                        if (__instance.creatureSFX.isPlaying)
                        {
                            __instance.creatureSFX.Stop();
                            if (debugSandworm) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} stopping Sounds");
                        }
                    }
                }
                    break;
                case 1:
                    {
                        if (debugSandworm && SandwormData.closestEnemy != null && __instance.IsOwner) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /3/");
                        if (networkMovingTowardsPlayer) break;
                        if (!networkMovingTowardsPlayer)
                        {
                            if (!__instance.creatureSFX.isPlaying && !__instance.inEmergingState && !__instance.emerged)
                            {
                                int num = UnityEngine.Random.Range(0, __instance.ambientRumbleSFX.Length);
                                __instance.creatureSFX.clip = __instance.ambientRumbleSFX[num];
                                __instance.creatureSFX.Play();
                                if (debugSandworm) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} Started playing sounds");

                            }
                            if (!__instance.IsOwner)    
                            {
                                break;
                            }
                            if (SandwormData.targetEnemy == null)
                            {
                                if (debugSandworm) Script.Logger.Log(LogLevel.Error,$"{LibraryCalls.DebugStringHead(__instance)} TargetEnemy is null! TargetingEntity set to false /Trigger 1/");
                                SandwormNetworking2.Instance.NetworkBehaviorStateServerRpc(__instance.NetworkBehaviourId,0);
                                //NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                break;
                            }
                            if (Vector3.Distance(SandwormData.targetEnemy.transform.position, __instance.transform.position) > 22f)
                            {
                                __instance.chaseTimer += Time.deltaTime;
                                if (debugSandworm && debugSpam && triggerFlag) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} updated chaseTimer: {__instance.chaseTimer}");
                            }
                            else
                            {
                                __instance.chaseTimer = 0f;
                                if (debugSandworm && triggerFlag) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} Reset chasetimer");
                            }
                            if (__instance.chaseTimer > 6f)
                            {
                                if (debugSandworm) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} Chasing for too long. targetEnemy set to null");
                                SandwormNetworking2.Instance.NetworkBehaviorStateServerRpc(__instance.NetworkBehaviourId,0);
                                //NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                SandwormData.targetEnemy = null;
                                SandwormNetworking2.Instance.MovingTowardsEnemyServerRpc(__instance.NetworkBehaviourId,false);
                                //NetworkTargetingEntity(__instance).Value = false;
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
            CheckDataIntegrityWorm(__instance);
            ExtendedSandWormAIData2 SandwormData = sandworms[__instance];
            Type type = __instance.GetType();
            int targetingEntity = SandwormData.NetworkSandwormBehaviorState;
            if (debugSandworm && debugSpam && triggerFlag) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: checking chaseTimer: {__instance.chaseTimer}");

            switch (targetingEntity)
            {
                case 0:
                    {
                        if (!__instance.emerged && !__instance.inEmergingState)
                        {
                            List<EnemyAI> tempList = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                            LibraryCalls.GetInsideOrOutsideEnemyList(ref tempList, __instance);
                            SandwormData.closestEnemy = LibraryCalls.FindClosestEnemy(ref tempList, SandwormData.closestEnemy, __instance);
                            __instance.agent.speed = 4f;
                            if (debugSandworm) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: assigned {SandwormData.closestEnemy} as closestEnemy");
                            if (SandwormData.closestEnemy != null && Vector3.Distance(__instance.transform.position, SandwormData.closestEnemy.transform.position) < 15f)
                            {
                                if (debugSandworm && triggerFlag) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /1/");
                                __instance.SetDestinationToPosition(SandwormData.closestEnemy.transform.position);
                                SandwormNetworking2.Instance.NetworkBehaviorStateServerRpc(__instance.NetworkBehaviourId, 1);
                                //NetworkSandwormBehaviorState(__instance).Value = 1;
                                //__instance.SwitchToBehaviourState(1);
                                SandwormData.targetEnemy = SandwormData.closestEnemy;
                                SandwormNetworking2.Instance.MovingTowardsEnemyServerRpc(__instance.NetworkBehaviourId, true);
                                //NetworkTargetingEntity(__instance).Value = true;
                                __instance.chaseTimer = 0;
                                if (debugSandworm) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Set targetingEntity to {targetingEntity} and reset chaseTimer: {__instance.chaseTimer}");
                                break;
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (debugSandworm && SandwormData.closestEnemy != null && triggerFlag) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} closestEnemy is {LibraryCalls.DebugStringHead(SandwormData.closestEnemy)}, isEnemyDead: {SandwormData.closestEnemy.isEnemyDead} /2/");
                        if (__instance.movingTowardsTargetPlayer) break;
                        if (SandwormData.targetEnemy == null || SandwormData.targetEnemy.isEnemyDead)
                        {
                            if (debugSandworm) Script.Logger.Log(LogLevel.Error,$"{LibraryCalls.DebugStringHead(__instance)}: targetEnemy is at null or dead. Setting targetingEntity to false /Trigger 2/");
                            SandwormData.targetEnemy = null;
                            SandwormNetworking2.Instance.MovingTowardsEnemyServerRpc(__instance.NetworkBehaviourId, false);
                            SandwormNetworking2.Instance.NetworkBehaviorStateServerRpc(__instance.NetworkBehaviourId, 0);
                            //NetworkTargetingEntity(__instance).Value = false;
                            //NetworkSandwormBehaviorState(__instance).Value = 0;
                            //__instance.SwitchToBehaviourState(0);
                            break;
                        }
                        SandwormData.targetEnemy = SandwormData.closestEnemy;
                        if (debugSandworm) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Set {SandwormData.targetEnemy} as targetEnemy");
                        if (SandwormData.targetEnemy != null)
                        {
                            if (Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) > 19f)
                            {
                                SandwormData.targetEnemy = null;
                                SandwormNetworking2.Instance.MovingTowardsEnemyServerRpc(__instance.NetworkBehaviourId, false);
                                //NetworkTargetingEntity(__instance).Value = false;
                                if (debugSandworm) Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: TargetEnemy too far! set to null");
                                break;
                            }
                            if (!__instance.emerged && !__instance.inEmergingState)
                            {
                                __instance.SetDestinationToPosition(SandwormData.targetEnemy.transform.position, checkForPath: true);
                                if (debugSandworm && debugSpam) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Set destitantion to {SandwormData.targetEnemy}");
                            }
                            if (__instance.chaseTimer < 1.5f && Vector3.Distance(__instance.transform.position, SandwormData.targetEnemy.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(__instance.transform.position), __instance.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
                            {
                                Script.Logger.Log(LogLevel.Message,$"{LibraryCalls.DebugStringHead(__instance)} DoAIInterval: Emerging!");
                                SandwormNetworking2.Instance.NetworkBehaviorStateServerRpc(__instance.NetworkBehaviourId, 0);
                                //NetworkSandwormBehaviorState(__instance).Value = 0;
                                //__instance.SwitchToBehaviourState(0);
                                __instance.StartEmergeAnimation();
                            }
                        }
                    }
                break;
            }

            if (!__instance.movingTowardsTargetPlayer && __instance.currentBehaviourStateIndex == 1)
            {
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
                    Script.Logger.Log(LogLevel.Info,"Prevented sandworm from eating player inside ship");
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

        public static void CheckDataIntegrityWorm(SandWormAI __instance)
        {
            if (!sandworms.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                sandworms.Add(__instance, new ExtendedSandWormAIData2());
            }
        }
    }
}
