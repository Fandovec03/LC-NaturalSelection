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
        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugTriggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            if (debugSpam && debugUnspecified)Script.Logger.LogInfo("Called Setup library!");
            /*foreach (Collider collider in __instance.gameObject.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger != true)
                {
                    collider.isTrigger = true;
                    Script.Logger.LogInfo("Found non-trigger collider.");
                }
            }*/
            __instance.agent.radius = __instance.agent.radius * Script.clampedAgentRadius;
        }

        public static string DebugStringHead(EnemyAI? instance)
        {
            //if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance);
        }
        public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetCompleteList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance,FilterThemselves, includeOrReturnThedDead);
        }

        public static List<EnemyAI> GetOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetOutsideEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetOutsideEnemyList(importEnemyList, instance);
        }

        public static List<EnemyAI> GetInsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetInsideEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetInsideEnemyList(importEnemyList, instance);
        }

        public static EnemyAI? FindClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library findClosestEnemy!");
            return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(importEnemyList, importClosestEnemy, instance, includeTheDead);
        }
        public static List<EnemyAI> FilterEnemyList(List<EnemyAI> importEnemyList, List<Type> targetTypes, EnemyAI instance, bool inverseToggle = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library filterEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(importEnemyList, targetTypes, instance, inverseToggle);
        }
        

        static public Dictionary<EnemyAI,float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
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
