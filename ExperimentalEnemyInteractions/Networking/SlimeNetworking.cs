using NaturalSelection.EnemyPatches;
using NaturalSelection.Generics;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace NaturalSelection.Networking
{
    public class SlimeNetworking : NetworkBehaviour
    {
        BlobAI parentEnemy;
        BlobData data = null;

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
 