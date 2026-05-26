using BepInEx;
using Imperium.API;
using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;

namespace NaturalSelection.Compatibility
{
    internal class ImperiumCompat
    {
        private static string getClosestEnemy(EnemyAI enemyAI)
        {
            EnemyDataBase? data = Utilities.TryGetEnemyData(enemyAI);
            string temp = "-";
            if (data != null && data.closestEnemy != null) {
                temp = $"{data.closestEnemy.enemyType.enemyName} #{data.closestEnemy.NetworkObjectId}";
            }
            return temp;
        }

        private static string getTargetEnemy(EnemyAI enemyAI)
        {
            EnemyDataBase? data = Utilities.TryGetEnemyData(enemyAI);
            string temp = "-";
            if (data != null && data.targetEnemy != null)
            {
                temp = $"{data.targetEnemy.enemyType.enemyName} #{data.targetEnemy.NetworkObjectId}";
            }
            return temp;
        }

        public static void registerEnemyVisualization()
        {
            try
            {
                Visualization.InsightsFor<EnemyAI>()
                .RegisterInsight("T Enemy", entity => getTargetEnemy(entity))
                .RegisterInsight("C Enemy", entity => getClosestEnemy(entity));
            }
            catch
            {

            }
        }
    }
}
