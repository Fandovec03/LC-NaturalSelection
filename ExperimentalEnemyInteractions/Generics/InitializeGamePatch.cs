using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NaturalSelection.Generics
{
    class InitializeGamePatch
    {
        private static bool finishedLoading = false;

        public static List<EnemyAI> loadedEnemyList = Resources.FindObjectsOfTypeAll<EnemyAI>().ToList();

        public static List<string> speedModifierList = Script.BoundingConfig.speedModifierList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public static Dictionary<string, float> speedModifierDictionay = new Dictionary<string, float>();

        [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.SendToNextScene))]
        [HarmonyPostfix]
        public static void AwakePatch()
        {
            if (!finishedLoading)
            {

                foreach (var item in speedModifierList)
                {
                    try
                    {
                        string itemName = item.Split(":")[0];
                        float itemSpeed = float.Parse(item.Split(":")[1].Replace(".", ","));
                        speedModifierDictionay.Add(itemName, itemSpeed);
                        Script.Logger.LogDebug("Found " + itemName + ", " + itemSpeed);
                    }
                    catch (Exception e)
                    {
                        Script.Logger.LogError("Failed to add enemy into speedModifierDictionary");
                        Script.Logger.LogError(item);
                        Script.Logger.LogError(item.Split(":")[0]);
                        Script.Logger.LogError(item.Split(":")[1]);
                        Script.Logger.LogError(e);
                        continue;
                    }
                }

                foreach (var item in loadedEnemyList)
                {
                    string itemName = item.enemyType.enemyName;
                    Script.Logger.LogInfo("Checking enemy: " + itemName);
                    if (!speedModifierDictionay.Keys.Contains(itemName))
                    {
                        Script.Logger.LogInfo("Generating web speed modifier for " + itemName);
                        speedModifierDictionay.Add(itemName, 1);
                    }
                }

                string finalSpeedModifierString = "";
                foreach (var entry in speedModifierDictionay)
                {
                    finalSpeedModifierString = finalSpeedModifierString + entry.Key + ":" + entry.Value + ",";
                }
                Script.BoundingConfig.speedModifierList.Value = finalSpeedModifierString;

                Script.Logger.LogInfo("Finished loading enemy types");
            }

            finishedLoading = true;
        }
    }
}