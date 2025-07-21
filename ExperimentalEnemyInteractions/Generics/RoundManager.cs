using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NaturalSelection.Generics;
[HarmonyPatch(typeof(RoundManager))]
class RoundManagerPatch
{
    static float nextUpdate = 0;
    static Dictionary<Type, List<EnemyAI>> checkedTypes = new Dictionary<Type, List<EnemyAI>>();
    public static float updateListInterval = Script.BoundingConfig.globalListsUpdateInterval.Value;
    static bool logSpam = Script.Bools["spammyLogs"];
    static bool logUnspecified = Script.Bools["debugNetworking"];

    //Only used when SellBodiesFixed/Enhanced Monsters are in the modpack
    public static List<GameObject> deadEnemiesList = new List<GameObject>();

    static void Event_OnConfigSettingChanged(string entryKey, bool value)
    {
        if (entryKey == "debugNetworking") logUnspecified = value;
        if (entryKey == "spammyLogs") logSpam = value;
        //Script.Logger.Log(LogLevel.Message,$"RoundManager received event. logUnspecified = {logUnspecified}, logSpam = {logSpam}");
    }

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    static void AwakePostfixPatch()
    {
        Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
    }
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    static void UpdatePatch()
    {
        if (Time.realtimeSinceStartup >= nextUpdate)
        {
            foreach (Type type in checkedTypes.Keys.ToList())
            {
                List<EnemyAI> listChecked = checkedTypes[type];
                NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(type, ref listChecked);
            }
            checkedTypes.Clear();
            nextUpdate = Time.realtimeSinceStartup + updateListInterval;
        }
    }

    public static bool RequestUpdate(EnemyAI instance)
    {
        if (!checkedTypes.ContainsKey(instance.GetType()) && instance.IsOwner)
        {
            checkedTypes.Add(instance.GetType(), new List<EnemyAI>());
            if (logUnspecified && logSpam) Script.Logger.Log(LogLevel.Message,$"/RoundManager/ request was Accepted. Requested by {LibraryCalls.DebugStringHead(instance)} at {Time.realtimeSinceStartup}");
            return true;
        }
        else
        {
            if (logUnspecified && logSpam && !instance.IsOwner) Script.Logger.Log(LogLevel.Debug,"/RoundManager/ request was Denied. Not owner of the instance.");
            else if (logUnspecified && logSpam) Script.Logger.Log(LogLevel.Debug,$"/RoundManager/ request was Denied. Requested by {LibraryCalls.DebugStringHead(instance)} at {Time.realtimeSinceStartup}");
            return false;
        }
    }

    public static void ScheduleGlobalListUpdate(EnemyAI instance, ref List<EnemyAI> list)
    {
        if (checkedTypes.ContainsKey(instance.GetType()))
        {
            checkedTypes[instance.GetType()] = list;
        }
        if (!NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists.ContainsKey(instance.GetType()))
        {
            List<EnemyAI> tempList = checkedTypes[instance.GetType()];
            Script.Logger.Log(LogLevel.Warning,LibraryCalls.DebugStringHead(instance) + " global enemy list for this enemy does not exist! Creating a new one.");
            NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(instance.GetType(), ref tempList);
        }
    }
}
