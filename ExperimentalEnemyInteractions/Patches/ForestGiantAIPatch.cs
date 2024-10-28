using HarmonyLib;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace ExperimentalEnemyInteractions.Patches
{

    [HarmonyPatch(typeof(ForestGiantAI))]
    class ForestGiantPatch
    {

        [HarmonyPatch("KillEnemy")]
        [HarmonyPrefix] 
        static void KillEnemyPatchPrefix(ForestGiantAI __instance, out bool __state)
        {
            if (__instance.burningParticlesContainer.activeSelf == true)
            {
                __state = true;
            }
            else
            {
                __state = false;
            }
            Script.Logger.LogInfo("State status: " + __state);
        }

        [HarmonyPatch("KillEnemy")]
        [HarmonyPostfix]
        static void KillEnemyPatchPostfix(ForestGiantAI __instance, bool __state)
        {
            if (__state == true)
            {
                __instance.burningParticlesContainer.SetActive(true);
            }
            else
            {
                __instance.burningParticlesContainer.SetActive(false);
            }
            Script.Logger.LogInfo("State status2: " + __state);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(ForestGiantAI __instance)
        {
            if (__instance.isEnemyDead && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning < 20f)
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