using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Reflection.Emit;
using NaturalSelection.EnemyPatches;
using ReXuvination.src.Patches;
using SandSpiderWebTrapPatch = NaturalSelection.EnemyPatches.SandSpiderWebTrapPatch;

namespace NaturalSelection.Compatibility
{
    public class ReXuvinationPatch()
    {
        public static bool patched = false;

        [HarmonyPatch(typeof(SandSpiderWebTrap) ,"Awake")]
        [HarmonyPostfix]
        [HarmonyAfter("XuuXiaolan.ReXuvination")]
        static void AwakePostfix(SandSpiderWebTrap __instance)
        {
            if (Script.rexuvinationPresent && !SandSpiderWebTrapPatch.spiderWebs[__instance].patchedCollisionLayer)
            {
                Collider[] colliders = __instance.gameObject.GetComponents<Collider>();
                int patched = 0;
                foreach (Collider collider in colliders)
                {
                    if (!collider.isTrigger) continue;
                    Script.Logger.Log(LogLevel.Message,$"awake found  {collider.excludeLayers.value}");
                    Script.Logger.Log(LogLevel.Message,$"awake expected {~(StartOfRound.Instance.playersMask ^ LayerMask.GetMask("Enemies"))}");
                    collider.excludeLayers = ~(StartOfRound.Instance.playersMask ^ LayerMask.GetMask("Enemies"));
                    patched++;
                }
                if (patched > 0)
                {
                    SandSpiderWebTrapPatch.spiderWebs[__instance].patchedCollisionLayer = true;
                }
            }
        }

        [HarmonyPatch(typeof(QuickMenuManagerPatch) ,"QuickMenuManagerStartPatch")]
        [HarmonyPrefix]
        static void QuickMenuManagerStartPatch()
        {
            QuickMenuManagerPatch.alreadyPatched = true;
            Script.Logger.LogMessage("Prevented Rexuvination from patching enemy colliders");
        }
    }
}