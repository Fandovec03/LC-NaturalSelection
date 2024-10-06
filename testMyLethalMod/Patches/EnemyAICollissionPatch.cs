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
        static float HitCooldownTime = (float)0.1;


        public static void DebugLog(string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
            HitCooldownTime -= Time.deltaTime;

            if (HitCooldownTime <= 0)
            {
                Script.Logger.LogInfo(mainscript + ", ID: " + mainscript?.GetInstanceID() + "Hit collider of " + mainscript2 + ", ID: " + mainscript2?.GetInstanceID() + ", Tag: " + text);
                HitCooldownTime = (float)0.1;
            }

            if (mainscript != null && mainscript2 != null)
            {
                if (mainscript is SandSpiderAI && mainscript2 is not SandSpiderAI && mainscript2 != null)
                {
                    SandSpiderAI? spiderAI = mainscript as SandSpiderAI;

                    if (spiderAI?.timeSinceHittingPlayer > 1f && mainscript2 is HoarderBugAI)
                    {
                        spiderAI.timeSinceHittingPlayer = 0f;
                        spiderAI.creatureSFX.PlayOneShot(spiderAI.attackSFX);
                        mainscript2.HitEnemy(2, null, playHitSFX: true);
                    }
                }

                if (mainscript is BlobAI && mainscript2 is not BlobAI && mainscript2 != null)
                {
                    BlobAI? blobAI = mainscript as BlobAI;

                    if (blobAI?.timeSinceHittingLocalPlayer > 1.5f && mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI)
                    {
                        if (mainscript2 is FlowermanAI)
                        {
                            FlowermanAI? flowermanAI = mainscript2 as FlowermanAI;

                            float AngerbeforeHit = flowermanAI.angerMeter;
                            blobAI.timeSinceHittingLocalPlayer = 0f;
                            flowermanAI.HitEnemy(1, null, playHitSFX: true);
                            flowermanAI.isInAngerMode = false;  
                            flowermanAI.angerMeter = AngerbeforeHit;
                        }

                        else
                        {
                            blobAI.timeSinceHittingLocalPlayer = 0f;
                            mainscript2.HitEnemy(1, null, playHitSFX: true);
                        }
                    }
                }
            }
        } 
    }

    [HarmonyPatch(typeof(EnemyAICollisionDetect), "OnTriggerStay")]
    class AICollisionDetectPatch
    {
        //[HarmonyPrefix]
        static bool Prefix(Collider other, EnemyAICollisionDetect __instance)
        {
            EnemyAICollisionDetect compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();


            if (__instance != null)
            {
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.DebugLog("Player", null, null);
                    return true;
                }
                if (other.CompareTag("Enemy") && compoment2.mainScript != __instance.mainScript && compoment2.mainScript.isEnemyDead == false
                      && IsEnemyImmortal.EnemyIsImmortal(compoment2.mainScript) == false)
                {
                    if (compoment2.mainScript != null)
                    {
                        OnCollideWithUniversal.DebugLog("Enemy", __instance.mainScript, compoment2.mainScript);
                    }
                    return true;
                }

                if (other == null)
                {
                    Script.Logger.LogError("Collider is NULL");
                }
            }
            //Script.Logger.LogError("EnemyAICollisionDetect triggered, Return stage");
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
                if(instance is BlobAI)
                    {
                    return true;
                }
                if(instance is SpringManAI)
                    {
                    return true;
                }
                if(instance is SandWormAI)
                    {
                    return true;
                }
                if (instance is ButlerBeesEnemyAI)
                {
                    return true;
                }
            return false;
        }
    }
}
