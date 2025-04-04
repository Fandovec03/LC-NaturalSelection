using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using NaturalSelection.Generics;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace NaturalSelection.EnemyPatches
{
	
	class BlobData
	{
        public EnemyAI? closestEnemy = null;
		public bool playSound = false;
		public List<EnemyAI> localEnemyList = new List<EnemyAI>();
		public Dictionary<EnemyAI, float> hitRegistry = new Dictionary<EnemyAI, float>();
    }

	[HarmonyPatch(typeof(BlobAI))]
	public class BlobAIPatch
	{
		static Dictionary<BlobAI, BlobData> slimeList = [];
		static bool logBlob = Script.BoundingConfig.debugHygrodere.Value;
        static List<string> blacklist = Script.BoundingConfig.blobBlacklist.Value.ToUpper().Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
        static LNetworkEvent BlobEatCorpseEvent(BlobAI instance)
		{
            string NWID = "NSSlimeEatEvent" + instance.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
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
            ///////////////////////////////////////Networking

            BlobEatCorpseEvent(__instance).OnClientReceived += EventReceived;

            void EventReceived()
            {
				slimeList[__instance].playSound = true;
                //Script.Logger.LogMessage("Received event. Changed value to " + blobData.playSound + ", eventLimiter: " + eventLimiter);
            }
        }
		[HarmonyPatch("DoAIInterval")]
		[HarmonyPrefix]
		static bool DoAIIntervalPrefixPatch(BlobAI __instance)
		{
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
			BlobData blobData = slimeList[__instance];
			Type type = __instance.GetType();
            /*void EventReceived()
			{
                blobData.playSound = true;
                //Script.Logger.LogMessage("Received event. Changed value to " + blobData.playSound + ", eventLimiter: " + eventLimiter);
            }*/

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
				List<EnemyAI> tempList = EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetCompleteList(__instance, true, 1), null, blacklist, __instance, false, true).ToList();
                RoundManagerPatch.ScheduleGlobalListUpdate(__instance, tempList);
			}
			if (__instance.IsOwner)
			{
				blobData.localEnemyList = EnemyAIPatch.GetInsideOrOutsideEnemyList(NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[type], __instance).ToList();
				blobData.closestEnemy = EnemyAIPatch.FindClosestEnemy(blobData.localEnemyList, blobData.closestEnemy, __instance, Script.BoundingConfig.blobPathfindToCorpses.Value);
                EnemyAIPatch.addToAPModifier(blobData.closestEnemy);
            }

            //BlobEatCorpseEvent(__instance).OnClientReceived += EventReceived;

			if (blobData.playSound)
			{
                Script.Logger.LogMessage("Playing sound. NetworkObjectID: " + __instance.NetworkObjectId);
                __instance.creatureVoice.PlayOneShot(__instance.killPlayerSFX);
				blobData.playSound = false;
			}
        }
		
		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
            BlobData blobData = slimeList[__instance];

            if (!blobData.hitRegistry.ContainsKey(mainscript2))
			{
				if (mainscript2.isEnemyDead && IsEnemyImmortal.EnemyIsImmortal(mainscript2) == false && Vector3.Distance(__instance.transform.position, mainscript2.transform.position) <= 2.8f && Script.BoundingConfig.blobConsumesCorpses.Value)
				{
                    //__instance.creatureVoice.PlayOneShot(__instance.killPlayerSFX);

                    if (__instance.IsOwner && mainscript2.thisNetworkObject.IsSpawned)
					{
                        BlobEatCorpseEvent(__instance).InvokeClients();
                        Script.Logger.LogMessage("Send event");
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
