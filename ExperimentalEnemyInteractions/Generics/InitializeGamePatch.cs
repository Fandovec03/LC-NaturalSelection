using BepInEx;
using BepInEx.Logging;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using HarmonyLib;
using Mono.Cecil;
using NaturalSelection;
using NaturalSelection.EnemyPatches;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NaturalSelection.Generics
{
    class InitializeGamePatch
    {
        private static bool finishedLoading = false;
        static List<string> loadedEnemyNamesFromConfig = new List<string>();

        static List<string> beeBlacklistList = Script.BoundingConfig.beeBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> blobBlacklistList = Script.BoundingConfig.blobBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> sandwormBlacklistList = Script.BoundingConfig.sandwormBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderWebBlacklistList = Script.BoundingConfig.spiderWebBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> speedModifierList = Script.BoundingConfig.speedModifierList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderBlacklistList = Script.BoundingConfig.spiderBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> customSizeOverrideListList = Script.BoundingConfig.customSizeOverrideList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        static Dictionary<string, bool> beeBlacklistrDictionay = new Dictionary<string, bool>();
        static Dictionary<string, bool> blobBlacklistDictionay = new Dictionary<string, bool>();
        static Dictionary<string, bool> sandwormBlacklistDictionay = new Dictionary<string, bool>();
        static Dictionary<string, bool> spiderWebBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, float> speedModifierDictionay = new Dictionary<string, float>();
        static Dictionary<string, bool> spiderBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, int> customSizeOverrideListDictionary = new Dictionary<string, int>();

        public static List<string> beeBlacklistFinal = new List<string>();
        public static List<string> blobBlacklistFinal = new List<string>();
        public static List<string> sandwormBlacklistFinal = new List<string>();
        public static List<string> spiderWebBlacklistFinal = new List<string>();
        public static List<string> spiderBlacklistFinal = new List<string>();
        public static List<EnemyAI> tryFindLater = new List<EnemyAI>();

        [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Start))]
        [HarmonyPostfix]
        public static void InitializeGameStartPatch()
        {
            Script.loadedEnemyList = Resources.FindObjectsOfTypeAll<EnemyAI>().ToList();
            if (!finishedLoading)
            {
                Script.Logger.Log(LogLevel.Message,$"Reading/Checking/Writing entries for enemies.");
                ReadConfigLists();
                CheckConfigLists(Script.loadedEnemyList);
                WriteToConfigLists();
            }

            LibraryCalls.SubscribeToConfigChanges();
            Networking.SubscribeToConfigChanges();

            finishedLoading = true;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPostfix]
        public static void StartOfRoundPatch()
        {
            if (tryFindLater.Count > 0)
            {
                Script.Logger.Log(LogLevel.Message,$"Secondary list is not empty. Checking/Writing entries for skipped enemies");
                try
                {
                    CheckConfigLists(tryFindLater);
                    WriteToConfigLists();
                    tryFindLater.Clear();
                }
                catch
                {
                Script.Logger.Log(LogLevel.Error,$"An error has occured while working with the list.");
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
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemSpeed}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into speedModifierDictionary");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in beeBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    if (itemValue == true) beeBlacklistFinal.Add(itemName);
                    beeBlacklistrDictionay.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into beeBlacklistrDictionay");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in blobBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    if (itemValue == true) blobBlacklistFinal.Add(itemName);
                    blobBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into blobBlacklistDictionay");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in sandwormBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    if (itemValue == true) sandwormBlacklistFinal.Add(itemName);
                    sandwormBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into sandwormBlacklistDictionay");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in spiderWebBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    if (itemValue == true) spiderWebBlacklistFinal.Add(itemName);
                    spiderWebBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into spiderWebBlacklistDictionay");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in spiderBlacklistList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    bool itemValue = bool.Parse(item.Split(":")[1]);
                    if (itemValue == true) spiderBlacklistFinal.Add(itemName);
                    spiderBlacklistDictionay.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug,$"Found {itemName}, {itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into spiderBlacklistDictionay");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error,item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in customSizeOverrideListList)
            {
                try
                {
                    string itemName = item.Split(":")[0];
                    int itemValue = int.Parse(item.Split(":")[1]);
                    if (itemValue < 0 || itemValue > 5)
                    {
                        Script.Logger.Log(LogLevel.Error, $"Invalid size value {itemValue}. Defaulting to (0 || {(CustomEnemySize)0})");
                        itemValue = 0;
                    }
                    customSizeOverrideListDictionary.Add(itemName, itemValue);
                    Script.Logger.Log(LogLevel.Debug, $"Found {itemName}, {(CustomEnemySize)itemValue}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error, "Failed to add enemy into customSizeOverrideListList");
                    Script.Logger.Log(LogLevel.Error, item);
                    Script.Logger.Log(LogLevel.Error, item.Split(":")[0]);
                    Script.Logger.Log(LogLevel.Error, item.Split(":")[1]);
                    Script.Logger.Log(LogLevel.Error, e);
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
                    Script.Logger.Log(LogLevel.Warning,$"Failed to get enemy name from {item.name}. Adding to list for 2nd attempt.");
                    tryFindLater.Add(item);
                    continue;
                }

                if (customSizeOverrideListList.Count < 1)
                {
                    switch (item.enemyType.EnemySize)
                    {
                        case EnemySize.Tiny:
                            {
                                if (!item.enemyType.canDie) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Undefined; break; }
                                if (item is FlowerSnakeEnemy || item is DoublewingAI || item is CentipedeAI) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Tiny; break; }
                                else if (item.enemyHP <= 3) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Small; break; }
                                else if (item.enemyHP <= 12) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Medium; break; }
                                else if (item.enemyHP <= 30) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Large; break; }
                                else if (item.enemyHP > 30) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Giant; break; }
                                else customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Undefined;
                                break;
                            }
                        case EnemySize.Medium:
                            {
                                customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Medium;
                                break;
                            }
                        case EnemySize.Giant:
                            {
                                customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Giant;
                                break;
                            }

                        default:
                            customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Undefined;
                            break;
                    }
                }

                if (!loadedEnemyNamesFromConfig.Contains(itemName))
                {
                    loadedEnemyNamesFromConfig.Add(itemName);
                }
            }
            foreach(var itemName in loadedEnemyNamesFromConfig)
            {
                Script.Logger.Log(LogLevel.Info,$"Checking config entries for enemy: {itemName}");

                if (!speedModifierDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new web speed modifier entry for {itemName}");
                    speedModifierDictionay.Add(itemName, 1);
                }
                if (!beeBlacklistrDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new bee blacklist entry for {itemName}");
                    bool value = false;

                    if (beeBlacklistrDictionay.Keys.Contains("Earth Leviathan") || beeBlacklistrDictionay.Keys.Contains("Docile Locust Bees"))
                    {
                        value = true;
                    }
                    beeBlacklistrDictionay.Add(itemName, value);
                }
                if (!blobBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new blob blacklist entry for {itemName}");
                    blobBlacklistDictionay.Add(itemName, false);
                }
                if (!sandwormBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new Sandworm blacklist entry for {itemName}");
                    sandwormBlacklistDictionay.Add(itemName, false);
                }
                if (!spiderWebBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new spider web blacklist entry for {itemName}");
                    spiderWebBlacklistDictionay.Add(itemName, false);
                }
                if (!spiderBlacklistDictionay.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new spider blacklist entry for {itemName}");
                    spiderBlacklistDictionay.Add(itemName, false);
                }
                if (!customSizeOverrideListDictionary.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug, $"Generating new custom enemy size entry for {itemName}");
                    customSizeOverrideListDictionary.Add(itemName, 0);
                }
            }
            if (Script.BoundingConfig.debugBool.Value == true)
            {
                foreach(var item in beeBlacklistFinal)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking final blacklist {nameof(beeBlacklistFinal)} -> {item}");
                }
                foreach (var item in sandwormBlacklistFinal)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking final blacklist {nameof(sandwormBlacklistFinal)} -> {item}");
                }
                foreach (var item in spiderWebBlacklistFinal)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking final blacklist {nameof(spiderWebBlacklistFinal)} -> {item}");
                }
                foreach (var item in spiderBlacklistFinal)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking final blacklist {nameof(spiderBlacklistFinal)} -> {item}");
                }
                foreach (var item in speedModifierDictionay)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking final speed modifier list {nameof(speedModifierDictionay)} -> {item.Key}, {item.Value}");
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
            string customSizeOverrideListFinal = "";

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

                foreach(var entry in customSizeOverrideListDictionary)
                {
                    customSizeOverrideListFinal = $"{customSizeOverrideListFinal}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.customSizeOverrideList.Value = customSizeOverrideListFinal;

                Script.Logger.Log(LogLevel.Info,"Finished generating configucations.");

            }
            catch(Exception e)
            {
                Script.Logger.Log(LogLevel.Error,"Failed to generate configucations.");
                Script.Logger.Log(LogLevel.Error,e);
            }
        }
    }
}