using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Reflection.Emit;
using ReXuvination.src.Patches;
using SandSpiderWebTrapPatch = NaturalSelection.EnemyPatches.SandSpiderWebTrapPatch;
using System.Linq;
using ReXuvination;
using NaturalSelection.EnemyPatches;

namespace NaturalSelection.Compatibility
{
    public class ReXuvinationPatch()
    {
        public static bool patchedQuickMenu = false;

        [HarmonyPatch(typeof(SandSpiderWebTrap) ,"Update")]
        [HarmonyPrefix]
        static void UpdatePrefix(SandSpiderWebTrap __instance)
        {
            SpiderWebValues webData = (SpiderWebValues)EnemyAIPatch.GetEnemyData(__instance, new SpiderWebValues());
            if (Script.rexuvinationPresent && !webData.patchedCollisionLayer)
            {
                Collider[] colliders = __instance.gameObject.GetComponents<Collider>();
                int patched = 0;
                foreach (Collider collider in colliders)
                {
                    if (!collider.isTrigger) continue;

                    /*Script.LogNS(LogLevel.Message,$"awake found  {collider.excludeLayers.value}");
                    Script.LogNS(LogLevel.Message,$"awake expected {~(StartOfRound.Instance.playersMask ^ LayerMask.GetMask("Enemies"))}");*/

                    collider.includeLayers |= LayerMask.GetMask("Enemies");
                    patched++;
                }
                if (patched > 0)
                {
                    webData.patchedCollisionLayer = true;
                }
            }
        }

        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        [HarmonyPostfix]
        [HarmonyAfter("XuuXiaolan.ReXuvination")]
        static void QuickMenuManagerPostfix(SandSpiderWebTrap __instance)
        {
            if (!patchedQuickMenu)
            {
                foreach (EnemyType enemy in Resources.FindObjectsOfTypeAll<EnemyType>())
                {
                    if (string.IsNullOrEmpty(enemy.enemyName)) continue;
                    if (enemy.enemyPrefab == null) continue;
                    if (ReXuvination.src.ReXuvination.PluginConfig.ConfigEnemyBlacklist.Value.Contains(enemy.enemyName)) continue;

                    foreach (var collision in enemy.enemyPrefab.GetComponentsInChildren<EnemyAICollisionDetect>())
                    {
                        foreach (var collider in collision.gameObject.GetComponentsInChildren<Collider>())
                        {
                            if (!collider.isTrigger) continue;

                            collider.includeLayers |= LayerMask.GetMask("Enemies");
                        }
                    }

                }
                patchedQuickMenu = true;
            }
        }
        /*
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(QuickMenuManagerPatch) ,"QuickMenuManagerStartPatch")]
        public static IEnumerable<CodeInstruction> QuickMenuManagerStartPatch(IEnumerable<CodeInstruction> instructions)
        {

            CodeMatcher matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Stloc_2),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Stloc_3)
                )
                .ThrowIfInvalid("Failed to get a match.")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                .Insert(new CodeInstruction(OpCodes.Stloc_1));

            Script.LogNS(LogLevel.Message,"Patched ReXuvination");

            return matcher.Instructions();
        }*/
    }
}