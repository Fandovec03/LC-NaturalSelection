
using System.Collections.Generic;
using EnhancedMonsters.Monobehaviours;
using HarmonyLib;
using UnityEngine;
using CleaningCompany;
using LogLevel = BepInEx.Logging.LogLevel;
using NaturalSelection.Generics;
using NaturalSelection.EnemyPatches;

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
            Script.LogNS(LogLevel.Message, $"Triggered start of dead body {instance.gameObject.name}");

            if (!RoundManagerPatch.deadEnemiesList.Contains(instance))
            {
                RoundManagerPatch.deadEnemiesList.Add(instance);
            }
        }

        private void OnDestroy()
        {
            Script.LogNS(LogLevel.Message, $"Removing DeadBodyTrackerScript {instance.gameObject.name} from list");
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

            Script.LogNS(LogLevel.Warning,"Fired compatibility for EnhancedMonsters");
            
            if (!__instance.gameObject.GetComponentInChildren<DeadBodyTrackerScript>())
            {
                __instance.gameObject.AddComponent<DeadBodyTrackerScript>();
                Script.LogNS(LogLevel.Message,$"Successfully added script to {__instance.enemyType.enemyName} | EnhancedMonstersTraceScript");
            }
            else
            {
                Script.LogNS(LogLevel.Warning, $"There is already compoment in {__instance.enemyType.enemyName}");
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

                Script.LogNS(LogLevel.Message, $"Added DeadBodyTrackerScript to {pair.Value} at {pair.Key}", item);

            }
        }
    }
}
