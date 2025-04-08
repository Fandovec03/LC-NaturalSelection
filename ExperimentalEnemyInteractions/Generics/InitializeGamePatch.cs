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

        public static List<string> beeBlacklistList = Script.BoundingConfig.beeBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public static List<string> blobBlacklistList = Script.BoundingConfig.blobBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public static List<string> sandwormBlacklistList = Script.BoundingConfig.sandwormBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public static List<string> spiderWebBlacklistList = Script.BoundingConfig.spiderWebBlacklist.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        public static List<string> speedModifierList = Script.BoundingConfig.speedModifierList.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public static Dictionary<string, bool> beeBlacklistrDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> blobBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> sandwormBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, bool> spiderWebBlacklistDictionay = new Dictionary<string, bool>();
        public static Dictionary<string, float> speedModifierDictionay = new Dictionary<string, float>();


        [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Start))]
        [HarmonyPostfix]
        public static void InitializeGameStartPatch()
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

                foreach (var item in loadedEnemyList)
                {
                    try
                    {
                        string itemName = string.Empty;
                        try
                        {
                            itemName = item.enemyType.enemyName;
                        }
                        catch
                        {
                            Script.Logger.LogWarning($"Failed to get enemy name. Resorting using backup name.");
                            itemName = item.name;
                        }
                        Script.Logger.LogInfo($"Generating config entries for enemy: {itemName}");

                        if (!speedModifierDictionay.Keys.Contains(itemName))
                        {
                            Script.Logger.LogDebug($"Generating web speed modifier for {itemName}");
                            speedModifierDictionay.Add(itemName, 1);
                        }
                        if (!beeBlacklistrDictionay.Keys.Contains(itemName))
                        {
                            Script.Logger.LogDebug($"Generating bee blacklist for {itemName}");
                            beeBlacklistrDictionay.Add(itemName, false);
                        }
                        if (!blobBlacklistDictionay.Keys.Contains(itemName))
                        {
                            Script.Logger.LogDebug($"Generating blob blacklist for {itemName}");
                            blobBlacklistDictionay.Add(itemName, false);
                        }
                        if (!sandwormBlacklistDictionay.Keys.Contains(itemName))
                        {
                            Script.Logger.LogDebug($"Generating Sandworm blacklist for {itemName}");
                            sandwormBlacklistDictionay.Add(itemName, false);
                        }
                        if (!spiderWebBlacklistDictionay.Keys.Contains(itemName))
                        {
                            Script.Logger.LogDebug($"Generating spider web for {itemName}");
                            spiderWebBlacklistDictionay.Add(itemName, false);
                        }
                    }
                    catch (Exception e)
                    {
                        Script.Logger.LogError(e);
                        continue;
                    }
                }

                string finalSpeedModifierString = "";
                string finalBeeBlacklistString = "";
                string finalBlobBlacklistString = "";
                string finalSandWormBlacklistString = "";
                string finalSpiderWebBlacklistString = "";

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


                    Script.Logger.LogInfo("Finished generating configucations.");

                }
                catch(Exception e)
                {
                    Script.Logger.LogError("Failed to generate configucations.");
                    Script.Logger.LogError(e);
                }
            }

            finishedLoading = true;
        }
    }
}