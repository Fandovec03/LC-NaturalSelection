using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(PufferAI))]
    class PufferAIPatch()
    {
        static bool enableSporeLizard = Script.BoundingConfig.enableSporeLizard.Value;
        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
                if (enableSporeLizard != true) return;
                instance.creatureAnimator.SetBool("alerted", true);
                instance.enemyHP -= force;
                Script.Logger.LogDebug("SpodeLizard CustomHit Triggered");

                if (instance.enemyHP <= 0)
                {
                    instance.KillEnemy(true);
                }
        }
    }
}