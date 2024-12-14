using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{

	class BlobData
	{
        public float timeSinceHittingLocalMonster = 0f;
        public EnemyAI? closestEnemy = null;
    }

	[HarmonyPatch(typeof(BlobAI))]
	public class BlobAIPatch
	{
		static Dictionary<BlobAI, BlobData> slimeList = [];

		static bool logBlob = Script.BoundingConfig.debugHygrodere.Value;

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static void StartPatch(BlobAI __instance)
		{
			if (!slimeList.ContainsKey(__instance))
			{
				slimeList.Add(__instance, new BlobData());
			}
        }
		[HarmonyPatch("DoAIInterval")]
		[HarmonyPrefix]
		static bool DoAIIntervalPrefixPatch(BlobAI __instance)
		{
            BlobData blobData = slimeList[__instance];

			if (Script.BoundingConfig.blobPathfind.Value == true)
			{
				if (blobData.closestEnemy != null && Vector3.Distance(__instance.transform.position, __instance.GetClosestPlayer().transform.position) > Vector3.Distance(__instance.transform.position, blobData.closestEnemy.transform.position) && Script.BoundingConfig.blobPathfindToCorpses.Value)
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
					__instance.SetDestinationToPosition(blobData.closestEnemy.transform.position, true);
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

            blobData.timeSinceHittingLocalMonster += Time.deltaTime;
			if (RoundManagerPatch.RequestUpdate(__instance) == true)
			{
				RoundManagerPatch.ScheduleGlobalListUpdate(__instance, EnemyAIPatch.FilterEnemyList(EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(__instance, true, 1), __instance), null, __instance, false, true));
			}
			blobData.closestEnemy = EnemyAIPatch.FindClosestEnemy(NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[__instance.GetType()], blobData.closestEnemy, __instance, true);
		}


		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
            BlobData blobData = slimeList[__instance];

            if (blobData.timeSinceHittingLocalMonster > 1.5f)
			{
                if (mainscript2.isEnemyDead && IsEnemyImmortal.EnemyIsImmortal(mainscript2) == false && Vector3.Distance(__instance.transform.position, mainscript2.transform.position) <= 2.8f && Script.BoundingConfig.blobConsumesCorpses.Value)
                {
					mainscript2.thisNetworkObject.Despawn(true);
                    __instance.creatureVoice.PlayOneShot(__instance.killPlayerSFX);
                    return;
                }
				if (!mainscript2.isEnemyDead)
				{
					if (mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI)
					{

						blobData.timeSinceHittingLocalMonster = 0f;

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
								HoarderBugPatch.CustomOnHit(1, __instance, true, hoarderBugAI);
								if (mainscript2.enemyHP <= 0)
								{
									mainscript2.KillEnemyOnOwnerClient();
								}
							}
							return;
						}

						blobData.timeSinceHittingLocalMonster = 0f;
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
