using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using NaturalSelection.Generics;
using Steamworks.ServerList;
using BepInEx.Logging;
using System.Linq;
using UnityEngine;
using JetBrains.Annotations;

namespace NaturalSelection.EnemyPatches
{
    class EnemyData : EnemyDataBase
    {
        internal float originalAgentRadius = 0f;
    }

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.Bools["debugUnspecified"];
        static bool debugTriggerFlags = Script.Bools["debugTriggerFlags"];
        public static Dictionary<string, EnemyDataBase> enemyDataDict = [];
        static void Event_OnConfigSettingChanged(string entryKey, bool value)

        {
            if (entryKey == "debugUnspecified") debugUnspecified = value;
            if (entryKey == "debugTriggerFlags") debugTriggerFlags = value;
            //Script.LogNS(LogLevel.Message,$"EnemyAI received event. debugUnspecified = {debugUnspecified}, debugTriggerFlags = {debugTriggerFlags}");
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {
            EnemyData data = (EnemyData)GetEnemyData(__instance, new EnemyData(), true);

            data.originalAgentRadius = __instance.agent.radius;

            if (InitializeGamePatch.customSizeOverrideListDictionary.ContainsKey(__instance.enemyType.enemyName))
            {
                data.customEnemySize = (CustomEnemySize)InitializeGamePatch.customSizeOverrideListDictionary[__instance.enemyType.enemyName];
            }

            Script.LogNS(BepInEx.Logging.LogLevel.Debug, $"Final size: {data.customEnemySize}", __instance);

            __instance.agent.radius = __instance.agent.radius * Script.BoundingConfig.agentRadiusModifier.Value;
            Script.LogNS(BepInEx.Logging.LogLevel.Message, $"Modified agent radius. Original: {data.originalAgentRadius}, Modified: {__instance.agent.radius}", toggle: debugUnspecified && debugTriggerFlags);
            Script.OnConfigSettingChanged += Event_OnConfigSettingChanged;
        }

        static public int ReactToHit(int force = 0, EnemyAI? enemyAI = null, PlayerControllerB? player = null)
        {
            if (force > 0)
            {
                return 1;
            }
            if (force > 1)
            {
                return 2;
            }
            return 0;
        }

        [HarmonyPatch("HitEnemy")]
        [HarmonyPostfix]
        public static void HitEnemyPatch(EnemyAI __instance, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            string playerString = "unknown";
            if (playerWhoHit != null)
            {
                playerString = $"{playerWhoHit.playerUsername}(SteamID: {playerWhoHit.playerSteamId}, playerClientID: {playerWhoHit.playerClientId})";
            }
            Script.LogNS(LogLevel.Info, $"registered hit by {playerString} with force of {force}. playHitSFX:{playHitSFX}, hitID:{hitID}.", __instance, debugTriggerFlags);
        }

        public static EnemyDataBase GetEnemyData(object __instance, EnemyDataBase enemyData, bool returnToEnemyAIType = false)
        {
            string id = "-1";
            if (__instance is EnemyAI)
            {
                id = ((EnemyAI)__instance).enemyType.enemyName + ((EnemyAI)__instance).NetworkBehaviourId;
                if (returnToEnemyAIType) id += ".base";
            }
            else if (__instance is SandSpiderWebTrap) id = ((SandSpiderWebTrap)__instance).mainScript.enemyType.enemyName + ((SandSpiderWebTrap)__instance).mainScript.NetworkBehaviourId +"SpiderWeb"+ ((SandSpiderWebTrap)__instance).trapID;
            else if (__instance is string) id = (string)__instance;
            else return null;
            
            if (!enemyDataDict.ContainsKey(id))
            {
                Script.LogNS(LogLevel.Info, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
                enemyDataDict.Add(id, enemyData);
            }
            return enemyDataDict[id];
        }

        public static void GetEnemyData(string __instance, out EnemyDataBase enemyDataOut)
        {
            enemyDataOut = enemyDataDict[__instance];
        }
        public static void ReactToAttack(EnemyAI instance, Collider other)
        {
            string id = other.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript.enemyType.enemyName + other.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript.NetworkBehaviourId;
            GetEnemyData(id, out EnemyDataBase enemyData);
            if (enemyData != null) enemyData.ReactToAttack(instance, other.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript,1);
        }
    }
    public class ReversePatchAI
    {
        public static Action<EnemyAI> originalUpdate;

        static ReversePatchAI()
        {
            var method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Update));
            var dm = new DynamicMethod("Base.Update", null, [typeof(EnemyAI)], typeof(EnemyAI));
            var gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, method);
            gen.Emit(OpCodes.Ret);

            originalUpdate = (Action<EnemyAI>)dm.CreateDelegate(typeof(Action<EnemyAI>));
        }
    }
}
