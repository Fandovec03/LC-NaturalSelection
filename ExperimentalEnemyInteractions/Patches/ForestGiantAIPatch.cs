using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace ExperimentalEnemyInteractions.Patches
{
    class GiantData
    {
        public bool logGiant = Script.BoundingConfig.debugGiants.Value;
        public bool? extinguish = null;
    }

    [HarmonyPatch(typeof(ForestGiantAI))]
    class ForestGiantPatch
    {
        static Dictionary<ForestGiantAI, GiantData> giantDictionary = [];

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void startPostfix(ForestGiantAI __instance)
        {
            {
                if (!giantDictionary.ContainsKey(__instance))
                {
                    giantDictionary.Add(__instance, new GiantData());
                }
            }


            [HarmonyPatch("KillEnemy")]
            [HarmonyPrefix]
            static void KillEnemyPatchPrefix(ForestGiantAI __instance, out bool __state)
            {
                GiantData giantDaata = giantDictionary[__instance];
                if (__instance.burningParticlesContainer.activeSelf == true)
                {
                    __state = true;
                }
                else
                {
                    __state = false;
                }
                if (giantDaata.logGiant) Script.Logger.LogInfo("Giant state status: " + __state);
            }

            [HarmonyPatch("KillEnemy")]
            [HarmonyPostfix]
            static void KillEnemyPatchPostfix(ForestGiantAI __instance, bool __state)
            {
                GiantData giantDaata = giantDictionary[__instance];
                if (__state == true)
                {
                    __instance.burningParticlesContainer.SetActive(true);
                }
                else
                {
                    __instance.burningParticlesContainer.SetActive(false);
                }
                if (giantDaata.logGiant) Script.Logger.LogInfo("Giant state status2: " + __state);
            }

            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static bool UpdatePrefix(ForestGiantAI __instance)
            {
                GiantData giantData = giantDictionary[__instance];
                if (__instance.currentBehaviourStateIndex == 2 && __instance.IsOwner && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning > 9.5f && __instance.enemyHP > 20 && giantData.extinguish == null)
                {
                    int extinguishRandom = Random.Range(0, 100);

                    if (extinguishRandom <= 33)
                    {
                        __instance.enemyHP -= 20;
                        __instance.burningParticlesContainer.SetActive(false);
                        __instance.giantBurningAudio.Stop();
                        __instance.creatureAnimator.SetBool("burning", false);
                        __instance.SwitchToBehaviourState(0);
                        giantData.extinguish = true;
                        if (giantData.logGiant) Script.Logger.LogInfo("Giant successfully extinguished itself. Skipping Update. Rolled " + extinguishRandom);
                        return false;
                    }
                    else
                    {
                        giantData.extinguish = false;
                        if (giantData.logGiant) Script.Logger.LogInfo("Giant failed to extinguish itself. rolled " + extinguishRandom);
                    }
                }
                return true;
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void UpdatePostfix(ForestGiantAI __instance)
            {
                if (__instance.isEnemyDead && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning < 20f && __instance.currentBehaviourStateIndex == 2)
                {
                    if (!__instance.giantBurningAudio.isPlaying)
                    {
                        __instance.giantBurningAudio.Play();
                    }
                    __instance.giantBurningAudio.volume = Mathf.Min(__instance.giantBurningAudio.volume + Time.deltaTime * 0.5f, 1f);
                }
                else if (__instance.isEnemyDead && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning > 26f)
                {
                    __instance.burningParticlesContainer.SetActive(false);
                }
            }
        }
    } 
}