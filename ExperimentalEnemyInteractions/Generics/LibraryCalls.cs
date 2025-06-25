using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace NaturalSelection.Generics;

public class LibraryCalls
{
    static bool debugSpam = Script.Bools["spammyLogs"];
    static bool debugTriggerFlag = Script.Bools["debugTriggerFlags"];
    static bool debugLibraryCalls = Script.Bools["debugLibrary"];

    static void Event_OnConfigSettingChanged(string entryKey, bool value)
    {
            if (entryKey == "debugTriggerFlags") debugTriggerFlag = value;
            if (entryKey == "spammyLogs") { debugSpam = value; NaturalSelectionLib.NaturalSelectionLib.SetLibraryLoggers(Script.Logger, value, debugLibraryCalls); }
            if (entryKey == "debugLibrary") { debugLibraryCalls = value; NaturalSelectionLib.NaturalSelectionLib.SetLibraryLoggers(Script.Logger, debugSpam, value); }

            //Script.Logger.Log(LogLevel.Message,$"LibraryCalls received event. debugTriggerFlag = {debugTriggerFlag}, debugSpam = {debugSpam}");
    }
    public static void SubscribeToConfigChanges()
    {
        Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
    }

    public static string DebugStringHead(EnemyAI? instance, bool shortFormat = true)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library DebugStringHead!");
        return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance, shortFormat);
    }
    public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library GetCompleteList!");
        return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
    }

    public static void GetInsideOrOutsideEnemyList(ref List<EnemyAI> importEnemyList, EnemyAI instance)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library GetInsideOrOutsideEnemyList!");
        NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(ref importEnemyList, instance);
    }

    public static EnemyAI? FindClosestEnemy(ref List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library findClosestEnemy!");
        return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(ref importEnemyList, importClosestEnemy, instance, includeTheDead);
    }
    public static void FilterEnemyList(ref List<EnemyAI> importEnemyList, List<string>? blacklist, EnemyAI instance, bool filterOutImmortal = true, bool filterTheSameType = true)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library filterEnemyList!");
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library filterEnemyList!");
        NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(ref importEnemyList, blacklist, instance, filterOutImmortal, filterTheSameType);
    }

    public static void FilterEnemySizes(ref Dictionary<EnemyAI, int> importEnemySizeDict, int[] enemySizes, EnemyAI instance, bool inverseToggle = false)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library FilterEnemySizes!");
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info, "Called library FilterEnemySizes!");
        NaturalSelectionLib.NaturalSelectionLib.FilterEnemySizes(ref importEnemySizeDict, enemySizes, instance, inverseToggle);
    }

    public static void FilterEnemySizes(ref List<EnemyAI> importEnemySizeDict, EnemySize[] enemySizes, EnemyAI instance, bool inverseToggle = false)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.LogInfo("Called library FilterEnemySizes!");
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info, "Called library FilterEnemySizes!");
        NaturalSelectionLib.NaturalSelectionLib.FilterEnemySizes(ref importEnemySizeDict, enemySizes, instance, inverseToggle);
    }

    static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, ref List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1, float importRadius = 0f,Vector3? importEyePosition = null)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info,"Called library GetEnemiesInLOS!");
        return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, ref importEnemyList, width, importRange, proximityAwareness, importRadius, importEyePosition);
    }

    static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, float width = 45f, int importRange = 0, float proximityAwareness = -1, float importRadius = 0f, Vector3? importEyePosition = null)
    {
        if (debugLibraryCalls && debugSpam && debugTriggerFlag) Script.Logger.Log(LogLevel.Info, "Called library GetEnemiesInLOS!");
        return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, width, importRange, proximityAwareness, importRadius, importEyePosition);
    }

}