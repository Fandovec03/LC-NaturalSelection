using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(PufferAI))]
    class PufferAIPatch ()
    {
        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
            instance.creatureAnimator.SetBool("alerted", true);
            instance.enemyHP -= force;
            Script.Logger.LogInfo("SpodeLizard CustomHit Triggered");

            if (instance.enemyHP <= 0)
            {
                instance.KillEnemy(true);
            }
        }
    }
}