using HarmonyLib;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class SpiderWebValues()
    {
        internal EnemyAI? trappedEnemy = null;
        internal EnemyAI? enemyReference = null;
        internal float enemyReferenceTimer = 0f;
    }


    [HarmonyPatch(typeof(SandSpiderWebTrap))]
    class SandSpiderWebTrapPatch
    {
        class EnemyInfo(EnemyAI enemy, float enterAgentSpeed, float enterAnimationSpeed)
        {
            internal EnemyAI EnemyAI { get; set; } = enemy;
            internal float EnterAgentSpeed { get; set; } = enterAgentSpeed;
            internal float EnterAnimationSpeed { get; set; } = enterAnimationSpeed;
            internal List<SandSpiderWebTrap> NumberOfTraps { get; set; } = new List<SandSpiderWebTrap>();
        }

        static Dictionary<SandSpiderWebTrap, SpiderWebValues> spiderWebs = new Dictionary<SandSpiderWebTrap, SpiderWebValues>();
        static Dictionary<EnemyAI, EnemyInfo> enemyData = new Dictionary<EnemyAI, EnemyInfo>();
        static bool debugLogs = Script.Bools["debugBool"];
        static bool debugWebs = Script.Bools["debugSpiderWebs"];
        static Dictionary<string, float> speedModifierDictionary = InitializeGamePatch.speedModifierDictionay;
        static List<string> spiderWebBlacklist = InitializeGamePatch.spiderWebBlacklistFinal;
        static float webStrenght = Script.BoundingConfig.webStrength.Value;

        static float debugCD = 0.0f;

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugBool") debugLogs = value;
            if (entryKey == "debugSpiderWebs") debugWebs = value;
            //Script.Logger.LogMessage($"Bunker Spider web received event. debugBool = {debugLogs}, debugSpiderWebs = {debugWebs}");
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(SandSpiderWebTrap __instance)
        {
            if (!spiderWebs.ContainsKey(__instance))
            {
                spiderWebs.Add(__instance, new SpiderWebValues());
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        static void OnTriggerStayPatch(Collider other, SandSpiderWebTrap __instance)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript) trippedEnemy = trippedEnemyCollision.mainScript;
            if (trippedEnemy == __instance.mainScript) return;

            if (trippedEnemy != null && !spiderWebBlacklist.Contains(trippedEnemy.enemyType.enemyName))
            {
                webData.trappedEnemy = trippedEnemy;
                float SpeedModifier = 1f;


                SpeedModifier = speedModifierDictionary[trippedEnemy.enemyType.enemyName];
                if (!enemyData.ContainsKey(trippedEnemy))
                {
                    enemyData[trippedEnemy] = new EnemyInfo(trippedEnemy, trippedEnemy.agent.speed, trippedEnemy.creatureAnimator.speed);
                }
                if (!enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Add(__instance);
                    if (debugLogs) Script.Logger.LogInfo($"Added instance to NumberOfWebTraps {enemyData[trippedEnemy].NumberOfTraps.Count}");
                }

                if (debugWebs) Script.Logger.LogDebug($"{__instance} Collided with {trippedEnemy}");

                trippedEnemy.agent.speed = (enemyData[trippedEnemy].EnterAgentSpeed / ((1 + enemyData[trippedEnemy].NumberOfTraps.Count) * webStrenght)) * SpeedModifier;
                trippedEnemy.creatureAnimator.speed = (enemyData[trippedEnemy].EnterAnimationSpeed / ((1 + enemyData[trippedEnemy].NumberOfTraps.Count) * webStrenght)) * SpeedModifier;

                if (Script.BoundingConfig.debugSpiderWebs.Value)
                {
                    if (debugCD <= 0)
                    {
                        Script.Logger.LogDebug($"{__instance} Slowed down movement of {trippedEnemy} from {enemyData[trippedEnemy].EnterAgentSpeed} to {trippedEnemy.agent.speed} with Speed modifier {SpeedModifier}");
                        Script.Logger.LogDebug($"{__instance} Slowed down animation of {trippedEnemy} from {enemyData[trippedEnemy].EnterAnimationSpeed} to {trippedEnemy.creatureAnimator.speed} with Speed modifier {SpeedModifier}");
                        debugCD = 0.2f;
                    }
                    else
                    {
                        debugCD -= Time.deltaTime;
                    }
                }
                if (!__instance.webAudio.isPlaying)
                {
                    __instance.webAudio.Play();
                    __instance.webAudio.PlayOneShot(__instance.mainScript.hitWebSFX);
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(SandSpiderWebTrap __instance, out bool __state)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            if (__instance.currentTrappedPlayer != null)
            {
                __state = false;
                return true;
            }
            else if (webData.trappedEnemy != null)
            {
                __state = true;
                return false;
            }
            __state = false;
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(SandSpiderWebTrap __instance, bool __state)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            if (!__state)
            {
                return;
            }

            if (webData.trappedEnemy != null)
            {
                __instance.leftBone.LookAt(webData.trappedEnemy.transform.position);
                __instance.rightBone.LookAt(webData.trappedEnemy.transform.position);
            }
            else
            {
                __instance.leftBone.LookAt(__instance.centerOfWeb);
                __instance.rightBone.LookAt(__instance.centerOfWeb);
            }
            __instance.transform.localScale = Vector3.Lerp(__instance.transform.localScale, new Vector3(1f, 1f, __instance.zScale), 8f * Time.deltaTime);

            if (webData.enemyReference != null && webData.enemyReference != webData.trappedEnemy && enemyData.ContainsKey(webData.enemyReference) && enemyData[webData.enemyReference].NumberOfTraps.Count <= 0)
            {
                if(debugLogs) Script.Logger.LogInfo($"Removing {webData.enemyReference} from NumberOfWebTraps {enemyData[webData.enemyReference].NumberOfTraps.Count}");
                enemyData.Remove(webData.enemyReference);
                webData.enemyReference = null;
                webData.enemyReferenceTimer = 0;
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPrefix]
        static void OnTriggerExitPatch(Collider other, SandSpiderWebTrap __instance)
        {
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript) trippedEnemy = trippedEnemyCollision.mainScript;

            if (trippedEnemy != null && !spiderWebBlacklist.Contains(trippedEnemy.enemyType.enemyName))
            {
                if (enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Remove(__instance);
                    if (debugLogs) Script.Logger.LogInfo($"Removed instance to NumberOfWebTraps {enemyData[trippedEnemy].NumberOfTraps.Count}");
                }
                if (debugLogs && debugWebs) Script.Logger.LogInfo($"Removing {trippedEnemy}");
                //trippedEnemy.agent.speed = enemyData[trippedEnemy].EnterAgentSpeed;

                trippedEnemy.creatureAnimator.speed = enemyData[trippedEnemy].EnterAnimationSpeed;
                webData.enemyReference = trippedEnemy;
                webData.trappedEnemy = null;
                __instance.webAudio.Stop();
            }
        }
    }
}
