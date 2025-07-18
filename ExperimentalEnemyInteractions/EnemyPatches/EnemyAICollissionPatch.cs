using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.Compatibility;
using NaturalSelection.Generics;
using System;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    class OnCollideWithUniversal
    {
        class EnemyCollisionData
        {
            internal bool subscribed = false;
        }

        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool enableSlime = Script.BoundingConfig.enableSlime.Value;
        static bool enableBees = Script.BoundingConfig.enableRedBees.Value;

        static bool logUnspecified = Script.Bools["debugUnspecified"];
        static bool triggerFlag = Script.Bools["debugTriggerFlags"];
        static bool debugSpam = Script.Bools["spammyLogs"];

        static void Event_OnConfigSettingChanged()
        {
            logUnspecified = Script.Bools["debugUnspecified"];
            debugSpam = Script.Bools["spammyLogs"];
            triggerFlag = Script.Bools["debugTriggerFlags"];
            //Script.NSLog(LogLevel.Message,$"EnemYAICollision received event. logUnspecified = {logUnspecified}, debugSpam = {debugSpam}, triggetFlag = {triggerFlag}");
        }

        public static void Collide(string text, EnemyAI? mainscript, EnemyAI? mainscript2, GameObject? gameObject = null)
        {

            if (logUnspecified && debugSpam && triggerFlag) Script.Logger.Log(LogLevel.Debug,$"{LibraryCalls.DebugStringHead(mainscript)} hit collider of {LibraryCalls.DebugStringHead(mainscript2)} Tag: {text}");
            if (mainscript != null && text == "Player")
            {

            }

            if (mainscript != null && text == "Corpse")
            {
                if (mainscript is BlobAI && enableSlime && gameObject != null)
                {
                    BlobAIPatch.OnEnemyCorpseCollision((BlobAI)mainscript, gameObject);
                }
            }
            if (mainscript != null && mainscript2 != null)
            {
                if (mainscript is SandSpiderAI && mainscript2 != null && enableSpider)
                {
                    SandSpiderAIPatch.OnCustomEnemyCollision((SandSpiderAI)mainscript, mainscript2);
                }

                if (mainscript is BlobAI && mainscript2 != null && enableSlime)
                {
                    BlobAIPatch.OnCustomEnemyCollision((BlobAI)mainscript, mainscript2);
                }

                if (mainscript is RedLocustBees && mainscript2 != null && enableBees)
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
    [HarmonyPatch(typeof(EnemyAICollisionDetect))]
    public class AICollisionDetectPatch
    {
        [HarmonyPatch(nameof(EnemyAICollisionDetect.OnTriggerStay))]
        [HarmonyPrefix]
        static bool OnTriggerStayPrefix(Collider other, EnemyAICollisionDetect __instance)
        {


            if (other == null) { Script.Logger.Log(LogLevel.Error,$"{LibraryCalls.DebugStringHead(__instance.mainScript)} Collider is null! Using original function..."); return true; }
            EnemyAICollisionDetect? compoment2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();

            if (__instance != null)
            {
                EnemyAI? hitEnemy = null;
                DeadBodyTrackerScript corpse = other.GetComponent<DeadBodyTrackerScript>();

                if (corpse != null)
                {
                    if (Script.Bools["spammyLogs"] && Script.Bools["debugTriggerFlags"]) Script.Logger.LogInfo("Collided with corpse");
                    OnCollideWithUniversal.Collide("Corpse", __instance.mainScript, null,corpse.gameObject);
                }
                if (other.CompareTag("Player") && __instance.mainScript.isEnemyDead == false)
                {
                    OnCollideWithUniversal.Collide("Player", null, null);
                    return true;
                }
                if (compoment2 != null)
                {
                    if (compoment2?.mainScript == null)
                    {
                        hitEnemy = compoment2?.mainScript;
                    }
                    else
                    {
                        hitEnemy = other.gameObject.GetComponentInParent<EnemyAI>();
                    }
                    if (other.CompareTag("Enemy") && hitEnemy != null && hitEnemy != __instance.mainScript && !IsEnemyImmortal.EnemyIsImmortal(hitEnemy) && !__instance.mainScript.isEnemyDead)
                    {
                        OnCollideWithUniversal.Collide("Enemy", __instance.mainScript, hitEnemy);
                        return true;
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
