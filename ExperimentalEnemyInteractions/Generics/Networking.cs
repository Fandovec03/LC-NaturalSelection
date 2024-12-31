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
        static bool logNetworking = Script.BoundingConfig.debugNetworking.Value;

        public static LNetworkVariable<float> NSEnemyNetworkVariableFloat(string NWID)
        {
            if (!NetworkingDictionary.ContainsKey(NWID)) NetworkingDictionary.Add(NWID, 31);
            return LNetworkVariable<float>.Connect(NWID);
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
                            if (logNetworking) Script.Logger.LogDebug("Clearing subscriptions of event " + pair.Key);
                            LNetworkEvent.Connect(pair.Key).ClearSubscriptions(); break;
                        }
                    case 31:
                        {
                            if (logNetworking) Script.Logger.LogDebug("Disposing of network float " + pair.Key);
                            LNetworkVariable<float>.Connect(pair.Key).Dispose(); break;
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
            Script.Logger.LogInfo("/Networking-ResetGameValuesToDefault/ Clearing all subscribtions in dictionary.");
            Networking.ClearSubscribtionsInDictionary();
        }

        [HarmonyPatch(typeof(RoundManager), "ResetEnemyVariables")]
        [HarmonyPostfix]
        static void ResetEnemyVariablesPatch()
        {
            Script.Logger.LogInfo("/Networking-ResetEnemyVariables/ Clearing all subscribtions in dictionary.");
            Networking.ClearSubscribtionsInDictionary();
        }
    }
}
