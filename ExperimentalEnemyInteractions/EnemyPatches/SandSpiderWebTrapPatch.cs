﻿using HarmonyLib;
using LethalNetworkAPI;
﻿using BepInEx.Logging;
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
        internal bool patchedCollisionLayer = false;
        //internal bool playSound = false;
    }

    [HarmonyPatch(typeof(SandSpiderWebTrap))]
    class SandSpiderWebTrapPatch
    {
        /*static LNetworkEvent NetworkEnemyTripTrapEnter(SandSpiderWebTrap instance)
        {
            string NWID = "NSSpiderTripTrapEnter" + instance.trapID;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        static LNetworkEvent NetworkEnemyTripTrapExit(SandSpiderWebTrap instance)
        {
            string NWID = "NSSpiderTripTrapExit" + instance.trapID;
            return Networking.NSEnemyNetworkEvent(NWID);
        }*/

        class EnemyInfo(EnemyAI enemy, float enterAgentSpeed, float enterAnimationSpeed)
        {
            internal EnemyAI EnemyAI { get; set; } = enemy;
            internal float EnterAgentSpeed { get; set; } = enterAgentSpeed;
            internal float EnterAnimationSpeed { get; set; } = enterAnimationSpeed;
            internal List<SandSpiderWebTrap> NumberOfTraps { get; set; } = new List<SandSpiderWebTrap>();
        }

        internal static Dictionary<SandSpiderWebTrap, SpiderWebValues> spiderWebs = new Dictionary<SandSpiderWebTrap, SpiderWebValues>();
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
            //Script.Logger.Log(LogLevel.Message,$"Bunker Spider web received event. debugBool = {debugLogs}, debugSpiderWebs = {debugWebs}");
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(SandSpiderWebTrap __instance)
        {
            if (!spiderWebs.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for web {__instance.trapID}");
                spiderWebs.Add(__instance, new SpiderWebValues());
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        static void OnTriggerStayPatch(Collider other, SandSpiderWebTrap __instance)
        {
            CheckDataIntegrityWeb(__instance);
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript) trippedEnemy = trippedEnemyCollision.mainScript;
            if (trippedEnemy == __instance.mainScript || trippedEnemy != null && trippedEnemy.isEnemyDead) return;

            if (trippedEnemy != null && !spiderWebBlacklist.Contains(trippedEnemy.enemyType.enemyName))
            {
                webData.trappedEnemy = trippedEnemy;
                float SpeedModifier = 1f;


                SpeedModifier = speedModifierDictionary[trippedEnemy.enemyType.enemyName];
                if (!enemyData.ContainsKey(trippedEnemy))
                {
                    float creatureAnimatorSpeed = 1f;
                    if (trippedEnemy.creatureAnimator != null)
                    {
                        creatureAnimatorSpeed = trippedEnemy.creatureAnimator.speed;
                    }
                    enemyData.Add(trippedEnemy, new EnemyInfo(trippedEnemy, trippedEnemy.agent.speed, creatureAnimatorSpeed));
                }
                if (!enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Add(__instance);
                    SandSpiderAIPatch.AlertSpider(__instance.mainScript, __instance);
                    if (debugLogs) Script.Logger.Log(LogLevel.Info,$"Added instance to NumberOfWebTraps {enemyData[trippedEnemy].NumberOfTraps.Count}");
                }

                if (debugWebs) Script.Logger.Log(LogLevel.Debug,$"{__instance} Collided with {trippedEnemy}");

                float test = (enemyData[trippedEnemy].EnterAgentSpeed / ((1 + enemyData[trippedEnemy].NumberOfTraps.Count) * webStrenght)) * SpeedModifier;

                trippedEnemy.agent.speed = test;

                if (trippedEnemy.agent.velocity.magnitude / trippedEnemy.agent.speed > 1)
                {
                    if (debugLogs) Script.Logger.LogInfo($"Agent velocity: {trippedEnemy.agent.velocity.magnitude}, Agent speed: {trippedEnemy.agent.speed} || {trippedEnemy.agent.velocity.magnitude / test}");
                    trippedEnemy.agent.velocity /= trippedEnemy.agent.velocity.magnitude / test;
                }

                if (trippedEnemy.creatureAnimator != null) trippedEnemy.creatureAnimator.speed = (enemyData[trippedEnemy].EnterAnimationSpeed / ((1 + enemyData[trippedEnemy].NumberOfTraps.Count) * webStrenght)) * SpeedModifier;

                if (Script.BoundingConfig.debugSpiderWebs.Value)
                {
                    if (debugCD <= 0)
                    {
                        Script.Logger.Log(LogLevel.Debug,$"{__instance} Slowed down movement of {trippedEnemy} from {enemyData[trippedEnemy].EnterAgentSpeed} to {trippedEnemy.agent.speed} with Speed modifier {SpeedModifier}");

                        if (trippedEnemy.creatureAnimator != null) Script.Logger.Log(LogLevel.Debug,$"{__instance} Slowed down animation of {trippedEnemy} from {enemyData[trippedEnemy].EnterAnimationSpeed} to {trippedEnemy.creatureAnimator.speed} with Speed modifier {SpeedModifier}");
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
            CheckDataIntegrityWeb(__instance);
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
            CheckDataIntegrityWeb(__instance);
            SpiderWebValues webData = spiderWebs[__instance];
            if (!__state)
            {
                return;
            }

            if (webData.trappedEnemy != null && !webData.trappedEnemy.isEnemyDead)
            {
                __instance.leftBone.LookAt(webData.trappedEnemy.transform.position);
                __instance.rightBone.LookAt(webData.trappedEnemy.transform.position);
            }
            else
            {
                __instance.leftBone.LookAt(__instance.centerOfWeb);
                __instance.rightBone.LookAt(__instance.centerOfWeb);

                if (__instance.webAudio.isPlaying)
                {
                    __instance.webAudio.Stop();
                }

            }
            __instance.transform.localScale = Vector3.Lerp(__instance.transform.localScale, new Vector3(1f, 1f, __instance.zScale), 8f * Time.deltaTime);

            if (webData.enemyReference != null && webData.enemyReference != webData.trappedEnemy && enemyData.ContainsKey(webData.enemyReference) && enemyData[webData.enemyReference].NumberOfTraps.Count <= 0)
            {
                if(debugLogs) Script.Logger.Log(LogLevel.Info,$"Removing {webData.enemyReference} from NumberOfWebTraps {enemyData[webData.enemyReference].NumberOfTraps.Count}");
                enemyData.Remove(webData.enemyReference);
                webData.enemyReference = null;
                webData.enemyReferenceTimer = 0;
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPrefix]
        static void OnTriggerExitPatch(Collider other, SandSpiderWebTrap __instance)
        {
            CheckDataIntegrityWeb(__instance);
            SpiderWebValues webData = spiderWebs[__instance];
            EnemyAICollisionDetect? trippedEnemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            EnemyAI? trippedEnemy = null;
            if (trippedEnemyCollision != null && trippedEnemyCollision.mainScript != __instance.mainScript && !trippedEnemyCollision.mainScript.isEnemyDead) trippedEnemy = trippedEnemyCollision.mainScript;

            if (trippedEnemy != null && !spiderWebBlacklist.Contains(trippedEnemy.enemyType.enemyName))
            {
                if (enemyData[trippedEnemy].NumberOfTraps.Contains(__instance))
                {
                    enemyData[trippedEnemy].NumberOfTraps.Remove(__instance);
                    if (debugLogs) Script.Logger.Log(LogLevel.Info,$"Removed instance to NumberOfWebTraps {enemyData[trippedEnemy].NumberOfTraps.Count}");
                }
                if (debugLogs && debugWebs) Script.Logger.Log(LogLevel.Info,$"Removing {trippedEnemy}");
                //trippedEnemy.agent.speed = enemyData[trippedEnemy].EnterAgentSpeed;

                if (trippedEnemy.creatureAnimator != null) trippedEnemy.creatureAnimator.speed = enemyData[trippedEnemy].EnterAnimationSpeed;
                webData.enemyReference = trippedEnemy;
                webData.trappedEnemy = null;

                if (__instance.webAudio.isPlaying)
                {
                    __instance.webAudio.Stop();
                    //NetworkEnemyTripTrapExit(__instance).InvokeClients();
                }
                //__instance.webAudio.Stop();
            }
        }

        public static void CheckDataIntegrityWeb(SandSpiderWebTrap __instance)
        {
            if (!spiderWebs.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for trap {__instance.trapID}. Attempting to fix...");
                spiderWebs.Add(__instance, new SpiderWebValues());
            }
        }
    }
}
