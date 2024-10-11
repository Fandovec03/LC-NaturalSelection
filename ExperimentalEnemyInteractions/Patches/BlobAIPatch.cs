using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace ExperimentalEnemyInteractions.Patches
{
	[HarmonyPatch(typeof(BlobAI))]
	public class BlobAIPatch
	{
		static float timeSinceHittingLocalMonster = 0f;
		static float CDtimer = 0.5f;
		static List<EnemyAI> whiteList = new List<EnemyAI>();
		static EnemyAI? closestEnemy = null;

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static void BlobUpdatePatch(EnemyAI __instance)
		{
			timeSinceHittingLocalMonster += Time.deltaTime;
			if (CDtimer > 0)
			{
				CDtimer -= Time.deltaTime;
			}
			if (CDtimer <= 0)
			{
				List<EnemyAI> tempList = new List<EnemyAI>();

				tempList = EnemyAIPatch.GetInsideEnemyList(__instance);

				for (int i = 0; i < tempList.Count; i++)
				{
					if (tempList[i] != IsEnemyImmortal.EnemyIsImmortal(tempList[i]))
					{
						if (tempList[i] is NutcrackerEnemyAI)
						{
							Script.Logger.LogInfo(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + tempList[i] + " is blacklisted!");
						}
						else
						{
							if (!whiteList.Contains(tempList[i]))
							{
								Script.Logger.LogMessage(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Added "+ tempList[i] +" to whitelist");
								whiteList.Add(tempList[i]);
							}
							if (whiteList.Contains(tempList[i]))
							{
								Script.Logger.LogWarning(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + tempList[i] + " is already in the whitelist");
							}
						}
					}
				}

				for (int i = 0; i < whiteList.Count; i++)
				{
					if (whiteList[i] == null)
					{
						Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": found NULL enemz in whitelist. removing.");
						whiteList.Remove(whiteList[i]);
					}
				}
			}

			EnemyAIPatch.findClosestEnemy(whiteList, closestEnemy, __instance);	
		}


		public static void OnCustomEnemyCollision(BlobAI __instance, EnemyAI mainscript2)
		{
			BlobAI blobAI = __instance;
			if (timeSinceHittingLocalMonster > 1.5f && mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI)
            {
                if (mainscript2 is FlowermanAI)
                {
                     FlowermanAI? flowermanAI = mainscript2 as FlowermanAI;
                     if (flowermanAI != null)
                     {
                         float AngerbeforeHit = flowermanAI.angerMeter;
						bool wasAngryBefore = flowermanAI.isInAngerMode;
                          timeSinceHittingLocalMonster = 0f;
								
                          flowermanAI.HitEnemy(1, null, playHitSFX: true);
                          flowermanAI.isInAngerMode = false;
                          flowermanAI.angerMeter = AngerbeforeHit;
						flowermanAI.isInAngerMode = wasAngryBefore;
								
                    }  
                }

                else
                {
                    blobAI.timeSinceHittingLocalPlayer = 0f;
                    mainscript2.HitEnemy(1, null, playHitSFX: true);
            	}
            }
		}
	}
}
