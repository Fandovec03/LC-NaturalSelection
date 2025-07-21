using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NaturalSelection.Generics;
    class InitializeGamePatch
    {
        private static bool finishedLoading = false;
        static List<string> loadedEnemyNamesFromConfig = new List<string>();
        static List<string> beeBlacklistLoaded = Script.BoundingConfig.beeBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> blobBlacklistLoaded = Script.BoundingConfig.blobBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> sandwormBlacklistLoaded = Script.BoundingConfig.sandwormBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderWebBlacklistLoaded = Script.BoundingConfig.spiderWebBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> speedModifierLoaded = Script.BoundingConfig.speedModifierList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> spiderBlacklistLoaded = Script.BoundingConfig.spiderBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        static List<string> customSizeOverrideListLoaded = Script.BoundingConfig.customSizeOverrideList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        internal static List<string> beeBlacklist = new List<string>();
        internal static List<string> blobBlacklist = new List<string>();
        internal static List<string> sandwormBlacklist = new List<string>();
        internal static List<string> spiderWebBlacklist = new List<string>();
        internal static List<string> speedModifier = new List<string>();
        internal static List<string> spiderBlacklist = new List<string>();
        internal static List<string> customSizeOverrideList = new List<string>();

        public static Dictionary<string, float> speedModifierDictionay = new Dictionary<string, float>();
        public static Dictionary<string, int> customSizeOverrideListDictionary = new Dictionary<string, int>();
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
            foreach (var item in speedModifierLoaded)
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

            foreach (var item in beeBlacklistLoaded)
            {
                try
                {
                    beeBlacklist.Add(item);
                    Script.Logger.Log(LogLevel.Debug,$"Found {item}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into beeBlacklist");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in blobBlacklistLoaded)
            {
                try
                {
                    blobBlacklist.Add(item);
                    Script.Logger.Log(LogLevel.Debug,$"Found {item}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into blobBlacklist");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in sandwormBlacklistLoaded)
            {
                try
                {
                    sandwormBlacklist.Add(item);
                    Script.Logger.Log(LogLevel.Debug,$"Found {item}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into sandwormBlacklist");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in spiderWebBlacklistLoaded)
            {
                try
                {
                    spiderWebBlacklist.Add(item);
                    Script.Logger.Log(LogLevel.Debug,$"Found {item}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into spiderWebBlacklist");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in spiderBlacklistLoaded)
            {
                try
                {
                    spiderBlacklist.Add(item);
                    Script.Logger.Log(LogLevel.Debug,$"Found {item}");
                }
                catch (Exception e)
                {
                    Script.Logger.Log(LogLevel.Error,"Failed to add enemy into spiderBlacklist");
                    Script.Logger.Log(LogLevel.Error,item);
                    Script.Logger.Log(LogLevel.Error,e);
                    continue;
                }
            }

            foreach (var item in customSizeOverrideListLoaded)
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

                if (customSizeOverrideListLoaded.Count < 1)
                {
                    switch (item.enemyType.EnemySize)
                    {
                        case EnemySize.Tiny:
                            {
                                if (!item.enemyType.canDie) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Undefined; break; }
                                if (item is FlowerSnakeEnemy || item is DoublewingAI || item is RedLocustBees || item is DocileLocustBeesAI || item is ButlerBeesEnemyAI) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Tiny; break; }
                                else if (item.enemyHP <= 3) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Small; break; }
                                else if (item.enemyHP <= 15) { customSizeOverrideListDictionary[itemName] = (int)CustomEnemySize.Medium; break; }
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
                if (beeBlacklist.Count <= 0)
                {
                    Script.Logger.Log(LogLevel.Debug,$"Generating new bee blacklist entry for {itemName}");
                    if (beeBlacklist.Contains("Earth Leviathan") || beeBlacklist.Contains("Docile Locust Bees"))
                    {
                        beeBlacklist.Add(itemName);
                    }
                }
                if (!customSizeOverrideListDictionary.Keys.Contains(itemName))
                {
                    Script.Logger.Log(LogLevel.Debug, $"Generating new custom enemy size entry for {itemName}");
                    customSizeOverrideListDictionary.Add(itemName, 0);
                }
            }
            if (Script.BoundingConfig.debugBool.Value == true)
            {
                foreach(var item in beeBlacklist)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking blacklist {nameof(beeBlacklist)} -> {item}");
                }
                foreach (var item in sandwormBlacklist)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking blacklist {nameof(sandwormBlacklist)} -> {item}");
                }
                foreach (var item in spiderWebBlacklist)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking blacklist {nameof(spiderWebBlacklist)} -> {item}");
                }
                foreach (var item in spiderBlacklist)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking blacklist {nameof(spiderBlacklist)} -> {item}");
                }
                foreach (var item in speedModifierDictionay)
                {
                    Script.Logger.Log(LogLevel.Debug,$"checking speed modifier dictionary {nameof(speedModifierDictionay)} -> {item.Key}, {item.Value}");
                }
            }
        }

        public static void WriteToConfigLists()
        {
            string finalEnemyNamesString = "";
            string finalSpeedModifierString = "";
            string finalBeeBlacklistString = "";
            string finalBlobBlacklistString = "";
            string finalSandWormBlacklistString = "";
            string finalSpiderWebBlacklistString = "";
            string finalSpiderBlacklistString = "";
            string customSizeOverrideListFinal = "";

            try
            {
                foreach (var entry in loadedEnemyNamesFromConfig)
                {
                    finalEnemyNamesString = $"{finalEnemyNamesString}{entry},";
                }
                Script.BoundingConfig.enemyNames.Value = finalEnemyNamesString;

                foreach (var entry in speedModifierDictionay)
                {
                    finalSpeedModifierString = $"{finalSpeedModifierString}{entry.Key}:{entry.Value},";
                }
                Script.BoundingConfig.speedModifierList.Value = finalSpeedModifierString;

                foreach (var entry in beeBlacklist)
                {
                    finalBeeBlacklistString = $"{finalBeeBlacklistString}{entry},";
                }
                Script.BoundingConfig.beeBlacklist.Value = finalBeeBlacklistString;

                foreach (var entry in blobBlacklist)
                {
                    finalBlobBlacklistString = $"{finalBlobBlacklistString}{entry},";
                }
                Script.BoundingConfig.blobBlacklist.Value = finalBlobBlacklistString;

                foreach (var entry in sandwormBlacklist)
                {
                    finalSandWormBlacklistString = $"{finalSandWormBlacklistString}{entry},";
                }
                Script.BoundingConfig.sandwormBlacklist.Value = finalSandWormBlacklistString;

                foreach (var entry in spiderWebBlacklist)
                {
                    finalSpiderWebBlacklistString = $"{finalSpiderWebBlacklistString}{entry},";
                }
                Script.BoundingConfig.spiderWebBlacklist.Value = finalSpiderWebBlacklistString;

                foreach (var entry in spiderBlacklist)
                {
                    finalSpiderBlacklistString = $"{finalSpiderBlacklistString}{entry},";
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