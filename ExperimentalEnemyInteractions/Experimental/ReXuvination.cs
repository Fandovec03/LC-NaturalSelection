using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Reflection.Emit;
using NaturalSelection.EnemyPatches;

namespace NaturalSelection.Compatibility
{
    public class ReXuvinationPatch()
    {
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

        [HarmonyPatch(typeof(QuickMenuManager) ,"Start")]
        [HarmonyPrefix]
        static void QuickMenuManagerStartPatch()
        {
            foreach(EnemyAI enemy in Script.loadedEnemyList)
            {
                foreach (EnemyAICollisionDetect collisionDetectScript in enemy.enemyType.enemyPrefab.GetComponentsInChildren<EnemyAICollisionDetect>())
                {
                    foreach (Collider col in collisionDetectScript.gameObject.GetComponentsInChildren<Collider>())
                    {
                        if (col.isTrigger) continue;

                        if (collisionDetectScript.canCollideWithEnemies) continue;

                        collisionDetectScript.canCollideWithEnemies = true;
                    }
                }
            }
        }
    }
}