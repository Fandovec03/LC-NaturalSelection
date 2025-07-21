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
using NaturalSelectionLib;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace NaturalSelection.EnemyPatches
{

    class BlobData : EnemyDataBase
	{
		internal bool playSound = false;
		internal Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
    }

	[HarmonyPatch(typeof(BlobAI))]
	class BlobAIPatch
	{
		//static Dictionary<BlobAI, BlobData> slimeList = [];
		static bool logBlob = Script.Bools["debugHygrodere"];
		static bool triggerFlag = Script.Bools["debugTriggerFlags"];
        static List<string> blobBlacklist = InitializeGamePatch.blobBlacklist;
        static LNetworkEvent BlobEatCorpseEvent(BlobAI instance)
		{
            string NWID = "NSSlimeEatEvent" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugHygrodere") logBlob = value;
            if (entryKey == "debugTriggerFlags") triggerFlag = value;
			//Script.LogNSMessage($"Hygrodere received event. logBlob = {logBlob}, triggerFlag = {triggerFlag}");
        }


        [HarmonyPatch("Start")]
		[HarmonyPrefix]
		static void StartPatch(BlobAI __instance)
		{
            BlobData blobData = (BlobData)Utilities.GetEnemyData(__instance, new BlobData());
			blobData.SetOwner(__instance);
			blobData.Subscribe();
            __instance.enemyType.doorSpeedMultiplier = Script.BoundingConfig.blobAIOpeningDoorsMultiplier.Value;

            BlobEatCorpseEvent(__instance).OnClientReceived += EventReceived;

            void EventReceived()
            {
				blobData.playSound = true;
            }

            blobData.ChangeClosestEnemyAction += getClosestEnemyResult;

            void getClosestEnemyResult(EnemyAI? closestEnemy)
            {
                Script.LogNS(LogLevel.Info, $"Set {closestEnemy} as closestEnemy", __instance);
                Utilities.GetEnemyData(__instance, new BlobData()).closestEnemy = closestEnemy;
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }
		[HarmonyPatch("DoAIInterval")]
		[HarmonyPrefix]
		static bool DoAIIntervalPrefixPatch(BlobAI __instance)
		{
            if (__instance.isEnemyDead) return true;
            BlobData blobData = (BlobData)Utilities.GetEnemyData(__instance, new BlobData());

			if (Script.BoundingConfig.blobPathfind.Value == true)
			{

				GameObject? closestDeadBody = null;
				if (RoundManagerPatch.deadEnemiesList.Count > 0)
				{
					foreach (GameObject body in RoundManagerPatch.deadEnemiesList)
					{
						if (closestDeadBody == null || Vector3.Distance(closestDeadBody.transform.position, __instance.transform.position) > Vector3.Distance(body.transform.position, __instance.transform.position))
						{
							closestDeadBody = body;
						}
					}
				}

				if (__instance.GetClosestPlayer() != null &&
					(!__instance.PlayerIsTargetable(__instance.GetClosestPlayer()) ||
					blobData.closestEnemy != null && Vector3.Distance(blobData.closestEnemy.transform.position, __instance.transform.position) < Vector3.Distance(__instance.GetClosestPlayer().transform.position, __instance.transform.position) ||
					closestDeadBody != null && Vector3.Distance(closestDeadBody.transform.position, __instance.transform.position) < Vector3.Distance(__instance.GetClosestPlayer().transform.position, __instance.transform.position)) ||
					__instance.GetClosestPlayer() == null)
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

					if (blobData.closestEnemy != null)
					{
						float num1 = Vector3.Distance(blobData.closestEnemy.transform.position, __instance.transform.position);
						float num2 = 0f;
						if (closestDeadBody != null) num2 = Vector3.Distance(closestDeadBody.transform.position, __instance.transform.position);
                        if (closestDeadBody != null && num1 > num2)
						{
                            __instance.SetDestinationToPosition(closestDeadBody.transform.position, true);
                        }
                        else __instance.SetDestinationToPosition(blobData.closestEnemy.transform.position, true);
                    }
					else if (closestDeadBody != null)
					{
                        __instance.SetDestinationToPosition(closestDeadBody.transform.position, true);
                    }
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
            BlobData blobData = (BlobData)Utilities.GetEnemyData(__instance, new BlobData());
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
                LibraryCalls.FilterEnemyList(ref tempList, blobBlacklist, __instance, true);
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, ref tempList);
			}
			if (__instance.IsOwner)
			{
				List<EnemyAI> temp = NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type];
                LibraryCalls.GetInsideOrOutsideEnemyList(ref temp, __instance);
				//blobData.closestEnemy = LibraryCalls.FindClosestEnemy(ref temp, blobData.closestEnemy, __instance, Script.BoundingConfig.blobPathfindToCorpses.Value);
				if (Script.BoundingConfig.useExperimentalCoroutines.Value)
				{
					if (blobData.coroutineTimer < Time.realtimeSinceStartup) { __instance.StartCoroutine(LibraryCalls.FindClosestEnemyEnumerator(blobData.ChangeClosestEnemyAction, temp, blobData.closestEnemy, __instance ,usePathLenghtAsDistance: Script.usePathToFindClosestEnemy)); blobData.coroutineTimer = Time.realtimeSinceStartup + 0.2f; }
				}
				else
				{
					blobData.closestEnemy = LibraryCalls.FindClosestEnemy(ref temp, blobData.closestEnemy, __instance, usePathLenghtAsDistance: Script.usePathToFindClosestEnemy);
                }
            }

            if (blobData.playSound)
			{
                Script.LogNS(BepInEx.Logging.LogLevel.Message,"Playing sound.", __instance);
                __instance.creatureVoice.PlayOneShot(__instance.killPlayerSFX);
				blobData.playSound = false;
			}
        }

		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
			BlobData blobData = (BlobData)Utilities.GetEnemyData(__instance, new BlobData());

			if (!blobData.hitRegistry.ContainsKey(mainscript2) && !blobBlacklist.Contains(mainscript2.enemyType.enemyName))
			{
				if (mainscript2.isEnemyDead && IsEnemyImmortal.EnemyIsImmortal(mainscript2) == false && Vector3.Distance(__instance.transform.position, mainscript2.transform.position) <= 2.8f && Script.BoundingConfig.blobConsumesCorpses.Value)
				{
					if (__instance.IsOwner && mainscript2.thisNetworkObject.IsSpawned)
					{
						BlobEatCorpseEvent(__instance).InvokeClients();
						Script.LogNS(LogLevel.Message, $"consumed dead body of {mainscript2.enemyType.enemyName}", __instance);
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

        public static void OnEnemyCorpseCollision(BlobAI __instance, GameObject corpse)
        {
			NetworkObject nwObj = corpse.GetComponent<NetworkObject>();
            if (__instance.IsOwner && nwObj.IsSpawned)
            {
                BlobEatCorpseEvent(__instance).InvokeClients();
                Script.LogNS(LogLevel.Message, $"consumed dead body {LibraryCalls.DebugStringHead(corpse)}", __instance);
                nwObj.Despawn(true);
            }
        }
    }
}
