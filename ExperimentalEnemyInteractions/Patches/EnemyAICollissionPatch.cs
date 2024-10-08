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
        static float HitCooldownTime = 0.2f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool enableSlime = Script.BoundingConfig.enableSlime.Value;


        public static void DebugLog(string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
            HitCooldownTime -= Time.deltaTime;

            if (HitCooldownTime <= 0f)
            {
                if (debugMode) Script.Logger.LogInfo(mainscript + ", ID: " + mainscript?.GetInstanceID() + "Hit collider of " + mainscript2 + ", ID: " + mainscript2?.GetInstanceID() + ", Tag: " + text);
                HitCooldownTime = 0.2f;
            }

            if (mainscript != null && mainscript2 != null)
            {
                if (mainscript is SandSpiderAI && mainscript2 is not SandSpiderAI && mainscript2 != null && enableSpider)
                {
                    SandSpiderAI? spiderAI = mainscript as SandSpiderAI;

                    if (spiderAI?.timeSinceHittingPlayer > 1f && mainscript2 is HoarderBugAI)
                    {
                        spiderAI.timeSinceHittingPlayer = 0f;
                        spiderAI.creatureSFX.PlayOneShot(spiderAI.attackSFX);

                        if (mainscript2.enemyHP > 2 )
                        {
                            mainscript2.HitEnemy(2, null, playHitSFX: true);
                        }
                        else
                        {
                            mainscript2.HitEnemy(1, null, playHitSFX: true);
                        }
                    }
                }

                if (mainscript is BlobAI && mainscript2 is not BlobAI && mainscript2 != null && enableSlime)
                {
                    BlobAI? blobAI = mainscript as BlobAI;

                    if (blobAI?.timeSinceHittingLocalPlayer > 1.5f && mainscript2 is not NutcrackerEnemyAI && mainscript2 is not CaveDwellerAI)
                    {
                        if (mainscript2 is FlowermanAI)
                        {
                            FlowermanAI? flowermanAI = mainscript2 as FlowermanAI;
                            if (flowermanAI != null)
                            {
                                float AngerbeforeHit = flowermanAI.angerMeter;
                                blobAI.timeSinceHittingLocalPlayer = 0f;
                                flowermanAI.HitEnemy(1, null, playHitSFX: true);
                                flowermanAI.isInAngerMode = false;
                                flowermanAI.angerMeter = AngerbeforeHit;
                            }
                           
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
        static float HitDetectionNullCD = 0.5f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        //[HarmonyPrefix]
        static bool Prefix(Collider other, EnemyAICollisionDetect __instance)
        {
            EnemyAICollisionDetect compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            HitDetectionNullCD -= Time.deltaTime;

            if (__instance != null)
            {
                if ((other == null || __instance.mainScript == null || compoment2 == null || compoment2.mainScript == null) && HitDetectionNullCD < 0f && debugMode)
                {
                    if (other == null)
                    {
                        Script.Logger.LogError("Collider is NULL");
                    }
                    if (__instance.mainScript == null)
                    {
                        Script.Logger.LogError("Instance.mainScript is NULL");
                    }
                    if (compoment2 == null)
                    {
                        Script.Logger.LogError("Compoment2 is NULL");
                    }
                    HitDetectionNullCD = 0.5f;
                }

                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.DebugLog("Player", null, null);
                    return true;
                }
                if (other.CompareTag("Enemy") && compoment2 != null && compoment2.mainScript != __instance.mainScript && compoment2.mainScript.isEnemyDead == false
                      && IsEnemyImmortal.EnemyIsImmortal(compoment2.mainScript) == false)
                {
                    OnCollideWithUniversal.DebugLog("Enemy", __instance.mainScript, compoment2.mainScript);
                    return true;
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
