using System.Collections.Generic;
using CleaningCompany;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using LogLevel = BepInEx.Logging.LogLevel;

namespace NaturalSelection.Compatibility
{

    internal class SellBodiesTraceScript : MonoBehaviour
    {
        GameObject instance;

        public SellBodiesTraceScript()
        {
            instance = this.gameObject;
        }

        private void Awake()
        {
            instance = this.gameObject;
            Script.Logger.Log(LogLevel.Message,$"Successfully added SellBodiesTraceScript of {instance.name}");

            if (!SellBodiesFixedPatch.deadEnemyBodies.Contains(instance))
            {
                SellBodiesFixedPatch.deadEnemyBodies.Add(instance);
            }
        }

        private void OnDestroy()
        {
            Script.Logger.Log(LogLevel.Message,$"Removing SellBodiesTraceScript {instance.name} from list");
            SellBodiesFixedPatch.deadEnemyBodies.Remove(instance);
        }
    }


    internal class SellBodiesFixedPatch
    {
        public static List<GameObject> deadEnemyBodies = new List<GameObject>();

        public static void AddTracerScriptToPrefabs()
        {
            foreach (KeyValuePair<string,string> pair in Plugin.instance.pathToName)
            {
                Item item = Plugin.instance.bundle.LoadAsset<Item>(pair.Key);

                item.spawnPrefab.gameObject.AddComponent<SellBodiesTraceScript>();

                Script.Logger.Log(LogLevel.Message,$"Added SellBodiesTracerScript to {pair.Value} at {pair.Key}");

            }
        }
    }
}
