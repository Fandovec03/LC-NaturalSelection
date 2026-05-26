using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace NaturalSelection.Networking
{
    public class SlimeNetworking : NetworkBehaviour, INaturalSelectNetworking
    {
        BlobAI parentEnemy = null;
        BlobData data = null;
        EnemyAI INaturalSelectNetworking.OwningEnemy()
        {
            return parentEnemy;
        }
        public SlimeNetworking()
        {
            parentEnemy = this.GetComponent<BlobAI>();
            data = (BlobData)Utilities.GetEnemyData(parentEnemy, new BlobData());
        }

        [ServerRpc]
        public void BlobConsumeEventServerRPC()
        {
            BlobConsumeEventClientRPC();
        }

        [ClientRpc]
        public void BlobConsumeEventClientRPC()
        {
            data.playSound = true;
        }
    }
}
 