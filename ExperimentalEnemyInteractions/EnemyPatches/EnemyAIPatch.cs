using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;

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

        static void Event_OnConfigSettingChanged(string boolName, bool newValue)

        {
            if (boolName == "debugUnspecified")
            {
                debugUnspecified = newValue;
            }
            if (boolName == "debugTriggerFlags")
            {
                debugTriggerFlags = newValue;
            }
            Script.Logger.LogMessage($"Successfully invoked event. boolName = {boolName}, newValue = {newValue}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {

            if (!enemyData.ContainsKey(__instance)) enemyData.Add(__instance, new EnemyData());
            EnemyData data = enemyData[__instance];
            data.originalAgentRadius = __instance.agent.radius;
            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            if (debugUnspecified && debugTriggerFlags) Script.Logger.LogMessage($"Modified agent radius. Original: {enemyData[__instance].originalAgentRadius}, Modified: {__instance.agent.radius}");
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
            if (debugTriggerFlags) Script.Logger.LogInfo($"{LibraryCalls.DebugStringHead(__instance)} registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.");
        }
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
