using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx.Logging;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using HarmonyLib;
using NaturalSelection.EnemyPatches;
using NaturalSelectionLib.Comp;
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
    private object? owner;
    int CustomBehaviorStateIndex = 0;
    public EnemyAI? closestEnemy;
    public EnemyAI? targetEnemy;
    public CustomEnemySize customEnemySize = CustomEnemySize.Small;
    public string enemyID = "";
    internal Action<EnemyAI?>? ChangeClosestEnemyAction;
    private bool subscribed;
    public float coroutineTimer = 0f;

    public void SetOwner(object owner)
    {
        if (this.owner == null) this.owner = owner;
        return;
    }
    public void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;
        ChangeClosestEnemyAction += UpdateClosestEnemy;
    }
    public void Unsubscribe()
    {
        if (!subscribed) return;
        ChangeClosestEnemyAction -= UpdateClosestEnemy;
    }
    public void UpdateClosestEnemy(EnemyAI? importClosestEnemy)
    {
        if (this.owner == null)
        {
            Script.LogNS(LogLevel.Warning, "NULL owner! Unsubscribing...", this.owner);
            Unsubscribe();
        }
        this.closestEnemy = importClosestEnemy;
    }

    public void ReactToAttack(EnemyAI owner, EnemyAI attacker, int damage = 0)
    {
        Script.LogNS(LogLevel.Info, $"{LibraryCalls.DebugStringHead(attacker)} hit {LibraryCalls.DebugStringHead(attacker)} with {damage} damage");
    }

    public string GetOrSetId(EnemyAI instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.enemyType.enemyName + instance.NetworkObjectId;
        return enemyID;
    }

    public string GetOrSetId(string id)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = id;
        return enemyID;
    }
    public string GetOrSetId(SandSpiderWebTrap instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.mainScript.enemyType.enemyName + instance.mainScript.NetworkObjectId + instance.trapID; ;
        return enemyID;
    }
    public string GetOrSetId(GrabbableObject instance)
    {
        if (String.IsNullOrEmpty(enemyID)) enemyID = instance.itemProperties.itemName + instance.NetworkObjectId;
        return enemyID; 
    }
}
class Utilities
{
    public static Dictionary<string, EnemyDataBase> enemyDataDict = [];
    public static bool IsEnemyReachable(EnemyAI enemy)
    {
        if (enemy is CentipedeAI && ((CentipedeAI)enemy).clingingToCeiling) return false;
        if (enemy is SandWormAI) return false;
        if (enemy is DoublewingAI && ((DoublewingAI)enemy)) return false;
        if (enemy is RadMechAI && ((RadMechAI)enemy).inFlyingMode) return false;
        if (enemy is SandSpiderAI && ((SandSpiderAI)enemy).onWall) return false;
        return true;
    }

    public static bool IsVanilla(EnemyAI checkInput)
    {
        Script.LogNS(LogLevel.Debug, $"{checkInput.enemyType.enemyName}>{checkInput.GetType().Assembly.FullName}", "IsVanillaCheck");
        return checkInput.GetType().Assembly.FullName == "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    }

    public static EnemyDataBase GetEnemyData(object __instance, EnemyDataBase enemyData, bool returnToEnemyAIType = false)
    {
        string id = GetDataID(__instance, returnToEnemyAIType);
        if (id == "-1" || __instance == null) return null;

        if (!enemyDataDict.ContainsKey(id))
        {
            Script.LogNS(LogLevel.Info, $"Missing data container for {LibraryCalls.DebugStringHead(__instance)}. Creating new data container...");
            enemyDataDict.Add(id, enemyData);
            enemyDataDict[id].SetOwner(__instance);
            enemyDataDict[id].Subscribe();
        }
        return enemyDataDict[id];
    }

    public static void TryGetEnemyData(object __instance, out EnemyDataBase temp, bool returnToEnemyAIType = false)
    {
        string id = GetDataID(__instance, returnToEnemyAIType);

        enemyDataDict.TryGetValue(id, out temp);
    }

    public static void DeleteData(object instance, bool returnToEnemyAIType = false)
    {
        string id = GetDataID(instance, returnToEnemyAIType);
        if (enemyDataDict.ContainsKey(id)) enemyDataDict.Remove(id);
    }

    public static string GetDataID(object instance, bool returnToEnemyAIType = false)
    {
        string id = "-1";
        if (instance is EnemyAI)
        {
            id = ((EnemyAI)instance).enemyType.enemyName + ((EnemyAI)instance).NetworkObjectId;
            if (returnToEnemyAIType) id += ".base";
        }
        else if (instance is SandSpiderWebTrap) id = ((SandSpiderWebTrap)instance).mainScript.enemyType.enemyName + ((SandSpiderWebTrap)instance).mainScript.NetworkObjectId + "SpiderWeb" + ((SandSpiderWebTrap)instance).trapID;
        else if (instance is string) id = (string)instance;

        return id;
    }

}
