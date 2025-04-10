using System;
using System.Collections.Generic;

namespace NaturalSelection.Generics;

public class LibraryCalls
{
    static bool debugUnspecified = Script.debugUnspecified;
    static bool debugSpam = Script.spammyLogs;
    static bool debugTriggerFlag = Script.debugTriggerFlags;
        public static string DebugStringHead(EnemyAI? instance)
    {
        //if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
        return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance);
    }
    public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
    {
        if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetCompleteList!");
        return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
    }

    public static List<EnemyAI> GetInsideOrOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
    {
        if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetInsideOrOutsideEnemyList!");
        return NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(importEnemyList, instance);
    }

    public static EnemyAI? FindClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
    {
        if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library findClosestEnemy!");
        return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(importEnemyList, importClosestEnemy, instance, includeTheDead);
    }
    public static List<EnemyAI> FilterEnemyList(List<EnemyAI> importEnemyList, List<Type>? targetTypes, List<string>? blacklist, EnemyAI instance, bool inverseToggle = false, bool filterOutImmortal = true)
    {
        if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library filterEnemyList!");
        List<string> tempList = new List<string>();
        if (blacklist != null)
        {
            foreach (var item in blacklist)
            {
                tempList.Add(item.Split(":")[0]);
            }
        }
        return NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(importEnemyList, targetTypes, tempList, instance, inverseToggle, filterOutImmortal);
    }


    static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
    {
        if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
        return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, importEnemyList, width, importRange, proximityAwareness);
    }
}