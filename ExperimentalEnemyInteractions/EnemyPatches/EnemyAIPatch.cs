using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using UnityEngine;

namespace NaturalSelection.EnemyPatches
{

    [HarmonyPatch(typeof(EnemyAI))]
    class EnemyAIPatch
    {
        static bool debugUnspecified = Script.BoundingConfig.debugUnspecified.Value;
        static bool debugSpam = Script.BoundingConfig.spammyLogs.Value;
        static bool debugTriggerFlag = Script.BoundingConfig.debugTriggerFlags.Value;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(EnemyAI __instance)
        {

            if (debugSpam && debugUnspecified) Script.Logger.LogInfo("Called Setup library!");
            __instance.agent.radius = __instance.agent.radius * Script.clampedAgentRadius;
        }
        public static string DebugStringHead(EnemyAI? instance)
        {
            //if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library!");
            return NaturalSelectionLib.NaturalSelectionLib.DebugStringHead(instance);
        }
        public static List<EnemyAI> GetCompleteList(EnemyAI instance, bool FilterThemselves = true, int includeOrReturnThedDead = 0)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetCompleteList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetCompleteList(instance, FilterThemselves, includeOrReturnThedDead);
        }

        public static List<EnemyAI> GetInsideOrOutsideEnemyList(List<EnemyAI> importEnemyList, EnemyAI instance)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetInsideOrOutsideEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.GetInsideOrOutsideEnemyList(importEnemyList, instance);
        }

        public static EnemyAI? FindClosestEnemy(List<EnemyAI> importEnemyList, EnemyAI? importClosestEnemy, EnemyAI instance, bool includeTheDead = false)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library findClosestEnemy!");
            return NaturalSelectionLib.NaturalSelectionLib.FindClosestEnemy(importEnemyList, importClosestEnemy, instance, includeTheDead);
        }
        public static List<EnemyAI> FilterEnemyList(List<EnemyAI> importEnemyList, List<Type>? targetTypes, List<string>? blacklist, EnemyAI instance, bool inverseToggle = false, bool filterOutImmortal = true)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library filterEnemyList!");
            return NaturalSelectionLib.NaturalSelectionLib.FilterEnemyList(importEnemyList, targetTypes, blacklist, instance, inverseToggle, filterOutImmortal);
        }


        static public Dictionary<EnemyAI, float> GetEnemiesInLOS(EnemyAI instance, List<EnemyAI> importEnemyList, float width = 45f, int importRange = 0, float proximityAwareness = -1)
        {
            if (debugSpam && debugTriggerFlag && debugUnspecified) Script.Logger.LogInfo("Called library GetEnemiesInLOS!");
            return NaturalSelectionLib.NaturalSelectionLib.GetEnemiesInLOS(instance, importEnemyList, width, importRange, proximityAwareness);
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

        /*[HarmonyPatch("KillEnemy")]
        static void KillEnemyTranspiller()
        {
            static IEnumerable<CodeInstruction> Transpiller(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldstr),
                    new CodeMatch(OpCodes.Call, OpCodes.Call))
                    .Repeat(matcher => matcher
                    .RemoveInstructions(2)
                    )
                    .InstructionEnumeration();
            }
        }*/
    }
    [HarmonyPatch]
    public class ReversePatchAI
    {
        public static void ReverseUpdate(EnemyAI instance)
        {
            if (instance.enemyType.isDaytimeEnemy && !instance.daytimeEnemyLeaving)
            {
                instance.CheckTimeOfDayToLeave();
            }
            if (instance.stunnedIndefinitely <= 0)
            {
                if (instance.stunNormalizedTimer >= 0f)
                {
                    instance.stunNormalizedTimer -= Time.deltaTime / instance.enemyType.stunTimeMultiplier;
                }
                else
                {
                    instance.stunnedByPlayer = null;
                    if (instance.postStunInvincibilityTimer >= 0f)
                    {
                        instance.postStunInvincibilityTimer -= Time.deltaTime * 5f;
                    }
                }
            }
            if (!instance.ventAnimationFinished && instance.timeSinceSpawn < instance.exitVentAnimationTime + 0.005f * (float)RoundManager.Instance.numberOfEnemiesInScene)
            {
                instance.timeSinceSpawn += Time.deltaTime;
                if (!instance.IsOwner)
                {
                    _ = instance.serverPosition;
                    if (instance.serverPosition != Vector3.zero)
                    {
                        instance.transform.position = instance.serverPosition;
                        instance.transform.eulerAngles = new Vector3(instance.transform.eulerAngles.x, instance.targetYRotation, instance.transform.eulerAngles.z);
                    }
                }
                else if (instance.updateDestinationInterval >= 0f)
                {
                    instance.updateDestinationInterval -= Time.deltaTime;
                }
                else
                {
                    instance.SyncPositionToClients();
                    instance.updateDestinationInterval = 0.1f;
                }
                return;
            }
            if (!instance.inSpecialAnimation && !instance.ventAnimationFinished)
            {
                instance.ventAnimationFinished = true;
                if (instance.creatureAnimator != null)
                {
                    instance.creatureAnimator.SetBool("inSpawningAnimation", value: false);
                }
            }
            if (!instance.IsOwner)
            {
                if (instance.currentSearch.inProgress)
                {
                    instance.StopSearch(instance.currentSearch);
                }
                instance.SetClientCalculatingAI(enable: false);
                if (!instance.inSpecialAnimation)
                {
                    if (RoundManager.Instance.currentDungeonType == 4 && Vector3.Distance(instance.transform.position, RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position) < 1f)
                    {
                        instance.serverPosition += RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position - RoundManager.Instance.currentMineshaftElevator.previousElevatorPosition;
                    }
                    instance.transform.position = Vector3.SmoothDamp(instance.transform.position, instance.serverPosition, ref instance.tempVelocity, instance.syncMovementSpeed);
                    instance.transform.eulerAngles = new Vector3(instance.transform.eulerAngles.x, Mathf.LerpAngle(instance.transform.eulerAngles.y, instance.targetYRotation, 15f * Time.deltaTime), instance.transform.eulerAngles.z);
                }
                instance.timeSinceSpawn += Time.deltaTime;
                return;
            }
            if (instance.isEnemyDead)
            {
                instance.SetClientCalculatingAI(enable: false);
                return;
            }
            if (!instance.inSpecialAnimation)
            {
                instance.SetClientCalculatingAI(enable: true);
            }
            if (instance.movingTowardsTargetPlayer && instance.targetPlayer != null)
            {
                if (instance.setDestinationToPlayerInterval <= 0f)
                {
                    instance.setDestinationToPlayerInterval = 0.25f;
                    instance.destination = RoundManager.Instance.GetNavMeshPosition(instance.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                }
                else
                {
                    instance.destination = new Vector3(instance.targetPlayer.transform.position.x, instance.destination.y, instance.targetPlayer.transform.position.z);
                    instance.setDestinationToPlayerInterval -= Time.deltaTime;
                }
                if (instance.addPlayerVelocityToDestination > 0f)
                {
                    if (instance.targetPlayer == GameNetworkManager.Instance.localPlayerController)
                    {
                        instance.destination += Vector3.Normalize(instance.targetPlayer.thisController.velocity * 100f) * instance.addPlayerVelocityToDestination;
                    }
                    else if (instance.targetPlayer.timeSincePlayerMoving < 0.25f)
                    {
                        instance.destination += Vector3.Normalize((instance.targetPlayer.serverPlayerPosition - instance.targetPlayer.oldPlayerPosition) * 100f) * instance.addPlayerVelocityToDestination;
                    }
                }
            }
            if (instance.inSpecialAnimation)
            {
                return;
            }
            if (instance.updateDestinationInterval >= 0f)
            {
                instance.updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                instance.DoAIInterval();
                instance.updateDestinationInterval = instance.AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
            }
            if (Mathf.Abs(instance.previousYRotation - instance.transform.eulerAngles.y) > 6f)
            {
                instance.previousYRotation = instance.transform.eulerAngles.y;
                instance.targetYRotation = instance.previousYRotation;
                if (instance.IsServer)
                {
                    instance.UpdateEnemyRotationClientRpc((short)instance.previousYRotation);
                }
                else
                {
                    instance.UpdateEnemyRotationServerRpc((short)instance.previousYRotation);
                }
            }
        }
    }
}
