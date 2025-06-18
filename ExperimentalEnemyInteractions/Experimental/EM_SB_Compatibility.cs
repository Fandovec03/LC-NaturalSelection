
using System.Collections.Generic;
using EnhancedMonsters.Monobehaviours;
using HarmonyLib;
using UnityEngine;
using CleaningCompany;
using LogLevel = BepInEx.Logging.LogLevel;
using NaturalSelection.Generics;

namespace NaturalSelection.Compatibility
{
    public class DeadBodyTrackerScript : MonoBehaviour
    {
        GameObject instance;

        public DeadBodyTrackerScript()
        {
            instance = gameObject;
        }

        private void Start()
        {
            instance = gameObject;
            Script.Logger.Log(LogLevel.Message, $"Triggered start of dead body {instance.gameObject.name}");

            if (!RoundManagerPatch.deadEnemiesList.Contains(instance))
            {
                RoundManagerPatch.deadEnemiesList.Add(instance);
            }
        }

        private void OnDestroy()
        {
            Script.Logger.Log(LogLevel.Message, $"Removing DeadBodyTrackerScript {instance.gameObject.name} from list");
            RoundManagerPatch.deadEnemiesList.Remove(instance);
        }
    }

    public class EnhancedMonstersCompatibility
    {
        [HarmonyBefore("com.velddev.enhancedmonsters")]
        [HarmonyPatch(typeof(EnemyScrap), "Start")]
        [HarmonyPrefix]
        static void EM_EnemyScrapStartPatch(EnemyScrap __instance)
        {

            Script.Logger.LogWarning("Fired compatibility for EnhancedMonsters");
            
            if (!__instance.gameObject.GetComponentInChildren<DeadBodyTrackerScript>())
            {
                __instance.gameObject.AddComponent<DeadBodyTrackerScript>();
                Script.Logger.LogMessage($"Successfully added script to {__instance.enemyType.enemyName} | EnhancedMonstersTraceScript");
            }
            else
            {
                Script.Logger.Log(LogLevel.Warning, $"There is already compoment in {__instance.enemyType.enemyName}");
            }
        }
    }

    internal class SellBodiesFixedCompatibility
    {
        public static void AddTracerScriptToPrefabs()
        {
            Script.Logger.LogWarning("Fired compatibility for SellBodiesFixed");

            foreach (KeyValuePair<string, string> pair in Plugin.instance.pathToName)
            {
                Item item = Plugin.instance.bundle.LoadAsset<Item>(pair.Key);

                item.spawnPrefab.gameObject.AddComponent<DeadBodyTrackerScript>();

                Script.Logger.Log(LogLevel.Message, $"Added DeadBodyTrackerScript to {pair.Value} at {pair.Key}");

            }
        }
    }
}
