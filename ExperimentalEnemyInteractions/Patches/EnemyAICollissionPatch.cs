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
        static float HitCooldownTime = 0.3f;
        static bool debugMode = Script.BoundingConfig.debugBool.Value;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool enableSlime = Script.BoundingConfig.enableSlime.Value;


        public static void Collide(string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
            HitCooldownTime -= Time.deltaTime;

            if (HitCooldownTime <= 0f)
            {
                Script.Logger.LogDebug(mainscript + ", ID: " + mainscript?.GetInstanceID() + "Hit collider of " + mainscript2 + ", ID: " + mainscript2?.GetInstanceID() + ", Tag: " + text);
                HitCooldownTime = 0.3f;
            }
            if (mainscript != null && text == "Player")
            {
                
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
                   BlobAIPatch.OnCustomEnemyCollision((BlobAI)mainscript, mainscript2);
                }

                /*if (mainscript is PufferAI && mainscript2 is not PufferAI && mainscript2 != null)
                {
                    PufferAI? pufferAI = mainscript as PufferAI;
                    if (pufferAI != null)
                    {
                    PufferAIPatch.CustomOnHit(1, mainscript, true, pufferAI);
                    }
                }

                if (mainscript is HoarderBugAI && mainscript2 is not HoarderBugAI && mainscript2 != null)
                {
                    HoarderBugAI? hoarderBugAI = mainscript as HoarderBugAI;
                    if (hoarderBugAI != null)
                    {
                        HoarderBugPatch.CustomOnHit(1, mainscript, true, hoarderBugAI);
                    }
                }*/
            }
        } 
    }

    [HarmonyPatch(typeof(EnemyAICollisionDetect), "OnTriggerStay")]
    public class AICollisionDetectPatch
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
                if ((other == null || __instance.mainScript == null || compoment2 == null || compoment2.mainScript == null) && HitDetectionNullCD < 0f)
                {
                    HitDetectionNullCD = 0.5f;
                }

#pragma warning disable CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.Collide("Player", null, null);
                    return true;
                }
#pragma warning restore CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
#pragma warning disable CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
                if (other.CompareTag("Enemy") && compoment2 != null && compoment2.mainScript != __instance.mainScript && compoment2.mainScript.isEnemyDead == false
                      && IsEnemyImmortal.EnemyIsImmortal(compoment2.mainScript) == false)
                {
                    OnCollideWithUniversal.Collide("Enemy", __instance.mainScript, compoment2.mainScript);
                    return true;
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
