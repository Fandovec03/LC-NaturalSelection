using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace NaturalSelection.Networking
{
    internal class BeesNetworking : NetworkBehaviour, INaturalSelectNetworking
    {
        RedLocustBees parentEnemy = null;
        BeeValues data = null;
        EnemyAI INaturalSelectNetworking.OwningEnemy()
        {
            return parentEnemy;
        }

        public BeesNetworking()
        {
            parentEnemy = this.GetComponent<RedLocustBees>();
            data = (BeeValues)Utilities.GetEnemyData(parentEnemy, new BlobData());
        }

        public NetworkVariable<float> setFireChance = new NetworkVariable<float>(0);
        public NetworkVariable<float> MaxChance = new NetworkVariable<float>(0);
    }
}
