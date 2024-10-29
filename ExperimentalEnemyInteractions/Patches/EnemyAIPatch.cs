using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static List<EnemyAI> enemyList = new List<EnemyAI>();
        static float time = 0f;
        static float refreshCDtime = 1f;

        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]

        static void UpdatePostfixPatch(EnemyAI __instance)
        {
            time += Time.deltaTime;
            refreshCDtime -= Time.deltaTime;

            if (refreshCDtime <= 0)
            {
                foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == false)
                    {
                        if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found Duplicate " + enemy.gameObject.name + ", ID: " + enemy.GetInstanceID());
                    }
                    if (enemyList.Contains(enemy) && enemy.isEnemyDead == true)
                    {
                        enemyList.Remove(enemy);
                        if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Found and removed dead Enemy " + enemy.gameObject.name + ", ID:  " + enemy.GetInstanceID() + "on List.");
                    }
                    if (!enemyList.Contains(enemy) && enemy.isEnemyDead == false && enemy.name != __instance.name)
                    {
                        enemyList.Add(enemy);
                        if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Added " + enemy.gameObject.name + " detected in List. Instance: " + enemy.GetInstanceID());
                    }
                }

                for (int i = 0; i < enemyList.Count; i++)
                {
                    if (__instance != null && enemyList.Count > 0)
                    {
                        RaycastHit hit = new RaycastHit();
                        if (enemyList[i] == null)
                        {
                            if (debugUnspecified) Script.Logger.LogError(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Detected null enemy in the list. Removing...");
                            enemyList.RemoveAt(i);
                        }
                        if (enemyList[i] != null)
                        {
                            if (!Physics.Linecast(__instance.gameObject.transform.position, enemyList[i].gameObject.transform.position, out hit, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore))
                            {
                                if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "LOS check: Have LOS on " + enemyList[i] + ", ID: " + enemyList[i].GetInstanceID());
                            }
                        }
                    }
                }
                refreshCDtime = 1f;
            }
        }

        public static List<EnemyAI> GetCompleteList(EnemyAI __instance, bool FilterThemselves = true)
        {
            List<EnemyAI> tempList = enemyList;
            bool filter = FilterThemselves;

            if (__instance != null)
            {
                for (int i = 0; i < tempList.Count; i++)
                {
                    if (tempList[i].GetType() == __instance.GetType() && filter)
                    {
                        tempList.Remove(tempList[i]);
                    }
                }
            }
            return tempList;
        }

        public static List<EnemyAI> GetOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            List<EnemyAI> outsideEnemies = new List<EnemyAI>();

            foreach (EnemyAI enemy in importEnemyList)
            {
                if (enemy.isOutside == true && enemy != instance)
                {
                    outsideEnemies.Add(enemy);
                }
            }

            return outsideEnemies;
        }

        public static List<EnemyAI> GetInsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            List<EnemyAI> insideEnemies = new List<EnemyAI>();

            foreach (EnemyAI enemy in importEnemyList)
            {
                if (enemy.isOutside == false && enemy != instance)
                {
                    insideEnemies.Add(enemy);
                }
            }

            return insideEnemies;
        }

        public static EnemyAI? findClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI importClosestEnemy, EnemyAI __instance)
        {
            EnemyAI tempClosestEnemy = importClosestEnemy;

            for (int i = 0; i < importEnemyList.Count; i++)
            {
                if (tempClosestEnemy == null)
                {
                    if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "No enemy assigned. Assigning " + importEnemyList[i] + ", ID: " + importEnemyList[i].GetInstanceID() + " as new closestEnemy.");
                    tempClosestEnemy = importEnemyList[i];
                }
                if (tempClosestEnemy == importEnemyList[i])
                {
                    if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + importEnemyList[i] + ", ID: " + importEnemyList[i].GetInstanceID() + " is already assigned as closestEnemy");
                }
                if (importEnemyList[i] != tempClosestEnemy)
                {
                    if (Vector3.Distance(__instance.transform.position, importEnemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, tempClosestEnemy.transform.position))
                    {
                        tempClosestEnemy = importEnemyList[i];
                        if (debugUnspecified) Script.Logger.LogDebug(Vector3.Distance(__instance.transform.position, importEnemyList[i].transform.position) < Vector3.Distance(__instance.transform.position, tempClosestEnemy.transform.position));
                        if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Assigned " + importEnemyList[i] + ", ID: " + importEnemyList[i].GetInstanceID() + " as new closestEnemy. Distance: " + Vector3.Distance(__instance.transform.position, tempClosestEnemy.transform.position));
                    }
                }
            }
            return tempClosestEnemy;
        }
        public static List<EnemyAI> filterEnemyList(List<EnemyAI> importEnemyList, List<Type> targetTypes, EnemyAI __instance)
        {
            List<EnemyAI> filteredList = new List<EnemyAI>();

            for (int i = 0; i < importEnemyList.Count; i++)
            {
                if (targetTypes.Contains(importEnemyList[i].GetType()))
                {
                    if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": Enemy of type " + importEnemyList[i].GetType() + " passed the filter.");
                    filteredList.Add(importEnemyList[i]);
                }
                else if (debugUnspecified)
                {
                    if (debugUnspecified) Script.Logger.LogDebug(__instance.name + ", ID: " + __instance.GetInstanceID() + ": " + "Caught and filtered out Enemy of type " + enemyList[i].GetType());
                }
            }
            return filteredList;
        }
        

        static public Dictionary<EnemyAI,float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            List<EnemyAI> tempList = new List<EnemyAI>();
            Dictionary<EnemyAI,float> tempDictionary = new Dictionary<EnemyAI,float>();
            float range = (float) importRange;

            if (instance.isOutside && !instance.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(importRange, 0, 30);
            }
            foreach (EnemyAI enemy in importEnemyList)
            {
                if (!enemy.isEnemyDead && enemy != null)
                {
                   if (debugUnspecified) Script.Logger.LogDebug(instance.name + ", ID: " + instance.GetInstanceID() + "/GetEnemiesInLOS/: Added " + enemy + " to tempList");
                    tempList.Add(enemy);
                }
            }
            if (tempList != null && tempList.Count > 0)
            {
                for (int i = 0; i < tempList.Count; i++)
                {
                    Vector3 position = tempList[i].transform.position;
                    if (Vector3.Distance(position, instance.eye.position) < range && !Physics.Linecast(instance.eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        if (instance.CheckLineOfSightForPosition(position, width, (int)range, proximityAwareness, instance.eye))
                        {
                            if (!tempDictionary.ContainsKey(tempList[i]))
                            {
                                tempDictionary.Add(tempList[i], Vector3.Distance(instance.transform.position, position));
                                if (debugUnspecified)
                                    Script.Logger.LogDebug(instance.name + ", ID: " + instance.GetInstanceID() + "/GetEnemiesInLOS/: Added " + tempList[i] + " to tempDictionary");
                            }
                            else
                            {
                                Script.Logger.LogInfo(instance.name + ", ID: " + instance.GetInstanceID() + "/GetEnemiesInLOS/:" + tempList[i] + " is already in tempDictionary");
                            }
                        }
                    }
                }
            }
            if (tempDictionary.Count > 1)
            {
                tempDictionary.OrderBy(value => tempDictionary.Values);

                for(int i = 0;i < tempDictionary.Count;i++)
                {
                    if (debugUnspecified) 
                    Script.Logger.LogDebug(instance.name + ", ID: " + instance.GetInstanceID() + "/GetEnemiesInLOS/: Final list: "+ tempDictionary[tempList[i]] + ", position: " + i);
                }
            }
            return tempDictionary;
        }

        static public int ReactToHit(EnemyAI __instance, int force = 0, EnemyAI? enemyAI = null, PlayerControllerB? player = null)
        {

            if (__instance is PufferAI)
            {
                if (force > 0)
                {
                    return 1;
                }
                if (force > 1)
                {
                    return 2;
                }
            }
            return 0;
        }
    }
    
    public class ReversePatchEnemy : EnemyAI
    {
        public override void Update()
        {
            base.Update();
        }
    }
}
