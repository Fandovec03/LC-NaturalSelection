using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace ExperimentalEnemyInteractions.Patches
{
    public class OnCollideWithUniversal
    {
        public static float Logger1CD = 0;

        public static void DebugLog(Collider other, string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
            if (Logger1CD <= 0)
            {
                Script.Logger.LogInfo(mainscript + ", ID: " + mainscript?.GetInstanceID() + "hit collider " + other.gameObject.name + " Of " + mainscript2 + ", ID: " + mainscript2?.GetInstanceID() + ", Tag: " + text);
                Logger1CD = (float)0.5;
            }
            Logger1CD -= Time.deltaTime;

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
            EnemyAI compoment2 = other.gameObject.GetComponent<EnemyAI>();

            if (__instance != null && other != null && compoment2 != null)
            {
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.DebugLog(other, "Player", null, null);
                }
                if (other.CompareTag("Enemy") && __instance.mainScript != compoment2 && compoment2.isEnemyDead == false
                      && IsEnemyImmortal.EnemyIsImmortal(compoment2) == false)
                {
                    if (compoment2 != null)
                    {
                        OnCollideWithUniversal.DebugLog(other, "Enemy", __instance.mainScript, compoment2);
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
                if (instance is JesterAI)
                {
                    return true;
                }
                if (instance is BlobAI)
                    {
                    return true;
                }
                if (instance is SpringManAI)
                    {
                    return true;
                }
                if (instance is SandWormAI)
                    {
                    return true;
                }
            return false;
        }
    }
}
