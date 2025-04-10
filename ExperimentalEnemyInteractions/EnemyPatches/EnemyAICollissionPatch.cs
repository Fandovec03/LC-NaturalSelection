using HarmonyLib;
using NaturalSelection.Generics;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class OnCollideWithUniversal
    {
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool enableSlime = Script.BoundingConfig.enableSlime.Value;
        static bool enableBees = Script.BoundingConfig.enableRedBees.Value;

        static bool logUnspecified = Script.debugUnspecified;
        static bool triggerFlag = Script.debugTriggerFlags;
        static bool logSpider = Script.debugSpiders;
        static bool debugSpam = Script.spammyLogs;

        public static void Collide(string text, EnemyAI? mainscript, EnemyAI? mainscript2)
        {

            if (logUnspecified && debugSpam && triggerFlag) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(mainscript)} hit collider of {LibraryCalls.DebugStringHead(mainscript2)} Tag: {text}");
            if (mainscript != null && text == "Player")
            {
                
            }
            if (mainscript != null && mainscript2 != null)
            {
                if (mainscript is SandSpiderAI && mainscript2 is not SandSpiderAI && mainscript2 != null && enableSpider)
                {
                    SandSpiderAI spiderAI = (SandSpiderAI)mainscript;
                    if (logSpider && triggerFlag) Script.Logger.LogDebug($"{LibraryCalls.DebugStringHead(mainscript)} timeSinceHittingPlayer: {spiderAI.timeSinceHittingPlayer}");
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
                            else if (mainscript2.enemyHP > 0)
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
                            else if (mainscript2.enemyHP > 0)
                            {
                                PufferAIPatch.CustomOnHit(1, mainscript, playHitSFX: true, (PufferAI)mainscript2);
                            }
                        }
                        if (logSpider && triggerFlag) Script.Logger.LogMessage($"{LibraryCalls.DebugStringHead(mainscript)} hit {LibraryCalls.DebugStringHead(mainscript2)}, Tag: {text}");
                    }
                }

                if (mainscript is BlobAI && mainscript2 is not BlobAI && mainscript2 != null && enableSlime)
                {
                   BlobAIPatch.OnCustomEnemyCollision((BlobAI)mainscript, mainscript2);
                }

                if (mainscript is RedLocustBees && mainscript2 is not RedLocustBees && mainscript2 != null && enableBees)
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
        static bool Prefix(Collider other, EnemyAICollisionDetect __instance)
        {
            if (other == null) { Script.Logger.LogError($"{LibraryCalls.DebugStringHead(__instance.mainScript)} Collider is null! Using original function..."); return true; }
            EnemyAICollisionDetect compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();

            if (__instance != null)
            {
#pragma warning disable CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.Collide("Player", null, null);
                    return true;
                }
#pragma warning restore CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
#pragma warning disable CS8602 // P��stup p�es ukazatel k mo�n�mu odkazu s hodnotou null
                if (other.CompareTag("Enemy") && compoment2 != null && compoment2.mainScript != __instance.mainScript && IsEnemyImmortal.EnemyIsImmortal(compoment2.mainScript) == false && !__instance.mainScript.isEnemyDead)
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
