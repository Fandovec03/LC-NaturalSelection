using HarmonyLib;
using System.Collections.Generic;

namespace NaturalSelection.EnemyPatches
{
    class HoarderBugValues
    {
        public EnemyAI? targetEnemy = null;
        public EnemyAI? closestEnemy = null;
        public bool alertedByEnemy = false;
        public List<EnemyAI> enemies = new List<EnemyAI>();
        public List<EnemyAI> enemiesInLOS = new List<EnemyAI>();

    }


    [HarmonyPatch(typeof(HoarderBugAI))]
    class HoarderBugPatch()
    {
        static Dictionary<HoarderBugAI, HoarderBugValues> hoarderBugList = [];

        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, HoarderBugAI __instance)
        {
            __instance.enemyHP -= force;
            Script.Logger.LogDebug("Hoarderbug CustomHit Triggered");
            __instance.creatureVoice.PlayOneShot(__instance.hitPlayerSFX);
            RoundManager.PlayRandomClip(__instance.creatureVoice, __instance.angryScreechSFX);
            __instance.SwitchToBehaviourState(1);

            if (__instance.enemyHP <= 0)
            {
                __instance.KillEnemy(false);
            }
        }

        /*
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(HoarderBugAI __instance)
        {
            if (!hoarderBugList.ContainsKey(__instance))
            {
                hoarderBugList.Add(__instance, new HoarderBugValues());
            }
        }
        */ /*
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(HoarderBugAI __instance)
        {
            HoarderBugValues Bugvalues = hoarderBugList[__instance];

            Bugvalues.enemiesInLOS = EnemyAIPatch.GetEnemiesInLOS(__instance, Bugvalues.enemies, 60f, 12, 3f).Keys.ToList();
        }
        */
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
    }
}
