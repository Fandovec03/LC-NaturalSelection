using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnhancedMonsters;
using HarmonyLib;
using NaturalSelection.EnemyPatches;
using Unity.Netcode;
using UnityEngine;
using System.Reflection.Emit;
using BepInEx.Logging;

namespace NaturalSelection.Experimental
{
    public class EnhancedMonstersPatch()
    {

        public static List<GameObject> deadEnemiesList = new List<GameObject>();

        [HarmonyPatch(typeof(EnhancedMonsters.Patches.EnemyAI_Patches), "KillEnemy")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EnhancedMonstersKillEnemyPatchT(IEnumerable<CodeInstruction> instructions)
        {
            Script.Logger.LogWarning("Fired Transpiller for EnemyAI.KillEnemyOnOwnerClient");
            CodeMatcher matcher = new CodeMatcher(instructions);

            Script.Logger.LogInfo(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Script), nameof(Script.Logger))));
            Script.Logger.LogInfo(new CodeInstruction(OpCodes.Ldstr, "Successfully Transpiled EnhancedMonster by NaturalSelection"));
            Script.Logger.LogInfo(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ManualLogSource), nameof(ManualLogSource.LogMessage), [typeof(string)])));

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkObject), nameof(NetworkObject.Spawn)))
            )
            .ThrowIfInvalid("Could not find match for EnhancedMonstersKillEnemyPatchT")
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnhancedMonsters.Plugin), nameof(EnhancedMonsters.Plugin.logger))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldstr, "Successfully Transpiled EnhancedMonster by NaturalSelection"))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ManualLogSource), nameof(ManualLogSource.LogMessage), [typeof(string)])))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnhancedMonstersPatch), nameof(EnhancedMonstersPatch.deadEnemiesList))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 8))
            .Insert(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<GameObject>), nameof(List<GameObject>.Add), [typeof(GameObject)])))
            ;

            int offset = 0;




            for (int i = 0; i < instructions.ToList().Count; i++)
            {
                //if (!debug) break;
                try
                {
                    if (matcher.Instructions().ToList()[i].ToString() != instructions.ToList()[i - offset].ToString())
                    {
                        Script.Logger.LogError($"{matcher.Instructions().ToList()[i]} : {instructions.ToList()[i - offset]}");

                        if (matcher.Instructions().ToList()[i].ToString() != instructions.ToList()[i - offset].ToString())
                        {
                            offset++;
                        }
                    }
                    else Script.Logger.LogInfo(instructions.ToList()[i]);
                }
                catch
                {
                    Script.Logger.LogError("Failed to read instructions");
                }
            }

            return matcher.Instructions();
        }
    }
}
