using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem.HID;

namespace ExperimentalEnemyInteractions.EnemyPatches
{
    class SpiderData
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public List<EnemyAI> enemyList = new List<EnemyAI>();
        public List<EnemyAI> knownEnemy = new List<EnemyAI>();
        public List<EnemyAI> deadEnemyBodies = new List<EnemyAI>();
        public float LookAtEnemyTimer = 0f;
        public Dictionary<EnemyAI,float> enemiesInLOSDictionary = new Dictionary<EnemyAI, float>();   
    }

    [HarmonyPatch]
    class Reversepatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EnemyAI), "Update")]
        public static void ReverseUpdate(SandSpiderAI instance)
        {
            //Script.Logger.LogInfo("Reverse patch triggered");
        }
    }


    [HarmonyPatch(typeof(SandSpiderAI))]
    class SandSpiderAIPatch
    {
        static float refreshCDtimeSpider = 1f;
        static bool enableSpider = Script.BoundingConfig.enableSpider.Value;
        static bool spiderHuntHoardingbug = Script.BoundingConfig.spiderHuntHoardingbug.Value;

        static Dictionary<SandSpiderAI, SpiderData> spiderList = [];
        static bool debugSpider = Script.BoundingConfig.debugSpiders.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void StartPatch(SandSpiderAI __instance)
        {
            foreach (Transform child in __instance.gameObject.GetComponentsInChildren<Transform>())
            {
                if (child.name == "Abdomen" || child.name == "Head")
                {
                    Vector3 tempLocal = child.localPosition;
                    tempLocal.y = 0f;
                    child.localPosition = tempLocal;
                    Script.Logger.LogInfo("Found " + child.name + ", set position Y to " + tempLocal.y);
                }
            }

            if (!spiderList.ContainsKey(__instance))
            {
                spiderList.Add(__instance, new SpiderData());
                SpiderData spiderData = spiderList[__instance];
            }
        }
        [HarmonyPatch("lateUpdate")]
        [HarmonyPostfix]
        static void LateUpdate(SandSpiderAI __instance)
        {
        foreach(Transform child in __instance.gameObject.GetComponentsInChildren<Transform>())
            {
                if (child.name == "Abdomen" || child.name == "Head")
                {
                    Vector3 tempLocal = child.localPosition;
        tempLocal.y = 0f;
                    child.localPosition = tempLocal;
                    Script.Logger.LogInfo("Found " + child.name + ", set position Y to " + tempLocal.y);
                }
}
        }


        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefixPatch(SandSpiderAI __instance)
        {
            if (!enableSpider) return true;

            SpiderData spiderData = spiderList[__instance];

            /* if (__instance.navigateMeshTowardsPosition && spiderData.targetEnemy != null)
             {
                 __instance.CalculateSpiderPathToPosition();
             }*/

            spiderData.enemyList = EnemyAIPatch.GetInsideEnemyList(EnemyAIPatch.GetCompleteList(__instance), __instance);
            spiderData.enemiesInLOSDictionary = EnemyAIPatch.GetEnemiesInLOS(__instance, spiderData.enemyList, 80f, 15, 2f);

            foreach (KeyValuePair<EnemyAI, float> enemy in spiderData.enemiesInLOSDictionary)
            {
                if (enemy.Key.isEnemyDead)
                {
                    if (debugSpider) Script.Logger.LogWarning(__instance + " Update Postfix: " + enemy.Key + " is Dead! Checking deadEnemyBodies list and skipping...");

                    if (!spiderData.deadEnemyBodies.Contains(enemy.Key))
                    {
                        spiderData.deadEnemyBodies.Add(enemy.Key);
                        if (debugSpider) Script.Logger.LogWarning(__instance + " Update Postfix: " + enemy.Key + " added to deadEnemyBodies list");
                    }
                    continue;
                }
                if (spiderData.knownEnemy.Contains(enemy.Key))
                {
                    if (debugSpider && debugSpam) Script.Logger.LogDebug(__instance + " Update Postfix: " + enemy.Key + " is already in knownEnemyList");
                }
                else
                {
                    if (debugSpider) Script.Logger.LogInfo(__instance + " Update Postfix: Adding " + enemy.Key + " to knownEnemyList");
                    spiderData.knownEnemy.Add(enemy.Key);
                }
            }
            for (int i = 0; i < spiderData.knownEnemy.Count; i++)
            {
                if (spiderData.knownEnemy[i].isEnemyDead)
                {
                    if (debugSpider) Script.Logger.LogWarning(__instance + " Update Postfix: Removed " + spiderData.knownEnemy[i] + " from knownEnemyList");
                    spiderData.knownEnemy.Remove(spiderData.knownEnemy[i]);
                }
            }
            __instance.SyncMeshContainerPositionToClients();
            __instance.CalculateMeshMovement();
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {

#pragma warning disable CS8604 // Possible null reference argument.

                        spiderData.closestEnemy = EnemyAIPatch.findClosestEnemy(spiderData.knownEnemy, spiderData.closestEnemy, __instance);
#pragma warning restore CS8604 // Possible null reference argument.
                              //if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case0/ " + spiderData.closestEnemy + " is Closest enemy");


                        if (spiderData.closestEnemy != null && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye) != false)
                        {
                            spiderData.targetEnemy = spiderData.closestEnemy;
                            if (debugSpider) Script.Logger.LogInfo(__instance + "Update Postfix: /case0/ Set " + spiderData.closestEnemy + " as TargetEnemy");
                            __instance.SwitchToBehaviourState(2);
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case0/ Set state to " + __instance.currentBehaviourStateIndex);
                            __instance.chaseTimer = 12.5f;
                            __instance.watchFromDistance = Vector3.Distance(__instance.meshContainer.transform.position, spiderData.closestEnemy.transform.position) > 8f;
                        }
                        break;
                    }
                case 2:
                    {
                        if (__instance.targetPlayer != null) break;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

                        if (spiderData.targetEnemy != spiderData.closestEnemy && __instance.CheckLineOfSightForPosition(spiderData.closestEnemy.transform.position, 80f, 15, 2f, __instance.eye))
                        {
                            if (spiderData.targetEnemy is HoarderBugAI && spiderData.closestEnemy is not HoarderBugAI && (Vector3.Distance(__instance.meshContainer.position, spiderData.targetEnemy.transform.position) * 1.2f < Vector3.Distance(__instance.meshContainer.position, spiderData.closestEnemy.transform.position)))
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                            else
                            {
                                spiderData.targetEnemy = spiderData.closestEnemy;
                            }
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.


                        if (spiderData.targetEnemy == null)
                        {
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-0/ Stopping chasing: " + spiderData.targetEnemy);
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                            break;
                        }
                        if (__instance.onWall)
                        {
                            __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position);
                            __instance.agent.speed = 4.25f;
                            __instance.spiderSpeed = 4.25f;
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2/ onWall");
                            break;
                        }
                        if (__instance.watchFromDistance && __instance.GetClosestPlayer(true) != null && Vector3.Distance(__instance.meshContainerPosition, __instance.GetClosestPlayer(true).transform.position) > Vector3.Distance(__instance.meshContainerPosition, spiderData.targetEnemy.transform.position))
                        {
                            if (spiderData.LookAtEnemyTimer <= 0f)
                            {
                                spiderData.LookAtEnemyTimer = 3f;
                                __instance.movingTowardsTargetPlayer = false;
                                __instance.overrideSpiderLookRotation = true;
                                Vector3 position = spiderData.targetEnemy.transform.position;
                                __instance.SetSpiderLookAtPosition(position);
                            }
                            else
                            {
                                spiderData.LookAtEnemyTimer -= Time.deltaTime;
                            }
                            __instance.spiderSpeed = 0f;
                            __instance.agent.speed = 0f;
                            if (Physics.Linecast(__instance.meshContainer.position, spiderData.targetEnemy.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-1/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                            else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) < 5f || __instance.stunNormalizedTimer > 0f || spiderData.targetEnemy is HoarderBugAI)
                            {
                                __instance.watchFromDistance = false;
                            }
                            break;
                        }
                        __instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position);
                        if (spiderData.targetEnemy == null || spiderData.targetEnemy.isEnemyDead)
                        {
                            if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-2/ Stopping chasing: " + spiderData.targetEnemy);

                            if (spiderData.targetEnemy != null)
                            {
                                try
                                {
                                    spiderData.deadEnemyBodies.Add(spiderData.targetEnemy);
                                    spiderData.knownEnemy.Remove(spiderData.targetEnemy);
                                    if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-2/ Moved dead enemy to separate list");
                                }
                                catch
                                {
                                    Script.Logger.LogError(__instance + "Update Postfix: /case2-2/ Enemy does not exist!");
                                }
                            }
                            spiderData.targetEnemy = null;
                            __instance.StopChasing();
                        }
                        else if (Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.homeNode.position) > 12f && Vector3.Distance(spiderData.targetEnemy.transform.position, __instance.meshContainer.position) > 5f)
                        {
                            __instance.chaseTimer -= Time.deltaTime;
                            if (__instance.chaseTimer <= 0)
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "Update Postfix: /case2-3/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                __instance.StopChasing();
                            }
                        }
                        break;
                    }
            }

            if (refreshCDtimeSpider <= 0)
            {
                if (debugSpider && debugSpam)
                {
                    Script.Logger.LogInfo(__instance + "watchFromDistance: " + __instance.watchFromDistance);
                    Script.Logger.LogInfo(__instance + "overrideSpiderLookRotation: " + __instance.overrideSpiderLookRotation);
                    Script.Logger.LogInfo(__instance + "moveTowardsDestination: " + __instance.moveTowardsDestination);
                    Script.Logger.LogInfo(__instance + "movingTowardsTargetPlayer: " + __instance.movingTowardsTargetPlayer);
                }
                refreshCDtimeSpider = 0.5f;
            }
            else
            {
                refreshCDtimeSpider -= Time.deltaTime;
            }

            if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                Reversepatch.ReverseUpdate(__instance);
                if (__instance.updateDestinationInterval >= 0)
                {
                    __instance.updateDestinationInterval -= Time.deltaTime;
                }
                else
                {
                    __instance.updateDestinationInterval = __instance.AIIntervalTime + Random.Range(-0.015f, 0.015f);
                    __instance.DoAIInterval();
                }
                __instance.timeSinceHittingPlayer += Time.deltaTime;
                return false;
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefix(SandSpiderAI __instance)
        {
            if (!spiderHuntHoardingbug) return true;
            SpiderData spiderData = spiderList[__instance];
            SandSpiderAI Ins = __instance;

        if (spiderData.targetEnemy != null && !__instance.targetPlayer && __instance.currentBehaviourStateIndex == 2)
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();

                if (debugSpider && debugSpam) Script.Logger.LogDebug(__instance + "DoAIInterval Prefix: false");
                return false;
            }
            if (debugSpider && debugSpam) Script.Logger.LogDebug(__instance + "DoAIInterval Prefix: true");
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfix(SandSpiderAI __instance)
        {
            SandSpiderAI Ins = __instance;
            SpiderData spiderData = spiderList[__instance];
            
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (debugSpider && debugSpam) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case0/ nothing");
                        break;
                    }
                case 1:
                    {
                        if (debugSpider && debugSpam) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/");
                        List<EnemyAI> tempList = spiderData.enemiesInLOSDictionary.Keys.ToList();
                        if (Ins.reachedWallPosition)
                        {
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 5f || tempList[i] is HoarderBugAI)
                                {
                                    ChaseEnemy(__instance, tempList[i]);
                                    if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Chasing enemy: " + tempList[i]);
                                    break;
                                }
                                if (Vector3.Distance(Ins.meshContainer.position, tempList[i].transform.position) < 10f)
                                {
                                    Vector3 position = tempList[i].transform.position;
                                    float wallnumb = Vector3.Dot(position - Ins.meshContainer.position, Ins.wallNormal);
                                    Vector3 forward = position - wallnumb * Ins.wallNormal;
                                    Ins.meshContainerTargetRotation = Quaternion.LookRotation(forward, Ins.wallNormal);
                                    Ins.overrideSpiderLookRotation = true;
                                    if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case1/ Moving off-wall to enemy: " + tempList[i]);
                                    break;
                                }
                            }
                        }
                    }
                    Ins.overrideSpiderLookRotation = false;
                    break;
                case 2:
                    {
                        if (!(spiderData.targetEnemy == null))
                        {
                            if (spiderData.targetEnemy.isEnemyDead && !(__instance.SetDestinationToPosition(spiderData.targetEnemy.transform.position, checkForPath: true)))
                            {
                                if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case2/ Stopping chasing: " + spiderData.targetEnemy);
                                spiderData.targetEnemy = null;
                                Ins.StopChasing();
                            }
                            if (Ins.watchFromDistance && spiderData.targetEnemy != null)
                            {
                                Ins.SetDestinationToPosition(Ins.ChooseClosestNodeToPosition(spiderData.targetEnemy.transform.position, avoidLineOfSight: false, 4).transform.position);
                                if (debugSpider) Script.Logger.LogDebug(__instance + "DoAIInterval Postfix: /case2/ Set destination to: " + spiderData.targetEnemy);
                            }
                        }
                        break;
                    }
            }
        }

        static void ChaseEnemy(SandSpiderAI ins, EnemyAI target, SandSpiderWebTrap? triggeredWeb = null)
        {
            SpiderData spiderData = spiderList[ins];
            if ((ins.currentBehaviourStateIndex != 2 && ins.watchFromDistance) || Vector3.Distance(target.transform.position, ins.homeNode.position) < 25f || Vector3.Distance(ins.meshContainer.position, target.transform.position) < 15f)
            {
                ins.watchFromDistance = false;
                spiderData.targetEnemy = target;
                ins.chaseTimer = 12.5f;
                ins.SwitchToBehaviourState(2);
                if (debugSpider) Script.Logger.LogDebug(ins + "ChaseEnemy: Switched state to: " + ins.currentBehaviourStateIndex);
            }
        }
    }

    class SpiderWebValues
    {
        public struct EnemyInfo(EnemyAI enemy, float enterAgentSpeed, float enterAnimationSpeed)
        {
            internal EnemyAI enemyAI {get; set;} = enemy;
            internal float enterAgentSpeed {get; set;} = enterAgentSpeed;
            internal float enterAnimationSpeed {get; set;} = enterAnimationSpeed;
        }
         public Dictionary<EnemyAI, EnemyInfo> collidingEnemy = new Dictionary<EnemyAI, EnemyInfo>();

    }


    [HarmonyPatch(typeof(SandSpiderWebTrap))]
    class SandSpiderWebTrapPatch
    {

        static Dictionary<SandSpiderWebTrap, SpiderWebValues> spiderWebs = [];

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(SandSpiderWebTrap __instance)
        {
            if (!spiderWebs.ContainsKey(__instance))
            {
                spiderWebs.Add(__instance, new SpiderWebValues());
            }
        }






        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        static void OnTriggerStayPatch(Collider other, SandSpiderWebTrap __instance)
        {
            SpiderWebValues webValues = spiderWebs[__instance];
            EnemyAI trippedEnemy = other.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;
            if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Triggered");
            if (trippedEnemy != null)
            {
                if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Collided with " + trippedEnemy);
                if (!webValues.collidingEnemy.ContainsKey(trippedEnemy))
                {
                    webValues.collidingEnemy.Add(trippedEnemy, new SpiderWebValues.EnemyInfo(trippedEnemy,trippedEnemy.agent.speed,trippedEnemy.creatureAnimator.speed));
                    if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Added " + trippedEnemy + " to collidingEnemy");
                }
                trippedEnemy.agent.speed = webValues.collidingEnemy[trippedEnemy].enterAgentSpeed / 3;
                trippedEnemy.creatureAnimator.speed = webValues.collidingEnemy[trippedEnemy].enterAnimationSpeed / 3;
                if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Slowed down " + trippedEnemy);

                if (!__instance.webAudio.isPlaying)
                {
                    __instance.webAudio.Play();
                    __instance.webAudio.PlayOneShot(__instance.mainScript.hitWebSFX);
                }
            }
            if (trippedEnemy == null)
            {
                webValues.collidingEnemy.Clear();
                if (Script.BoundingConfig.debugSpiders.Value) Script.Logger.LogInfo(__instance + " Cleared " + trippedEnemy + " from collidingEnemy");
                if (__instance.webAudio.isPlaying && __instance.currentTrappedPlayer == null)
                {
                    __instance.webAudio.Stop();
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(SandSpiderWebTrap __instance, out bool __state)
        {
            SpiderWebValues webValues = spiderWebs[__instance];
            if (__instance.currentTrappedPlayer != null)
            {
                __state = false;
                return true;
            }
            else if (webValues.collidingEnemy.Count > 0)
            {
                __state = true;
                return false;
            }
            __state = false;
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(SandSpiderWebTrap __instance, bool __state)
        {
            if (!__state)
            {
                return;
            }
            SpiderWebValues webValues = spiderWebs[__instance];
            if (webValues.collidingEnemy.Count > 0)
            {
                __instance.leftBone.LookAt(webValues.collidingEnemy.First().Key.transform.position);
                __instance.rightBone.LookAt(webValues.collidingEnemy.First().Key.transform.position);
            }
            else
            {
                __instance.leftBone.LookAt(__instance.centerOfWeb);
                __instance.rightBone.LookAt(__instance.centerOfWeb);
            }
            __instance.transform.localScale = Vector3.Lerp(__instance.transform.localScale, new Vector3(1f, 1f, __instance.zScale), 8f * Time.deltaTime);
        }
    }


}