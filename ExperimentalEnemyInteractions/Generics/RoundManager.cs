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
        static Dictionary<KeyValuePair<Type, bool>, List<EnemyAI>> checkedTypes = new Dictionary<KeyValuePair<Type, bool>, List<EnemyAI>>();
        public static float updateListInterval = 1f;
        static bool logSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool logUnspecified = Script.BoundingConfig.debugUnspecified.Value;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void UpdatePatch()
        {
            if (Time.realtimeSinceStartup >= nextUpdate)
            {
                foreach (KeyValuePair<Type, bool> pair in checkedTypes.Keys.ToList())
                {
                    NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(pair, checkedTypes[pair]);
                }
                checkedTypes.Clear();
                nextUpdate = Time.realtimeSinceStartup + updateListInterval;
            }
        }

        public static bool RequestUpdate(EnemyAI instance)
        {
            if (!checkedTypes.ContainsKey(new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside)) && instance.IsOwner)
            {
                checkedTypes.Add(new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside), new List<EnemyAI>());
                if (logUnspecified && logSpam) Script.Logger.LogMessage("/RoundManager/ request was Accepted. Requested by " + EnemyAIPatch.DebugStringHead(instance) + " at " + Time.realtimeSinceStartup);
                return true;
            }
            else
            {
                if (logUnspecified && logSpam && !instance.IsOwner) Script.Logger.LogDebug("/RoundManager/ request was Denied. Not owner of the instance.");
                else if (logUnspecified && logSpam) Script.Logger.LogDebug("/RoundManager/ request was Denied. Requested by " + EnemyAIPatch.DebugStringHead(instance) + " at " + Time.realtimeSinceStartup);
                return false;
            }
        }

        public static void ScheduleGlobalListUpdate(EnemyAI instance, List<EnemyAI> list)
        {
            if (checkedTypes.ContainsKey(new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside)))
            {
                checkedTypes[new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside)] = list;
            }
            if (!NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists.ContainsKey(new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside)))
            {
                Script.Logger.LogWarning(EnemyAIPatch.DebugStringHead(instance) + "global enemy list for this enemy does not exist! Creating a new one.");
                NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside), checkedTypes[new KeyValuePair<Type, bool>(instance.GetType(), instance.isOutside)]);
            }
        }
    }
}
