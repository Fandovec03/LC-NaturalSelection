using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
namespace NaturalSelection.Generics;

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
        return checkInput.GetType().AssemblyQualifiedName == "";
    }
}
