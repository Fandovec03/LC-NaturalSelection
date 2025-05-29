using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Generics;
using System.Collections.Generic;

namespace NaturalSelection.EnemyPatches
{
    class HoarderBugValues()
    {
        internal EnemyAI? targetEnemy = null;
        internal EnemyAI? closestEnemy = null;
        internal bool alertedByEnemy = false;
        internal List<EnemyAI> enemies = new List<EnemyAI>();
        internal List<EnemyAI> enemiesInLOS = new List<EnemyAI>();
        internal bool limitSpeed = false;
        internal float limitedSpeed = 0f;
    }


    [HarmonyPatch(typeof(HoarderBugAI))]
    class HoarderBugPatch()
    {
        static Dictionary<HoarderBugAI, HoarderBugValues> hoarderBugList = [];
        static bool triggerFlag = Script.Bools["debugTriggerFlags"];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugTriggerFlags") triggerFlag = value;
            //Script.Logger.Log(LogLevel.Message,$"Hoarder bug received event. triggerFlag = {triggerFlag}");
        }

        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, HoarderBugAI __instance)
        {
            __instance.enemyHP -= force;
            if (triggerFlag) Script.Logger.Log(LogLevel.Debug,"Hoarderbug CustomHit Triggered");
            if (playHitSFX)
            {
                WalkieTalkie.TransmitOneShotAudio(__instance.creatureVoice, __instance.enemyType.hitBodySFX);
                __instance.creatureVoice.PlayOneShot(__instance.enemyType.hitBodySFX);
            }
            if (__instance.creatureVoice != null) __instance.creatureVoice.PlayOneShot(__instance.enemyType.hitEnemyVoiceSFX);
            RoundManager.PlayRandomClip(__instance.creatureVoice, __instance.angryScreechSFX);
            __instance.SwitchToBehaviourState(1);

            if (__instance.enemyHP <= 0)
            {
                __instance.KillEnemyOnOwnerClient(false);
            }
        }

        
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(HoarderBugAI __instance)
        {
            if (!hoarderBugList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Info, $"Creating data container for {LibraryCalls.DebugStringHead(__instance)}");
                hoarderBugList.Add(__instance, new HoarderBugValues());
            }

            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }
        
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(HoarderBugAI __instance)
        {
            if (__instance.isEnemyDead) return;
            CheckDataIntegrityHoarder(__instance);
            HoarderBugValues Bugvalues = hoarderBugList[__instance];
            if (Bugvalues.limitSpeed)
            {
                __instance.agent.speed = Bugvalues.limitedSpeed;
            }
            //Bugvalues.enemiesInLOS = EnemyAIPatch.GetEnemiesInLOS(__instance, Bugvalues.enemies, 60f, 12, 3f).Keys.ToList();
        }

        /*
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(HoarderBugAI __instance)
        {
           /* if (!__instance.movingTowardsTargetPlayer && hoarderBugList[__instance].targetEnemy != null)
            {
                return false;
            }
            return true;
        }
        */ /*
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static void DoAIIntervalPostfix(HoarderBugAI __instance)
        {
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {

                    }
                    break;
                case 1:
                    {

                    }
                    break;
                case 2:
                    {
                       if(__instance.targetPlayer == __instance.watchingPlayer && hoarderBugList[__instance].alertedByEnemy == true)
                        {
                            __instance.targetPlayer = null;
                            __instance.movingTowardsTargetPlayer = false;
                        }

                    }
                    break;
            }      
        }
        */

        public static void CheckDataIntegrityHoarder(HoarderBugAI __instance)
        {
            if (!hoarderBugList.ContainsKey(__instance))
            {
                Script.Logger.Log(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                hoarderBugList.Add(__instance, new HoarderBugValues());
            }
        }
    }
}
