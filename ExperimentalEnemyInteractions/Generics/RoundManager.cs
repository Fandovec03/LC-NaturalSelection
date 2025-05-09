using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using NaturalSelection.EnemyPatches;
using NaturalSelection.Experimental;
using UnityEngine;

namespace NaturalSelection.Generics
{
    [HarmonyPatch(typeof(RoundManager))]
    class RoundManagerPatch
    {
        static float nextUpdate = 0;
        static float updateTime = 0;
        static Dictionary<Type, List<EnemyAI>> checkedTypes = new Dictionary<Type, List<EnemyAI>>();
        public static float updateListInterval = 1f;
        static bool logSpam = Script.Bools["spammyLogs"];
        static bool logUnspecified = Script.Bools["debugUnspecified"];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugUnspecified") logUnspecified = value;
            if (entryKey == "spammyLogs") logSpam = value;
            //Script.Logger.LogMessage($"RoundManager received event. logUnspecified = {logUnspecified}, logSpam = {logSpam}");
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
                List<EnemyAI> listChecked;
                foreach (Type type in checkedTypes.Keys.ToList())
                {
                    listChecked = checkedTypes[type];
                    NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(type, ref listChecked);
                }
                checkedTypes.Clear();
                nextUpdate = Time.realtimeSinceStartup + updateListInterval;
            }
            /*
            if (EnhancedMonstersPatch.deadEnemiesList.Count > 0 && updateTime < Time.realtimeSinceStartup)
            {
                Script.Logger.LogMessage("Items in deadEnemiesList = " + EnhancedMonstersPatch.deadEnemiesList.Count);
                updateTime = Time.realtimeSinceStartup + 1f;

                List<GameObject> temp = new List<GameObject>(EnhancedMonstersPatch.deadEnemiesList);

                for (int i = 0; i < temp.Count; i++)
                {
                    try
                    {
                        if (temp[i] == null)
                        {
                            Script.Logger.LogWarning($"Item in deadEnemiesList is null. Removing at {i}");
                            EnhancedMonstersPatch.deadEnemiesList.RemoveAt(i);
                        }
                    }
                    catch
                    {
                        Script.Logger.LogWarning($"Catch > Failed to get item. Removing at {i}");
                        EnhancedMonstersPatch.deadEnemiesList.RemoveAt(i);
                    }
                }

            }*/

        }

        public static bool RequestUpdate(EnemyAI instance)
        {
            if (!checkedTypes.ContainsKey(instance.GetType()) && instance.IsOwner)
            {
                checkedTypes.Add(instance.GetType(), new List<EnemyAI>());
                if (logUnspecified && logSpam) Script.Logger.LogMessage($"/RoundManager/ request was Accepted. Requested by {LibraryCalls.DebugStringHead(instance)} at {Time.realtimeSinceStartup}");
                return true;
            }
            else
            {
                if (logUnspecified && logSpam && !instance.IsOwner) Script.Logger.LogDebug("/RoundManager/ request was Denied. Not owner of the instance.");
                else if (logUnspecified && logSpam) Script.Logger.LogDebug($"/RoundManager/ request was Denied. Requested by {LibraryCalls.DebugStringHead(instance)} at {Time.realtimeSinceStartup}");
                return false;
            }
        }

        public static void ScheduleGlobalListUpdate(EnemyAI instance, List<EnemyAI> list)
        {
            if (checkedTypes.ContainsKey(instance.GetType()))
            {
                checkedTypes[instance.GetType()] = list;
            }
            if (!NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists.ContainsKey(instance.GetType()))
            {
                Script.Logger.LogWarning(LibraryCalls.DebugStringHead(instance) + "global enemy list for this enemy does not exist! Creating a new one.");
                List<EnemyAI> tempList = checkedTypes[instance.GetType()];
                NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(instance.GetType(), ref tempList);
            }
        }
    }
}
