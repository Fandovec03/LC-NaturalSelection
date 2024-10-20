using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using UnityEngine.ProBuilder.MeshOperations;

namespace ExperimentalEnemyInteractions.Patches
{

	class BlobData
	{
        public float timeSinceHittingLocalMonster = 0f;
        public EnemyAI? closestEnemy = null;
    }

	[HarmonyPatch(typeof(BlobAI))]
	public class BlobAIPatch
	{
		static float CDtimer = 0.5f;
		static List<EnemyAI> whiteList = new List<EnemyAI>();
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
		/*
		[HarmonyPatch("DoAIInterval")]
		[HarmonyPostfix]
		static void DoAIIntervalPrefixPatch(BlobAI __instance)
		{
            BlobData blobData = slimeList[__instance];

            if (!blobData.closestEnemy.isEnemyDead && Vector3.Distance(__instance.transform.position,__instance.GetClosestPlayer().transform.position) < Vector3.Distance(__instance.transform.position, blobData.closestEnemy.transform.position))
			{
				__instance.StopSearch(__instance.searchForPlayers);
				__instance.SetDestinationToPosition(blobData.closestEnemy.transform.position);
			}
		}
		*/
        [HarmonyPatch("Update")]
		[HarmonyPrefix]
		static void BlobUpdatePatch(BlobAI __instance)
		{
			BlobData blobData = slimeList[__instance];

            blobData.timeSinceHittingLocalMonster += Time.deltaTime;
			if (CDtimer > 0)
			{
				CDtimer -= Time.deltaTime;
			}
			if (CDtimer <= 0)
			{
				List<EnemyAI> tempList = new List<EnemyAI>();

				tempList = EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance);

				for (int i = 0; i < tempList.Count; i++)
				{
					if (tempList[i] != IsEnemyImmortal.EnemyIsImmortal(tempList[i]))
					{
						if (tempList[i] is NutcrackerEnemyAI)
						{
							if (logBlob) Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + tempList[i] + " is blacklisted!");
						}
						else
						{
							if (!whiteList.Contains(tempList[i]))
							{
								if (logBlob) Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Added " + tempList[i] + " to whitelist");
								whiteList.Add(tempList[i]);
							}
							if (whiteList.Contains(tempList[i]))
							{
								if (logBlob) Script.Logger.LogWarning(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + tempList[i] + " is already in the whitelist");
							}
						}
					}
				}

				for (int i = 0; i < whiteList.Count; i++)
				{
					if (whiteList[i] == null)
					{
						if (logBlob) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": found NULL enemz in whitelist. removing.");
						whiteList.Remove(whiteList[i]);
					}
				}

                CDtimer = 0.5f;

            }
		}


		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
			if (slimeList.ContainsKey(__instance))
			{
            BlobData blobData = slimeList[__instance];

            if (blobData.timeSinceHittingLocalMonster > 1.5f)
			{
					if (mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI && !__instance.isEnemyDead)
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

						else if (mainscript2 is HoarderBugAI)
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
						}

						else
						{
							blobData.timeSinceHittingLocalMonster = 0f;
							mainscript2.HitEnemy(1, null, playHitSFX: true);
							if (mainscript2.enemyHP <= 0)
							{
								mainscript2.KillEnemyOnOwnerClient();
							}
							return;
						}
					}
                }
            }
		}
	}
}
