using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{
    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float refreshCDtime = 1f;
        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugTriggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            Script.Logger.LogInfo("Called Setup library!");
            NaturalSelectionLib.NaturalSelectionLib.LibrarySetup(Script.Logger, debugSpam,debugUnspecified);
            foreach (Collider collider in __instance.gameObject.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger != true)
                {
                    collider.isTrigger = true;
                    Script.Logger.LogInfo("Found non-trigger collider.");
                }
            }
            __instance.agent.radius = __instance.agent.radius * Script.clampedAgentRadius;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfixPatch(EnemyAI __instance)
        {
            if (refreshCDtime <= 0)
            {
                if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
                NaturalSelectionLib.NaturalSelectionLib.enemyListUpdate(__instance, refreshCDtime);
                refreshCDtime = 1f;
            }
            else refreshCDtime -= Time.deltaTime;
        }

        public static string DebugStringHead(EnemyAI? __instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(__instance);
        }
        public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance,FilterThemselves);
        }

        public static List<EnemyAI> GetOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.GetOutsideEnemyList(importEnemyList, instance);
        }

        public static List<EnemyAI> GetInsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.GetInsideEnemyList(importEnemyList, instance);
        }

        public static EnemyAI? findClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI __instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.findClosestEnemy(importEnemyList, importClosestEnemy, __instance);
        }
        public static List<EnemyAI> filterEnemyList(List<EnemyAI> importEnemyList, List<Type> targetTypes, EnemyAI instance, bool inverseToggle = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.filterEnemyList(importEnemyList, targetTypes, instance, inverseToggle);
        }
        

        static public Dictionary<EnemyAI,float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, importEnemyList, width, importRange, proximityAwareness);
        }

        static public int ReactToHit(int force = 0, EnemyAI? enemyAI = null, PlayerControllerB? player = null)
        {
            if (force > 0)
            {
                return 1;
            }
            if (force > 1)
            {
                return 2;
            }
            return 0;
        }
    }
    
    public class ReversePatchEnemy : EnemyAI
    {
        public override void Update()
        {
            base.Update();
        }
    }
}
