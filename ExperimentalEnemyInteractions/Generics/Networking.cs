using System;
using System.Collections.Generic;
using LethalNetworkAPI;
using HarmonyLib;
using BepInEx.Logging;
using NaturalSelection.EnemyPatches;

namespace NaturalSelection.Generics
{
    public class Networking
    {
        public static Dictionary<string, Type> NetworkingDictionary = new Dictionary<string, Type>();
        static bool logNetworking = Script.Bools["debugNetworking"];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugNetworking") logNetworking = value;
            //Script.Logger.Log(LogLevel.Message,$"Networking received event. logNetworking = {logNetworking}");
        }
        public static void SubscribeToConfigChanges()
        {
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        public static LNetworkVariable<T> NSEnemyNetworkVariable<T>(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, typeof(T));
            return LNetworkVariable<T>.Connect(NWID);
        }

        public static LNetworkEvent NSEnemyNetworkEvent(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, typeof(LNetworkEvent));
            return LNetworkEvent.Connect(NWID);
        }

        public static void ClearSubscribtionsInDictionary()
        {
            foreach (KeyValuePair<string, Type> pair in NetworkingDictionary)
            {

                if(pair.Value == typeof(LNetworkEvent))
                {
                    if (logNetworking) Script.Logger.Log(LogLevel.Debug,$"Clearing subscriptions of event {pair.Key}");
                    LNetworkEvent.Connect(pair.Key).ClearSubscriptions(); continue;
                }
                else
                {
                    if (logNetworking) Script.Logger.Log(LogLevel.Debug,$"Disposing of network {pair.Value} {pair.Key}");

                    if (pair.Value == typeof(int)) {LNetworkVariable<int>.Connect(pair.Key).Dispose(); continue;}
                    if (pair.Value == typeof(float)) {LNetworkVariable<float>.Connect(pair.Key).Dispose(); continue;}
                    if (pair.Value == typeof(bool)) {LNetworkVariable<bool>.Connect(pair.Key).Dispose(); continue;}

                    Script.Logger.Log(LogLevel.Warning,$"Unsupported type {pair.Value}");
                }
            }
            Script.Logger.Log(LogLevel.Info,"/Networking/ Finished clearing dictionary.");
            NetworkingDictionary.Clear();
        }
    }

    class NetworkingMethods
    {
        [HarmonyPatch(typeof(GameNetworkManager), "ResetGameValuesToDefault")]
        [HarmonyPostfix]
        static void ResetGameValuesToDefaultPatch()
        {
            Script.Logger.Log(LogLevel.Info,"/ResetGameValuesToDefault/ Clearing all subscribtions, globalEnemyLists and data dictionaries.");
            Networking.ClearSubscribtionsInDictionary();
            NaturalSelectionLib.NaturalSelectionLib.ClearAllEnemyLists();
            EnemyAIPatch.enemyDataDict.Clear();
            //EnemyAIPatch.enemyDataDict2.Clear();
            //SandSpiderWebTrapPatch.spiderWebs.Clear();
        }

        [HarmonyPatch(typeof(RoundManager), "ResetEnemyVariables")]
        [HarmonyPostfix]
        static void ResetEnemyVariablesPatch()
        {
            Script.Logger.Log(LogLevel.Info, "/ResetEnemyVariables/ Clearing all subscribtions, globalEnemyLists and data dictionaries.");
            Networking.ClearSubscribtionsInDictionary();
            NaturalSelectionLib.NaturalSelectionLib.ClearAllEnemyLists();
            EnemyAIPatch.enemyDataDict.Clear();
            //EnemyAIPatch.enemyDataDict2.Clear();
            //SandSpiderWebTrapPatch.spiderWebs.Clear();
        }
    }
}
