using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class PufferData()
    {
        internal int reactionToHit = 0;
        internal EnemyAI? targetEnemy = null;
    }

    [HarmonyPatch(typeof(PufferAI))]
    class PufferAIPatch
    {

        static Dictionary<PufferAI, PufferData> pufferList = [];

        /*static void Event_OnConfigSettingChanged(string boolName, bool newValue)
        {
            if (boolName == "debugUnspecified")
            {
                debugUnspecified = newValue;
            }
            if (boolName == "debugTriggerFlags")
            {
                debugTriggerFlags = newValue;
            }
            Script.Logger.Log(LogLevel.Message,$"Successfully invoked event. boolName = {boolName}, newValue = {newValue}");
        }*/

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(PufferAI __instance)
        {
            if (!pufferList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                pufferList.Add(__instance, new PufferData());
            }
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool PrefixAIInterval(PufferAI __instance)
        {
            if (__instance.isEnemyDead) return true;
            CheckDataIntegrityPuffer(__instance);
            PufferData pufferData = pufferList[__instance];

            if (__instance.currentBehaviourStateIndex == 2 && pufferData.targetEnemy != null && (Vector3.Distance(__instance.closestSeenPlayer.transform.position, __instance.transform.position) < Vector3.Distance(pufferData.targetEnemy.transform.position, __instance.transform.position)))
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();
                return false;
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void PostfixAIInterval(PufferAI __instance)
        {
            if (__instance.isEnemyDead) return;
            CheckDataIntegrityPuffer(__instance);
            PufferData pufferData = pufferList[__instance];

            if (__instance.currentBehaviourStateIndex == 2 && pufferData.targetEnemy != null && (Vector3.Distance(__instance.closestSeenPlayer.transform.position, __instance.transform.position) < Vector3.Distance(pufferData.targetEnemy.transform.position, __instance.transform.position)))
            {
                __instance.SetDestinationToPosition(pufferData.targetEnemy.transform.position, checkForPath: true);
            }
            else
            {
                pufferData.reactionToHit = 0;
            }
        }

        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
                PufferData pufferData = pufferList[instance];
                instance.creatureAnimator.SetBool("alerted", true);
                instance.enemyHP -= force;
                Script.Logger.Log(LogLevel.Debug,"SpodeLizard CustomHit Triggered");
                HitEnemyTest(force, enemyWhoHit, playHitSFX, instance);
                instance.SwitchToBehaviourState(2);
                if (instance.enemyHP <= 0)
                {
                    instance.KillEnemy(true);
                }
        }

        public static void CheckDataIntegrityPuffer(PufferAI __instance)
        {
            if (!pufferList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                pufferList.Add(__instance, new PufferData());
            }
        }

        public static void HitEnemyTest(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
            int reactionINT = EnemyAIPatch.ReactToHit(force);
            PufferData data = pufferList[instance];

            if (enemyWhoHit is SandSpiderAI)
            {
                data.reactionToHit = 2;
            }
            else
            {
                data.reactionToHit = 1;
            }
        }
    }
}