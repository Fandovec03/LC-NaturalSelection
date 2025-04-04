using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{

    class EnemyData
    {
        public float originalAgentRadius = 0f;
        public SphereCollider sphereCollider = null;
        public Dictionary<Type, int> targetedByEnemies = new Dictionary<Type, int>();
        public int originalAP = 25;
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugTriggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;
        static Dictionary<EnemyAI, EnemyData> enemyData = [];

        public static int returnPriority(EnemyAI __instance)
        {
            return __instance.agent.avoidancePriority;
        }

        public static void addToAPModifier(EnemyAI __instance, int value = 1)
        {
            if (!enemyData[__instance].targetedByEnemies.ContainsKey(__instance.GetType())) enemyData[__instance].targetedByEnemies.Add(__instance.GetType(), value);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(EnemyAI __instance)
        {
            if (__instance.IsOwner)
            {
                if (__instance.agent.avoidancePriority > 0 && __instance.agent.avoidancePriority < 100) __instance.agent.avoidancePriority = enemyData[__instance].originalAP - enemyData[__instance].targetedByEnemies.Count;
                enemyData[__instance].targetedByEnemies.Clear();
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {

            if (!enemyData.ContainsKey(__instance)) enemyData.Add(__instance, new EnemyData());
            enemyData[__instance].originalAgentRadius = __instance.agent.radius;
            enemyData[__instance].originalAP = __instance.agent.avoidancePriority;
            //enemyData[__instance].sphereCollider = __instance.gameObject.AddComponent<SphereCollider>();
            //enemyData[__instance].sphereCollider.radius = enemyData[__instance].originalAgentRadius;
            //enemyData[__instance].sphereCollider.isTrigger = true;
            //__instance.agent.radius = __instance.agent.radius * Script.clampedAgentRadius;
        }
        public static string DebugStringHead(EnemyAI? instance)
        {
            //if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance);
        }
        public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetCompleteList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
        }

        public static List<EnemyAI> GetInsideOrOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetInsideOrOutsideEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(importEnemyList, instance);
        }

        public static EnemyAI? FindClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library findClosestEnemy!");
            return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(importEnemyList, importClosestEnemy, instance, includeTheDead);
        }
        public static List<EnemyAI> FilterEnemyList(List<EnemyAI> importEnemyList, List<Type>? targetTypes, List<string>? blacklist, EnemyAI instance, bool inverseToggle = false, bool filterOutImmortal = true)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library filterEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(importEnemyList, targetTypes, blacklist, instance, inverseToggle, filterOutImmortal);
        }


        static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
            return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, importEnemyList, width, importRange, proximityAwareness);
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

        /*[HarmonyPatch("KillEnemy")]
        static void KillEnemyTranspiller()
        {
            static IEnumerable<CodeInstruction> Transpiller(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldstr),
                    new CodeMatch(OpCodes.Call, OpCodes.Call))
                    .Repeat(matcher => matcher
                    .RemoveInstructions(2)
                    )
                    .InstructionEnumeration();
            }
        }*/

        [HarmonyPatch("HitEnemy")]
        [HarmonyPostfix]
        public static void HitEnemyPatch(EnemyAI __instance, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            string playerString = "unknown";
            if (playerWhoHit != null)
            {
                playerString = $"{playerWhoHit.playerUsername}(SteamID: {playerWhoHit.playerSteamId}, playerClientID: {playerWhoHit.playerClientId})";
            }
            Script.Logger.LogInfo($"{DebugStringHead(__instance)} registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.");
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

        /*public static readonly Action<EnemyAI> BaseInteractMethod;

        static ReversePatchAI()
        {
            var method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Update));
            var dm = new DynamicMethod("Base.Update", null, [typeof(EnemyAI)], typeof(EnemyAI));
            var gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            //gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, method);
            gen.Emit(OpCodes.Ret);

            BaseInteractMethod = (Action<EnemyAI>)dm.CreateDelegate(typeof(Action<EnemyAI>));
        }*/
    }
}
