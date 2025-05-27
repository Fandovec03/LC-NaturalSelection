using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using Unity.Netcode;
using UnityEngine.UIElements;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{

    class EnemyData()
    {
        internal float originalAgentRadius = 0f;
        internal Dictionary<Type, int> targetedByEnemies = new Dictionary<Type, int>();
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.Bools["debugUnspecified"];
        static bool debugTriggerFlags = Script.Bools["debugTriggerFlags"];
        static Dictionary<EnemyAI, EnemyData> enemyData = [];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)

        {
            if (entryKey == "debugUnspecified") debugUnspecified = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlags = value;
            //Script.Logger.Log(LogLevel.Message,$"EnemyAI received event. debugUnspecified = {debugUnspecified}, debugTriggerFlags = {debugTriggerFlags}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            if (!enemyData.ContainsKey(__instance))
            {
                Script.Logger.Log(BepInEx.Logging.LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                enemyData.Add(__instance, new EnemyData());
            }
            EnemyData data = enemyData[__instance];
            data.originalAgentRadius = __instance.agent.radius;
            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            if (debugUnspecified && debugTriggerFlags) Script.Logger.Log(BepInEx.Logging.LogLevel.Message,$"Modified agent radius. Original: {enemyData[__instance].originalAgentRadius}, Modified: {__instance.agent.radius}");
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        static public int ReactToHit(int force = 0, EnemyAI? enemyAI = null, PlayerControllerB? player = null)
        {
            if (force > 0)
            {
                return 1;
            }
            if (force > 1)
            {
                return 2;
            }
            return 0;
        }

        [HarmonyPatch("HitEnemy")]
        [HarmonyPostfix]
        public static void HitEnemyPatch(EnemyAI __instance, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            string playerString = "unknown";
            if (playerWhoHit != null)
            {
                playerString = $"{playerWhoHit.playerUsername}(SteamID: {playerWhoHit.playerSteamId}, playerClientID: {playerWhoHit.playerClientId})";
            }
            if (debugTriggerFlags) Script.Logger.Log(BepInEx.Logging.LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.");
        }
        /*
        static bool CheckEnemyTypeForOverride(EnemyAI enemy)
        {
            if (enemy is ForestGiantAI)
            {
                ForestGiantAI giant = (ForestGiantAI)enemy;
                if (giant.currentBehaviourStateIndex == 2 && giant.burningParticlesContainer.activeSelf == true)
                {
                    ForestGiantPatch.RollToExtinguish(giant);
                    return true;
                }
            }
            return false;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyAI),nameof(EnemyAI.KillEnemyOnOwnerClient))]
        static IEnumerable<CodeInstruction> KillEnemyOnOwnerCLientT(IEnumerable<CodeInstruction> instructions)
        {
            Script.Logger.Log(LogLevel.Warning,"Fired Transpiller for EnemyAI.KillEnemyOnOwnerClient");
            CodeMatcher matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ret)
            )
            .ThrowIfInvalid("Could not find match for EnemyAI.KillEnemyOnOwnerClient")
            .Advance(4)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EnemyAIPatch), nameof(EnemyAIPatch.CheckEnemyTypeForOverride), [typeof(EnemyAI)])))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue, instructions.ToList()[4].labels.First()))
            .Insert(new CodeInstruction(OpCodes.Ret))
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
                    else Script.Logger.Log(LogLevel.Info,instructions.ToList()[i]);
                }
                catch
                {
                    Script.Logger.LogError("Failed to read instructions");
                }
            }

            return matcher.Instructions();
        }*/

    }

    public class ReversePatchAI
    {
        public static Action<EnemyAI> originalUpdate;

        static ReversePatchAI()
        {
            var method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Update));
            var dm = new DynamicMethod("Base.Update",null, [typeof(EnemyAI)], typeof(EnemyAI));
            var gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, method);
            gen.Emit(OpCodes.Ret);

            originalUpdate = (Action<EnemyAI>)dm.CreateDelegate(typeof(Action<EnemyAI>));
        }
    }
}
