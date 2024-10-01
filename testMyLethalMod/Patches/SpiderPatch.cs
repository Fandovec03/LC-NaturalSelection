using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace testMyLethalMod.Patches
{
    public class OnCollideWithUniversal
    {
        public static void DebugLog(Collider other, string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
            Debug.Log("Hit collider " + other.gameObject.name + " Of " + mainscript2 + ", Tag: " + text);

            if (mainscript is SandSpiderAI && mainscript2 is not SandSpiderAI && mainscript2 != null)
            {
                SandSpiderAI? spiderAI = mainscript as SandSpiderAI;

                if (spiderAI?.timeSinceHittingPlayer > 1f)
                {
                    spiderAI.timeSinceHittingPlayer = 0f;
                    spiderAI.creatureSFX.PlayOneShot(spiderAI.attackSFX);
                    mainscript2.HitEnemy(2, null, playHitSFX: true);
                }
            }
        }

        
    }

    [HarmonyPatch(typeof(EnemyAICollisionDetect), "OnTriggerStay")]

    class AICollisionDetectPatch
    {
        static bool Prefix(Collider other, EnemyAICollisionDetect __instance)
        {
            EnemyAICollisionDetect compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();

            if (__instance != null)
            {
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.DebugLog(other, "Player", null, null);
                }
                if (other.CompareTag("Enemy") && compoment2.mainScript != __instance.mainScript && compoment2.mainScript.isEnemyDead == false
                      && IsEnemyImmortal.EnemyIsImmortal(compoment2.mainScript) == false)
                {
                    if (other.TryGetComponent<EnemyAI> != null)
                    {
                        OnCollideWithUniversal.DebugLog(other, "Enemy", __instance.mainScript, compoment2.mainScript);
                    }
                }
            }
            return true;
        }
    }

    public class IsEnemyImmortal
    {
        public static bool EnemyIsImmortal(EnemyAI instance)
        {
            if (instance is NutcrackerEnemyAI)
            {
                if (instance.currentBehaviourStateIndex == 0)
                {
                    return true;
                }
        
            }
            return false;
        }
    }
}
