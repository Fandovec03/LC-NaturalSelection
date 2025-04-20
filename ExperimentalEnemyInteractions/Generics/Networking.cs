using System;
using System.Collections.Generic;
using System.Text;
using LethalNetworkAPI;
using HarmonyLib;

namespace NaturalSelection.Generics
{
    public class Networking
    {
        public static Dictionary<string, int> NetworkingDictionary = new Dictionary<string, int>();
        static bool logNetworking = Script.Bools["debugNetworking"];

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugNetworking") logNetworking = value;
            //Script.Logger.LogMessage($"Networking received event. logNetworking = {logNetworking}");
        }
        public static void SubscribeToConfigChanges()
        {
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }


        public static LNetworkVariable<float> NSEnemyNetworkVariableFloat(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, 31);
            return LNetworkVariable<float>.Connect(NWID);
        }

        public static LNetworkVariable<int> NSEnemyNetworkVariableInt(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, 32);
            return LNetworkVariable<int>.Connect(NWID);
        }

        public static LNetworkVariable<bool> NSEnemyNetworkVariableBool(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, 33);
            return LNetworkVariable<bool>.Connect(NWID);
        }

        public static LNetworkEvent NSEnemyNetworkEvent(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, 2);
            return LNetworkEvent.Connect(NWID);
        }

        public static void ClearSubscribtionsInDictionary()
        {
            foreach (KeyValuePair<string, int> pair in NetworkingDictionary)
            {
                switch (pair.Value)
                {
                    case 2:
                        {
                            if (logNetworking) Script.Logger.LogDebug($"Clearing subscriptions of event {pair.Key}");
                            LNetworkEvent.Connect(pair.Key).ClearSubscriptions(); break;
                        }
                    case 31:
                        {
                            if (logNetworking) Script.Logger.LogDebug($"Disposing of network float {pair.Key}");
                            LNetworkVariable<float>.Connect(pair.Key).Dispose(); break;
                        }
                    case 32:
                        {
                            if (logNetworking) Script.Logger.LogDebug($"Disposing of network int {pair.Key}");
                            LNetworkVariable<int>.Connect(pair.Key).Dispose(); break;
                        }
                    case 33:
                        {
                            if (logNetworking) Script.Logger.LogDebug($"Disposing of network bool {pair.Key}");
                            LNetworkVariable<bool>.Connect(pair.Key).Dispose(); break;
                        }
                }
            }
            Script.Logger.LogInfo("/Networking/ Finished clearing dictionary.");
            NetworkingDictionary.Clear();
        }
    }

    class NetworkingMethods
    {
        [HarmonyPatch(typeof(GameNetworkManager), "ResetGameValuesToDefault")]
        [HarmonyPostfix]
        static void ResetGameValuesToDefaultPatch()
        {
            Script.Logger.LogInfo("/Networking-ResetGameValuesToDefault/ Clearing all subscribtions and globalEnemyLists.");
            Networking.ClearSubscribtionsInDictionary();
            NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists.Clear();
        }

        [HarmonyPatch(typeof(RoundManager), "ResetEnemyVariables")]
        [HarmonyPostfix]
        static void ResetEnemyVariablesPatch()
        {
            Script.Logger.LogInfo("/Networking-ResetEnemyVariables/ Clearing all subscribtions and globalEnemyLists.");
            Networking.ClearSubscribtionsInDictionary();
            NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists.Clear();
        }
    }
}
