using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.EnemyPatches
{
    public class OnCollideWithUniversal
    {
        static float HitCooldownTime = 0.3f;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool enableSlime = Script.BoundingConfig.enableSlime.Value;
        static bool logUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool logSpider = Script.BoundingConfig.debugSpiders.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;

        public static void Collide(string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {
           // if (HitCooldownTime <= 0f)
            //{
                if (logUnspecified)Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(mainscript) + "Hit collider of " + EnemyAIPatch.DebugStringHead(mainscript2) + ", Tag: " + text);
                //HitCooldownTime = 0.3f;
            //}
            if (mainscript != null && text == "Player")
            {
                
            }
            if (mainscript != null && mainscript2 != null)
            {
                if (mainscript is SandSpiderAI && mainscript2 is not SandSpiderAI && mainscript2 != null && enableSpider)
                {
                    SandSpiderAI spiderAI = (SandSpiderAI)mainscript;
                    if (logSpider) Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(mainscript) + " timeSinceHittingPlayer: " + spiderAI.timeSinceHittingPlayer);
                    if (spiderAI.timeSinceHittingPlayer > 1f)
                    {
                        spiderAI.timeSinceHittingPlayer = 0f;
                        spiderAI.creatureSFX.PlayOneShot(spiderAI.attackSFX);
                        if (mainscript2 is HoarderBugAI)
                        {
                            if (mainscript2.enemyHP > 2)
                            {
                                mainscript2.HitEnemy(2, null, playHitSFX: true);

                            }
                            else
                            {
                                mainscript2.HitEnemy(1, null, playHitSFX: true);
                            }
                        }
                        if (mainscript2 is PufferAI)
                        {
                            if (mainscript2.enemyHP > 2)
                            {
                                PufferAIPatch.CustomOnHit(2, mainscript, playHitSFX: true, (PufferAI)mainscript2);      
                            }
                            else
                            {
                                PufferAIPatch.CustomOnHit(1, mainscript, playHitSFX: true, (PufferAI)mainscript2);
                            }
                        }
                        if (logSpider) Script.Logger.LogMessage(EnemyAIPatch.DebugStringHead(mainscript) + " Hit " + EnemyAIPatch.DebugStringHead(mainscript2)+ ", Tag: " + text);
                    }
                }

                if (mainscript is BlobAI && mainscript2 is not BlobAI && mainscript2 != null && enableSlime)
                {
                   BlobAIPatch.OnCustomEnemyCollision((BlobAI)mainscript, mainscript2);
                }

                if (mainscript is RedLocustBees && mainscript2 is not RedLocustBees && mainscript2 != null)
                {
                    BeeAIPatch.OnCustomEnemyCollision((RedLocustBees)mainscript, mainscript2);
                }

                /*
                if (mainscript is PufferAI && mainscript2 is not PufferAI && mainscript2 != null)
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
        static bool Prefix(Collider other, EnemyAICollisionDetect __instance)
        {
            EnemyAICollisionDetect compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            //HitDetectionNullCD -= Time.deltaTime;

            if (__instance != null)
            {
                /*if ((other == null || __instance.mainScript == null || compoment2 == null || compoment2.mainScript == null) && HitDetectionNullCD < 0f)
                {
                    HitDetectionNullCD = 0.5f;
                }*/

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
