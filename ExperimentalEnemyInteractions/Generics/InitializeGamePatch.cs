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

        static List<EnemyAI> loadedEnemyList = Resources.FindObjectsOfTypeAll<EnemyAI>().ToList();
        static List<string> loadedEnemyNamesFromConfig = new List<string>();

        static List<string> beeBlacklistList = Script.BoundingConfig.beeBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> blobBlacklistList = Script.BoundingConfig.blobBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> sandwormBlacklistList = Script.BoundingConfig.sandwormBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderWebBlacklistList = Script.BoundingConfig.spiderWebBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> speedModifierList = Script.BoundingConfig.speedModifierList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderBlacklistList = Script.BoundingConfig.spiderBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        public static Dictionary<string, bool> beeBlacklistrDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> blobBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> sandwormBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> spiderWebBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, float> speedModifierDictionay = new Dictionary<string, float>();
        public static Dictionary<string, bool> spiderBlacklistDictionay = new Dictionary<string, bool>();

        public static List<EnemyAI> tryFindLater = new List<EnemyAI>();

        [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Start))]
        [HarmonyPostfix]
        public static void InitializeGameStartPatch()
        {
            if (!finishedLoading)
            {
                Script.Logger.LogMessage($"Reading/Checking/Writing entries for enemies.");
                ReadConfigLists();
                CheckConfigLists(loadedEnemyList);
                WriteToConfigLists();
            }

            finishedLoading = true;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPostfix]
        public static void StartOfRoundPatch()
        {
            if (tryFindLater.Count > 0)
            {
                Script.Logger.LogMessage($"Secondary list is not empty. Checking/Writing entries for skipped enemies");
                try
                {
                    CheckConfigLists(tryFindLater);
                    WriteToConfigLists();
                    tryFindLater.Clear();
                }
                catch
                {
                Script.Logger.LogError($"An error has occured while working with the list.");
                tryFindLater.Clear();
                }
            }
        }

        public static void ReadConfigLists()
        {
            foreach (var item in speedModifierList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    float itemSpeed = float.Parse(item.Split(":")[1].Replace(".", ","));
                    speedModifierDictionay.Add(itemName, itemSpeed);
                    Script.Logger.LogDebug($"Found {itemName}, {itemSpeed}");
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

            foreach (var item in beeBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    beeBlacklistrDictionay.Add(itemName, itemValue);
                    Script.Logger.LogDebug($"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.LogError("Failed to add enemy into beeBlacklistrDictionay");
                    Script.Logger.LogError(item);
                    Script.Logger.LogError(item.Split(":")[0]);
                    Script.Logger.LogError(item.Split(":")[1]);
                    Script.Logger.LogError(e);
                    continue;
                }
            }

            foreach (var item in blobBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    blobBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.LogDebug($"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.LogError("Failed to add enemy into blobBlacklistDictionay");
                    Script.Logger.LogError(item);
                    Script.Logger.LogError(item.Split(":")[0]);
                    Script.Logger.LogError(item.Split(":")[1]);
                    Script.Logger.LogError(e);
                    continue;
                }
            }

            foreach (var item in sandwormBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    sandwormBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.LogDebug($"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.LogError("Failed to add enemy into sandwormBlacklistDictionay");
                    Script.Logger.LogError(item);
                    Script.Logger.LogError(item.Split(":")[0]);
                    Script.Logger.LogError(item.Split(":")[1]);
                    Script.Logger.LogError(e);
                    continue;
                }
            }

            foreach (var item in spiderWebBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    spiderWebBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.LogDebug($"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.LogError("Failed to add enemy into spiderWebBlacklistDictionay");
                    Script.Logger.LogError(item);
                    Script.Logger.LogError(item.Split(":")[0]);
                    Script.Logger.LogError(item.Split(":")[1]);
                    Script.Logger.LogError(e);
                    continue;
                }
            }

            foreach (var item in spiderBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    spiderBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.LogDebug($"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.LogError("Failed to add enemy into spiderBlacklistDictionay");
                    Script.Logger.LogError(item);
                    Script.Logger.LogError(item.Split(":")[0]);
                    Script.Logger.LogError(item.Split(":")[1]);
                    Script.Logger.LogError(e);
                    continue;
                }
            }
        }
        public static void CheckConfigLists(List<EnemyAI> listOfEnemies)
        {
            foreach (var item in listOfEnemies)
            {
                string itemName;
                try
                {
                    itemName = item.enemyType.enemyName;
                }
                catch
                {
                    Script.Logger.LogWarning($"Failed to get enemy name from {item.name}. Adding to list for 2nd attempt.");
                    tryFindLater.Add(item);
                    continue;
                }
                if (!loadedEnemyNamesFromConfig.Contains(itemName))
                {
                    loadedEnemyNamesFromConfig.Add(itemName);
                }
            }
            foreach(var itemName in loadedEnemyNamesFromConfig)
            {
                Script.Logger.LogInfo($"Checking config entries for enemy: {itemName}");

                if (!speedModifierDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new web speed modifier entry for {itemName}");
                    speedModifierDictionay.Add(itemName, 1);
                }
                if (!beeBlacklistrDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new bee blacklist entry for {itemName}");
                    beeBlacklistrDictionay.Add(itemName, false);
                }
                if (!blobBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new blob blacklist entry for {itemName}");
                    blobBlacklistDictionay.Add(itemName, false);
                }
                if (!sandwormBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new Sandworm blacklist entry for {itemName}");
                    sandwormBlacklistDictionay.Add(itemName, false);
                }
                if (!spiderWebBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new spider web blacklist entry for {itemName}");
                    spiderWebBlacklistDictionay.Add(itemName, false);
                }
                if (!spiderBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.LogDebug($"Generating new spider blacklist entry for {itemName}");
                    spiderBlacklistDictionay.Add(itemName, false);
                }
            }
        }

        public static void WriteToConfigLists()
        {
            string finalSpeedModifierString = "";
            string finalBeeBlacklistString = "";
            string finalBlobBlacklistString = "";
            string finalSandWormBlacklistString = "";
            string finalSpiderWebBlacklistString = "";
            string finalSpiderBlacklistString = "";

            try
            {

                foreach (var entry in speedModifierDictionay)
                {
                    finalSpeedModifierString = $"{finalSpeedModifierString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.speedModifierList.Value = finalSpeedModifierString;

                foreach (var entry in beeBlacklistrDictionay)
                {
                    finalBeeBlacklistString = $"{finalBeeBlacklistString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.beeBlacklist.Value = finalBeeBlacklistString;

                foreach (var entry in blobBlacklistDictionay)
                {
                    finalBlobBlacklistString = $"{finalBlobBlacklistString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.blobBlacklist.Value = finalBlobBlacklistString;

                foreach (var entry in sandwormBlacklistDictionay)
                {
                    finalSandWormBlacklistString = $"{finalSandWormBlacklistString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.sandwormBlacklist.Value = finalSandWormBlacklistString;

                foreach (var entry in spiderWebBlacklistDictionay)
                {
                    finalSpiderWebBlacklistString = $"{finalSpiderWebBlacklistString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.spiderWebBlacklist.Value = finalSpiderWebBlacklistString;

                foreach (var entry in spiderBlacklistDictionay)
                {
                    finalSpiderBlacklistString = $"{finalSpiderBlacklistString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.spiderBlacklist.Value = finalSpiderBlacklistString;


                Script.Logger.LogInfo("Finished generating configucations.");

            }
            catch(Exception e)
            {
                Script.Logger.LogError("Failed to generate configucations.");
                Script.Logger.LogError(e);
            }
        }
    }
}