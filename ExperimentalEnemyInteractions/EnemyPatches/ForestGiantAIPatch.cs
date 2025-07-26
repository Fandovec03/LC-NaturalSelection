using HarmonyLib;
using UnityEngine;
using LethalNetworkAPI;
using NaturalSelection.Generics;
using BepInEx.Logging;

namespace NaturalSelection.EnemyPatches
{
    class GiantData : EnemyDataBase
    {
        internal int extinguished = 0;
        internal bool setFireOnKill = false;
        internal float CachedNetworkOwnerPostfixResult = 0f;
    }




    [HarmonyPatch(typeof(ForestGiantAI))]
    class ForestGiantPatch
    {
        //static Dictionary<ForestGiantAI, GiantData> giantDictionary = [];
        static bool logGiant = Script.Bools["debugGiants"];
        static bool debugSpam = Script.Bools["spammyLogs"];
        static LNetworkEvent NetworkSetGiantOnFire(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSSetGiantOnFire" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        internal static LNetworkEvent NetworkExtinguish(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSExtinguish" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkEvent(NWID);
        }

        static LNetworkVariable<float> NetworkOwnerPostfixResult(ForestGiantAI forestGiantAI)
        {
            string NWID = "NSOwnerrealtimeSinceStartup" + forestGiantAI.NetworkObjectId;
            return Networking.NSEnemyNetworkVariable<float>(NWID);
        }

        static void Event_OnConfigSettingChanged(string entryKey, bool value)
        {
            if (entryKey == "debugGiants") logGiant = value;
            if (entryKey == "spammyLogs") debugSpam = value;
            //Script.LogNS(LogLevel.Message,$"Forest Keeper received event. logGiant = {logGiant}, debugSpam = {debugSpam}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void startPostfix(ForestGiantAI __instance)
        {
            GiantData data = (GiantData)Utilities.GetEnemyData(__instance, new GiantData());
            NetworkSetGiantOnFire(__instance).OnServerReceived += UpdateSetGiantOnFireServer;
            //NetworkSetGiantOnFire(__instance).OnClientReceived += UpdateSetGiantOnFire;

            void UpdateSetGiantOnFireServer(ulong client)
            {
                //NetworkSetGiantOnFire(__instance).InvokeClients();
                if (__instance.IsOwner)
                {
                    __instance.timeAtStartOfBurning = Time.realtimeSinceStartup;
                    __instance.SwitchToBehaviourState(2);
                    Script.LogNS(LogLevel.Info,"Received UpdateSetGiantOnFire event");
                }
            }

            NetworkExtinguish(__instance).OnClientReceived += ExtuinguishGiant;

            void ExtuinguishGiant()
            {
                __instance.SwitchToBehaviourState(0);
                __instance.burningParticlesContainer.SetActive(false);
                __instance.giantBurningAudio.Stop();
                __instance.creatureAnimator.SetBool("burning", false);
                data.extinguished = 1;
            }

            NetworkOwnerPostfixResult(__instance).OnValueChanged += OwnerPostfixResult;

            void OwnerPostfixResult(float oldValue, float newValue)
            {
                if (oldValue != newValue) { data.CachedNetworkOwnerPostfixResult = newValue; }
            }
            
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        [HarmonyPatch("KillEnemy")]
        [HarmonyPostfix]
        static void KillEnemyPatchPostfix(ForestGiantAI __instance)
        {
            GiantData giantDaata = (GiantData)Utilities.GetEnemyData(__instance, new GiantData());
            if (giantDaata.extinguished != 1 && __instance.currentBehaviourStateIndex == 2)
            {
                __instance.burningParticlesContainer.SetActive(true);
            }
            //if (logGiant) Script.LogNS(LogLevel.Info,LibraryCalls.DebugStringHead(__instance));
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(ForestGiantAI __instance)
        {
            GiantData giantData = (GiantData)Utilities.GetEnemyData(__instance, new GiantData());

            if (__instance.currentBehaviourStateIndex == 2 && Time.realtimeSinceStartup - __instance.timeAtStartOfBurning > 9.5f && __instance.enemyHP > 20 && giantData.extinguished == 0  && !__instance.isEnemyDead && __instance.IsOwner)
            {
                int randomNumber = Random.Range(0, 100);

                if (randomNumber <= Script.BoundingConfig.giantExtinguishChance.Value)
                {
                    NetworkExtinguish(__instance).InvokeClients();
                    Script.LogNS(LogLevel.Info,$"successfully extinguished itself. Skipping Update. Rolled {randomNumber}", __instance);
                }
                else
                {
                    Script.LogNS(LogLevel.Info,$"failed to extinguish itself. rolled {randomNumber}", __instance);
                    giantData.extinguished = 2;
                }
            }

            if (giantData.extinguished == 1)
            {
                __instance.enemyHP -= 20;
                giantData.extinguished = 2;
                return false;
            }
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(ForestGiantAI __instance)
        {
            GiantData giantData = (GiantData)Utilities.GetEnemyData(__instance, new GiantData());
            if (__instance.IsOwner) giantData.CachedNetworkOwnerPostfixResult = Time.realtimeSinceStartup - __instance.timeAtStartOfBurning;

            if (__instance.isEnemyDead && __instance.currentBehaviourStateIndex == 2 && giantData.CachedNetworkOwnerPostfixResult < 20f)
            {
                if (__instance.IsOwner) giantData.CachedNetworkOwnerPostfixResult = Time.realtimeSinceStartup - __instance.timeAtStartOfBurning;
                if (!__instance.giantBurningAudio.isPlaying)
                {
                    __instance.giantBurningAudio.Play();
                }
                __instance.giantBurningAudio.volume = Mathf.Min(__instance.giantBurningAudio.volume + Time.deltaTime * 0.5f, 1f);
            }
            else if (__instance.isEnemyDead && giantData.CachedNetworkOwnerPostfixResult > 26f && __instance.burningParticlesContainer.activeSelf == true)
            {     
                __instance.burningParticlesContainer.SetActive(false);
            }
        }

        /*public static void CheckDataIntegrityGiant(ForestGiantAI __instance)
        {
            if (!giantDictionary.ContainsKey(__instance))
            {
                Script.LogNS(LogLevel.Fatal, $"Critical failule. Failed to get data for {LibraryCalls.DebugStringHead(__instance)}. Attempting to fix...");
                giantDictionary.Add(__instance, new GiantData());
            }
        }*/
        /*
        public static void RollToExtinguish(ForestGiantAI __instance)
        {
            GiantData giantData = giantDictionary[__instance];
            if (__instance.enemyHP > 20 && giantData.extinguished == 0  && !__instance.isEnemyDead && __instance.IsOwner)
            {
                int randomNumber = Random.Range(0, 100);

                if (randomNumber <= Script.BoundingConfig.giantExtinguishChance.Value)
                {
                    NetworkExtinguish(__instance).InvokeClients();
                    Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} successfully extinguished itself. Skipping Update. Rolled {randomNumber}");
                }
                else
                {
                    Script.LogNS(LogLevel.Info,$"{LibraryCalls.DebugStringHead(__instance)} failed to extinguish itself. rolled {randomNumber}");
                    giantData.extinguished = 2;
                }
            }
        }*/
    }
} 