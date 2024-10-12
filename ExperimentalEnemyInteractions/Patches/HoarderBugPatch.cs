using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    class HoarderBugPatch()
    {
    public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, HoarderBugAI instance)
    {
                instance.enemyHP -= force;
                Script.Logger.LogDebug("Hoarderbug CustomHit Triggered");
                RoundManager.PlayRandomClip(instance.creatureVoice, instance.angryScreechSFX);

                if (instance.enemyHP <= 0)
                {
                    instance.KillEnemy(false);
                }
            }
    }
}
