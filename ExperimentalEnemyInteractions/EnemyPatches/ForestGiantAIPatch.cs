using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using LethalNetworkAPI;
using Unity.Burst.CompilerServices;
using LethalNetworkAPI.Utils;
using NaturalSelection.Generics;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{
    class GiantData()
    {
        internal int extinguished = 0;
        internal bool setFireOnKill = false;
    }




    [HarmonyPatch(typeof(ForestGiantAI))]
    class ForestGiantPatch
    {
        static Dictionary<ForestGiantAI, GiantData> giantDictionary = [];
        static bool logGiant = Script.Bools["debugGiants"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static LNetworkEvent NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSSetGiantOnFire" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        internal static LNetworkEvent NetworkExtinguish(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSExtinguish" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        static LNetworkVariable<float> NetworkOwnerPostfixResult(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSOwnerrealtimeSinceStartup" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<float>(NWID);
        }

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugGiants") logGiant = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            //Script.Logger.Log(LogLevel.Message,$"Forest Keeper received event. logGiant = {logGiant}, debugSpam = {debugSpam}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void startPostfix(ForestGiantAI __instance)
        {
            if (!giantDictionary.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                giantDictionary.Add(__instance, new GiantData());
            }
            GiantData data =giantDictionary[__instance];
            NetworkSetGiantOnFire(__instance).OnServerReceived += UpdateSetGiantOnFireServer;
            //NetworkSetGiantOnFire(__instance).OnClientReceived += UpdateSetGiantOnFire;

            void UpdateSetGiantOnFireServer(ulong client)
            {
                //NetworkSetGiantOnFire(__instance).InvokeClients();
                if (__instance.IsOwner)
                {
                    __instance.timeAtStartOfBurning = Time.realtimeSinceStartup;
                    __instance.SwitchToBehaviourState(2);
                    Script.Logger.Log(LogLevel.Info,"Received UpdateSetGiantOnFire event");
                }
            }

            NetworkExtinguish(__instance).OnClientReceived += ExtuinguishGiant;

            void ExtuinguishGiant()
            {
                __instance.SwitchToBehaviourState(0);
                __instance.burningParticlesContainer.SetActive(false);
                __instance.giantBurningAudio.Stop();
                __instance.creatureAnimator.SetBool("burning", false);
                data.extinguished = 1;
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("KillEnemy")]
        [HarmonyPostfix]
        static void KillEnemyPatchPostfix(ForestGiantAI __instance)
        {
            CheckDataIntegrityGiant(__instance);
            GiantData giantDaata = giantDictionary[__instance];
            if (giantDaata.extinguished != 1 && __instance.currentBehaviourStateIndex == 2)
            {
                __instance.burningParticlesContainer.SetActive(true);
            }
            if (logGiant) Script.Logger.Log(LogLevel.Info,LibraryCalls.DebugStringHead(__instance));
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(ForestGiantAI __instance)
        {
            CheckDataIntegrityGiant(__instance);
            GiantData giantData = giantDictionary[__instance];

            if (__instance.currentBehaviourStateIndex == 2 && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning > 9.5f && __instance.enemyHP > 20 && giantData.extinguished == 0  && !__instance.isEnemyDead && __instance.IsOwner)
            {
                int randomNumber = Random.Range(0, 100);

                if (randomNumber <= Script.BoundingConfig.giantExtinguishChance.Value)
                {
                    NetworkExtinguish(__instance).InvokeClients();
                    Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} successfully extinguished itself. Skipping Update. Rolled {randomNumber}");
                }
                else
                {
                    Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} failed to extinguish itself. rolled {randomNumber}");
                    giantData.extinguished = 2;
                }
            }

            if (giantData.extinguished == 1)
            {
                __instance.enemyHP -= 20;
                giantData.extinguished = 2;
                return false;
            }
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(ForestGiantAI __instance)
        {
            CheckDataIntegrityGiant(__instance);
            GiantData giantData = giantDictionary[__instance];
            if (__instance.IsOwner) NetworkOwnerPostfixResult(__instance).Value = Time.realtimeSinceStartup - __instance.timeAtStartOfBurning;

            if (__instance.isEnemyDead && __instance.currentBehaviourStateIndex == 2 && NetworkOwnerPostfixResult(__instance).Value < 20f)
            {
                if (__instance.IsOwner) NetworkOwnerPostfixResult(__instance).Value = Time.realtimeSinceStartup - __instance.timeAtStartOfBurning;
                if (!__instance.giantBurningAudio.isPlaying)
                {
                    __instance.giantBurningAudio.Play();
                }
                __instance.giantBurningAudio.volume = Mathf.Min(__instance.giantBurningAudio.volume + Time.deltaTime * 0.5f, 1f);
            }
            else if (__instance.isEnemyDead && NetworkOwnerPostfixResult(__instance).Value > 26f && __instance.burningParticlesContainer.activeSelf == true)
            {     
                __instance.burningParticlesContainer.SetActive(false);
            }
        }

        public static void CheckDataIntegrityGiant(ForestGiantAI __instance)
        {
            if (!giantDictionary.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                giantDictionary.Add(__instance, new GiantData());
            }
        }
        /*
        public static void RollToExtinguish(ForestGiantAI __instance)
        {
            GiantData giantData = giantDictionary[__instance];
            if (__instance.enemyHP > 20 && giantData.extinguished == 0  && !__instance.isEnemyDead && __instance.IsOwner)
            {
                int randomNumber = Random.Range(0, 100);

                if (randomNumber <= Script.BoundingConfig.giantExtinguishChance.Value)
                {
                    NetworkExtinguish(__instance).InvokeClients();
                    Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} successfully extinguished itself. Skipping Update. Rolled {randomNumber}");
                }
                else
                {
                    Script.Logger.Log(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} failed to extinguish itself. rolled {randomNumber}");
                    giantData.extinguished = 2;
                }
            }
        }*/
    }
} 