using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using NaturalSelection.Generics;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{

    class BlobData()
	{
        internal EnemyAI? closestEnemy = null;
		internal bool playSound = false;
		internal List<EnemyAI> localEnemyList = new List<EnemyAI>();
		internal Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
    }

	[HarmonyPatch(typeof(BlobAI))]
	class BlobAIPatch
	{
		static Dictionary<BlobAI, BlobData> slimeList = [];
		static bool logBlob = Script.Bools["debugHygrodere"];
		static bool triggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> blobBlacklist = InitializeGamePatch.blobBlacklistFinal;
        static LNetworkEvent BlobEatCorpseEvent(BlobAI instance)
		{
            string NWID = "NSSlimeEatEvent" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugHygrodere") logBlob = value;
            if (entryKey == "debugTriggerFlags") triggerFlag = value;
			//Script.Logger.LogMessage($"Hygrodere received event. logBlob = {logBlob}, triggerFlag = {triggerFlag}");
        }


        [HarmonyPatch("Start")]
		[HarmonyPrefix]
		static void StartPatch(BlobAI __instance)
		{
			if (!slimeList.ContainsKey(__instance))
			{
				slimeList.Add(__instance, new BlobData());
			}
			if (Script.BoundingConfig.blobAICantOpenDoors.Value)
			{
				__instance.openDoorSpeedMultiplier = 0f;
			}
            BlobData blobData = slimeList[__instance];

            BlobEatCorpseEvent(__instance).OnClientReceived += EventReceived;

            void EventReceived()
            {
				blobData.playSound = true;
                //Script.Logger.LogMessage("Received event. Changed value to " + blobData.playSound + ", eventLimiter: " + eventLimiter);
            }

			Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }
		[HarmonyPatch("DoAIInterval")]
		[HarmonyPrefix]
		static bool DoAIIntervalPrefixPatch(BlobAI __instance)
		{
            if (__instance.isEnemyDead) return true;
            BlobData blobData = slimeList[__instance];

			if (Script.BoundingConfig.blobPathfind.Value == true)
			{
				if (__instance.GetClosestPlayer() != null && (!__instance.PlayerIsTargetable(__instance.GetClosestPlayer()) || blobData.closestEnemy != null && Vector3.Distance(blobData.closestEnemy.transform.position, __instance.transform.position) < Vector3.Distance(__instance.GetClosestPlayer().transform.position, __instance.transform.position)) || __instance.GetClosestPlayer() == null)
				{
					if (__instance.moveTowardsDestination)
					{
						__instance.agent.SetDestination(__instance.destination);
					}
					__instance.SyncPositionToClients();

					if (__instance.searchForPlayers.inProgress)
					{
						__instance.StopSearch(__instance.searchForPlayers);
					}
					if (blobData.closestEnemy != null) __instance.SetDestinationToPosition(blobData.closestEnemy.transform.position, true);
					return false;
				}
			}
			return true;
		}

        [HarmonyPatch("Update")]
		[HarmonyPrefix]
		static void BlobUpdatePatch(BlobAI __instance)
		{
            if (__instance.isEnemyDead) return;
            BlobData blobData = slimeList[__instance];
			Type type = __instance.GetType();

			foreach(KeyValuePair<EnemyAI, float> enemy in new Dictionary<EnemyAI, float>(blobData.hitRegistry))
			{
				if (enemy.Value > 1.5f)
				{
					blobData.hitRegistry.Remove(enemy.Key); continue;
				}
				blobData.hitRegistry[enemy.Key] += Time.deltaTime;
            }
			if (RoundManagerPatch.RequestUpdate(__instance) == true)
			{
				List<EnemyAI> tempList = LibraryCalls.GetCompleteList(__instance, true, 1);
                LibraryCalls.FilterEnemyList(ref tempList, null, blobBlacklist, __instance, false, true);
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, ref tempList);
			}
			if (__instance.IsOwner)
			{
				List<EnemyAI> temp = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                LibraryCalls.GetInsideOrOutsideEnemyList(ref temp, __instance);
				blobData.localEnemyList = temp;
				blobData.closestEnemy = LibraryCalls.FindClosestEnemy(ref blobData.localEnemyList, blobData.closestEnemy, __instance, Script.BoundingConfig.blobPathfindToCorpses.Value);
            }

			if (blobData.playSound)
			{
                Script.Logger.Log(BepInEx.Logging.LogLevel.Message,"Playing sound. NetworkObjectID: " + __instance.NetworkObjectId);
                __instance.creatureVoice.PlayOneShot(__instance.killPlayerSFX);
				blobData.playSound = false;
			}
        }
		
		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
            BlobData blobData = slimeList[__instance];

            if (!blobData.hitRegistry.ContainsKey(mainscript2) && !blobBlacklist.Contains(mainscript2.enemyType.enemyName))
			{
				if (mainscript2.isEnemyDead && IsEnemyImmortal.EnemyIsImmortal(mainscript2) == false && Vector3.Distance(__instance.transform.position, mainscript2.transform.position) <= 2.8f && Script.BoundingConfig.blobConsumesCorpses.Value)
				{
                    if (__instance.IsOwner && mainscript2.thisNetworkObject.IsSpawned)
					{
                        BlobEatCorpseEvent(__instance).InvokeClients();
                        Script.Logger.Log(BepInEx.Logging.LogLevel.Message,"Send event");
                        mainscript2.thisNetworkObject.Despawn(true);
					}
                    return;
                }
				if (!mainscript2.isEnemyDead)
				{
					if (mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI)
					{

                        blobData.hitRegistry.Add(mainscript2, 0);

						if (mainscript2 is FlowermanAI)
						{
							FlowermanAI? flowermanAI = mainscript2 as FlowermanAI;
							if (flowermanAI != null)
							{
								float AngerbeforeHit = flowermanAI.angerMeter;
								bool wasAngryBefore = flowermanAI.isInAngerMode;

								flowermanAI.HitEnemy(1, null, playHitSFX: true);
								if (mainscript2.enemyHP <= 0)
								{
									mainscript2.KillEnemyOnOwnerClient();
								}
								
								flowermanAI.targetPlayer = null;
								flowermanAI.movingTowardsTargetPlayer = false;
								flowermanAI.isInAngerMode = false;
								flowermanAI.angerMeter = AngerbeforeHit;
								flowermanAI.isInAngerMode = wasAngryBefore;
								return;
							}
						}

						if (mainscript2 is HoarderBugAI)
						{
							HoarderBugAI? hoarderBugAI = mainscript2 as HoarderBugAI;

							if (hoarderBugAI != null)
							{
								HoarderBugPatch.CustomOnHit(1, __instance, false, hoarderBugAI);
								if (mainscript2.enemyHP <= 0)
								{
									mainscript2.KillEnemyOnOwnerClient();
								}
							}
							return;
						}

                        mainscript2.HitEnemy(1, null, playHitSFX: true);
						if (mainscript2.enemyHP <= 0)
						{
							mainscript2.KillEnemyOnOwnerClient();
						}
					}
				}
            }
        }
    }
}
