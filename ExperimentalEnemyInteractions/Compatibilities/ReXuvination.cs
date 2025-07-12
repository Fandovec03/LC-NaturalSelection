using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Reflection.Emit;
using ReXuvination.src.Patches;
using SandSpiderWebTrapPatch = NaturalSelection.EnemyPatches.SandSpiderWebTrapPatch;
using System.Linq;

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
                    Script.LogNS(LogLevel.Message,$"awake found  {collider.excludeLayers.value}");
                    Script.LogNS(LogLevel.Message,$"awake expected {~(StartOfRound.Instance.playersMask ^ LayerMask.GetMask("Enemies"))}");
                    collider.excludeLayers = ~(StartOfRound.Instance.playersMask ^ LayerMask.GetMask("Enemies"));
                    patched++;
                }
                if (patched > 0)
                {
                    SandSpiderWebTrapPatch.spiderWebs[__instance].patchedCollisionLayer = true;
                }
            }
        }

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
        }
    }
}