using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using NaturalSelection.EnemyPatches;
using UnityEngine;


namespace NaturalSelection.Generics;

public enum CustomEnemySize
{
    Undefined,
    Tiny,
    Small,
    Medium,
    Large,
    Giant
}
public class EnemyDataBase
{
    int CustomBehaviorStateIndex = 0;
    public EnemyAI? closestEnemy;
    public EnemyAI? targetEnemy;
    public CustomEnemySize customEnemySize = CustomEnemySize.Small;
    public string enemyID = "";
    public void ReactToAttack(EnemyAI owner, EnemyAI attacker, int damage = 0)
    {
        Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(attacker)} hit {LibraryCalls.DebugStringHead(attacker)} with {damage} damage");
    }

    public string GetOrSetId(EnemyAI instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.enemyType.enemyName + instance.NetworkBehaviourId;
        return enemyID;
    }

    public string GetOrSetId(string id)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = id;
        return enemyID;
    }
    public string GetOrSetId(SandSpiderWebTrap instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.mainScript.enemyType.enemyName + instance.mainScript.NetworkBehaviourId + instance.trapID; ;
        return enemyID;
    }
    public string GetOrSetId(GrabbableObject instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.itemProperties.itemName + instance.NetworkBehaviourId;
        return enemyID; 
    }
}
class NSUtilities()
{

    public static bool isEnemyReachable(EnemyAI enemy)
    {
        if (enemy is CentipedeAI && ((CentipedeAI)enemy).clingingToCeiling) return false;
        if (enemy is SandWormAI) return false;
        if (enemy is DoublewingAI && ((DoublewingAI)enemy)) return false;
        if (enemy is RadMechAI && ((RadMechAI)enemy).inFlyingMode) return false;
        if (enemy is SandSpiderAI && ((SandSpiderAI)enemy).onWall) return false;
        return true;
    }

    public static bool IsVanilla(object checkInput)
    {
        Script.LogNS(LogLevel.Debug, checkInput.GetType().AssemblyQualifiedName, "IsVanillaCheck");
        return checkInput.GetType().AssemblyQualifiedName == "Assembly-CSharp, 0.0.0.0, .NETStandard, v2.1";
    }
}
