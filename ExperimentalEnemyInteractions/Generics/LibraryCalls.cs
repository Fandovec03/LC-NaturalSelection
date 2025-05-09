using System;
using System.Collections.Generic;

namespace NaturalSelection.Generics;

public class LibraryCalls
{
    static bool debugSpam = Script.Bools["spammyLogs"];
    static bool debugTriggerFlag = Script.Bools["debugTriggerFlags"];

    static void Event_OnConfigSettingChanged(string entryKey, bool value)
    {
            if (entryKey == "debugTriggerFlags") debugTriggerFlag = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            //Script.Logger.LogMessage($"LibraryCalls received event. debugTriggerFlag = {debugTriggerFlag}, debugSpam = {debugSpam}");
    }
    public static void SubscribeToConfigChanges()
    {
        Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
    }

    public static string DebugStringHead(EnemyAI? instance, bool shortFormat = true)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library DebugStringHead!");
        return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance, shortFormat);
    }
    public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library GetCompleteList!");
        return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
    }

    public static void GetInsideOrOutsideEnemyList(ref List<EnemyAI> importEnemyList, EnemyAI instance)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library GetInsideOrOutsideEnemyList!");
        NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(ref importEnemyList, instance);
    }

    public static EnemyAI? FindClosestEnemy(ref List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library findClosestEnemy!");
        return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(ref importEnemyList, importClosestEnemy, instance, includeTheDead);
    }
    public static void FilterEnemyList(ref List<EnemyAI> importEnemyList, List<Type>? targetTypes, List<string>? blacklist, EnemyAI instance, bool inverseToggle = false, bool filterOutImmortal = true)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library filterEnemyList!");
        NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(ref importEnemyList, targetTypes, blacklist, instance, inverseToggle, filterOutImmortal);
    }

    static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, ref List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
    {
        if (debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
        return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, ref importEnemyList, width, importRange, proximityAwareness);
    }
    
}