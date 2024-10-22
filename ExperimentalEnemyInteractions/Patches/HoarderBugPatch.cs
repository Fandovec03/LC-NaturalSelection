using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    class HoarderBugPatch()
    {
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
    }
}
