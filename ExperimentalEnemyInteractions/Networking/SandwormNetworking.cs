using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace NaturalSelection.Networking
{
    internal class SandwormNetworking : NetworkBehaviour, INaturalSelectNetworking
    {
        RedLocustBees parentEnemy = null;
        BeeValues data = null;
        EnemyAI INaturalSelectNetworking.OwningEnemy()
        {
            return parentEnemy;
        }

        public SandwormNetworking()
        {
            parentEnemy = this.GetComponent<RedLocustBees>();
            data = (BeeValues)Utilities.GetEnemyData(parentEnemy, new BlobData());
        }
    }
}
