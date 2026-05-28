using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace NaturalSelection.Networking
{
    internal class ForestGiantNetworking : NetworkBehaviour, INaturalSelectNetworking
    {
        RedLocustBees parentEnemy = null;
        GiantData data = null;

        public INaturalSelectNetworking.NaturalNetworkDelegate? setGiantOnFire;
        public INaturalSelectNetworking.NaturalNetworkDelegate? extinguishFire;

        EnemyAI INaturalSelectNetworking.OwningEnemy()
        {
            return parentEnemy;
        }

        public ForestGiantNetworking()
        {
            parentEnemy = this.GetComponent<RedLocustBees>();
            data = (GiantData)Utilities.GetEnemyData(parentEnemy, new GiantData());
        }

        [ServerRpc]
        public void SetGiantOnFireServerRpc()
        {
            SetGiantOnFireClientRpc();
        }

        [ClientRpc]
        public void SetGiantOnFireClientRpc()
        {
            setGiantOnFire?.Invoke();
        }

        [ServerRpc]
        public void ExtinguishFireServerRpc()
        {
            ExtinguishFireClientRpc();
        }

        [ClientRpc]
        public void ExtinguishFireClientRpc()
        {
            extinguishFire?.Invoke();
        }
    }
}
