using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using LethalNetworkAPI;
using Unity.Burst.CompilerServices;
using LethalNetworkAPI.Utils;

namespace NaturalSelection.EnemyPatches
{
    class GiantData
    {
        public bool logGiant = Script.BoundingConfig.debugGiants.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        public int extinguished = 0;
        public bool setFireOnKill = false;
    }

    [HarmonyPatch(typeof(ForestGiantAI))]
    class ForestGiantPatch
    {
        static Dictionary<ForestGiantAI, GiantData> giantDictionary = [];

        static LNetworkMessage<bool> NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            return LNetworkMessage<bool>.Connect("NSSetGiantOnFire" + forestGiantAI.NetworkObjectId);
        }

        static LNetworkMessage<bool> NetworkExtinguish(ForestGiantAI forestGiantAI)
        {
            return LNetworkMessage<bool>.Connect("NSExtinguish" + forestGiantAI.NetworkObjectId);
        }

        static LNetworkVariable<float> NetworkOwnerPostfixResult(ForestGiantAI forestGiantAI)
        {
            return LNetworkVariable<float>.Connect("NSOwnerrealtimeSinceStartup" + forestGiantAI.NetworkObjectId);
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void startPostfix(ForestGiantAI __instance)
        {
            if (!giantDictionary.ContainsKey(__instance))
            {
                giantDictionary.Add(__instance, new GiantData());
            }
        }

        [HarmonyPatch("KillEnemy")]
        [HarmonyPostfix]
        static void KillEnemyPatchPostfix(ForestGiantAI __instance)
        {
            GiantData giantDaata = giantDictionary[__instance];
            if (giantDaata.extinguished != 1 && __instance.currentBehaviourStateIndex == 2)
            {
                __instance.burningParticlesContainer.SetActive(true);
            }
            if (giantDaata.logGiant) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance));
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(ForestGiantAI __instance)
        {
            GiantData giantData = giantDictionary[__instance];
            bool receivedEvent = false;

            NetworkSetGiantOnFire(__instance).OnServerReceived += UpdateSetGiantOnFireServer;
            //NetworkSetGiantOnFire(__instance).OnClientReceived += UpdateSetGiantOnFire;

            void UpdateSetGiantOnFireServer(bool i, ulong client)
            {
                //NetworkSetGiantOnFire(__instance).InvokeClients();
                if (__instance.IsOwner && i)
                {
                    __instance.timeAtStartOfBurning = Time.realtimeSinceStartup;
                    __instance.SwitchToBehaviourState(2);
                    receivedEvent = true;
                }
            }

           /* void UpdateSetGiantOnFire()
            {
                if (__instance.IsOwner)
                {
                    __instance.timeAtStartOfBurning = Time.realtimeSinceStartup;
                    __instance.SwitchToBehaviourState(2);
                    receivedEvent = true;
                }
            }*/

            if (receivedEvent)
            {
                Script.Logger.LogInfo("Received UpdateSetGiantOnFire event");
            }

            if (__instance.currentBehaviourStateIndex == 2 && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning > 9.5f && __instance.enemyHP > 20 && giantData.extinguished == 0  && !__instance.isEnemyDead&& __instance.IsOwner)
            {
                int randomNumber = Random.Range(0, 100);

                if (randomNumber <= Script.BoundingConfig.giantExtinguishChance.Value)
                {
                    NetworkExtinguish(__instance).SendClients(true);
                    if (giantData.logGiant) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + " successfully extinguished itself. Skipping Update. Rolled " + randomNumber);
                }
                else
                {
                    if (giantData.logGiant) Script.Logger.LogInfo(EnemyAIPatch.DebugStringHead(__instance) + " failed to extinguish itself. rolled " + randomNumber);
                    giantData.extinguished = 2;
                }
            }

            NetworkExtinguish(__instance).OnClientReceived += ExtuinguishGiant;

            void ExtuinguishGiant(bool i)
            {
                if (i)
                {
                    __instance.SwitchToBehaviourState(0);
                    __instance.burningParticlesContainer.SetActive(false);
                    __instance.giantBurningAudio.Stop();
                    __instance.creatureAnimator.SetBool("burning", false);
                    giantData.extinguished = 1;
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
    }
} 