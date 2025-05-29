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

        [HarmonyPatch(typeof(QuickMenuManager) ,"Start")]
        [HarmonyPrefix]
        [HarmonyBefore("XuuXiaolan.ReXuvination")]
        static void QuickMenuManagerStartPatch()
        {
            if (patched) return;

            foreach (EnemyAI enemy in Script.loadedEnemyList)
            {
                foreach (EnemyAICollisionDetect collisionDetectScript in enemy.enemyType.enemyPrefab.GetComponentsInChildren<EnemyAICollisionDetect>())
                {
                    collisionDetectScript.gameObject.TryGetComponent<Collider>(out Collider coltemp);

                    if (coltemp != null && !coltemp.isTrigger)
                    {
                        collisionDetectScript.canCollideWithEnemies = true;
                        Script.Logger.Log(LogLevel.Info, $"Unoptimized {enemy.enemyType.enemyName} collider {coltemp.name} to collide with enemies.");
                    }

                    foreach (Collider col in collisionDetectScript.gameObject.GetComponentsInChildren<Collider>())
                    {
                        if (col.isTrigger) continue;

                        if (collisionDetectScript.canCollideWithEnemies) continue;

                        collisionDetectScript.canCollideWithEnemies = true;
                        Script.Logger.Log(LogLevel.Info, $"Unoptimized {enemy.enemyType.enemyName} collider {col.name} to collide with enemies.");
                    }
                }
            }
            patched = true;
        }
    }
}