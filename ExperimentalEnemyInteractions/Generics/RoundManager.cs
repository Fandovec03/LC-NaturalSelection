using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using NaturalSelection.EnemyPatches;
using UnityEngine;

namespace NaturalSelection.Generics
{
    [HarmonyPatch(typeof(RoundManager))]
    class RoundManagerPatch
    {
        static float nextUpdate = 0;
        static Dictionary<Type, List<EnemyAI>> checkedTypes = new Dictionary<Type, List<EnemyAI>>();
        public static float updateListInterval = 1f;
        static bool logSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool logUnspecified = Script.BoundingConfig.debugUnspecified.Value;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void UpdatePatch()
        {
            if (Time.realtimeSinceStartup >= nextUpdate)
            {
                foreach (Type type in checkedTypes.Keys.ToList())
                {
                    NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(type, checkedTypes[type]);
                }
                checkedTypes.Clear();
                nextUpdate = Time.realtimeSinceStartup + updateListInterval;
            }
        }

        public static bool RequestUpdate(EnemyAI instance)
        {
            if (!checkedTypes.ContainsKey(instance.GetType()))
            {
                checkedTypes.Add(instance.GetType(), new List<EnemyAI>());
                if (logUnspecified && logSpam) Script.Logger.LogMessage("/RoundManager/ request was Accepted. Requested by " + EnemyAIPatch.DebugStringHead(instance) + " at " + Time.realtimeSinceStartup);
                return true;
            }
            else
            {
                if (logUnspecified && logSpam) Script.Logger.LogInfo("/RoundManager/ request was Denied. Requested by " + EnemyAIPatch.DebugStringHead(instance) + " at " + Time.realtimeSinceStartup);
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
                Script.Logger.LogError(EnemyAIPatch.DebugStringHead(instance) + "global enemy list for this enemy does not exist! Creating a new one.");
                NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(instance.GetType(), checkedTypes[instance.GetType()]);
            }
        }
    }
}
