using NaturalSelection.EnemyPatches;
using System;
using System.Collections.Generic;
using System.Text;
using Unity;
using Unity.Netcode;
using UnityEngine;

namespace NaturalSelection.Generics
{
    internal class Networking_New : NetworkBehaviour
    {
        /*
        public static Networking_New instance;

        public Networking_New()
        {
            if (instance != null)
            {
                this.OnDestroy();
                return;
            }
            instance = this;
        }

        [ServerRpc(RequireOwnership = true)]
        public void SandWormBehaviorStateServerRPC(NetworkBehaviour behaviour, int value)
        {
            Script.Logger.LogDebug($"ServerRPC NetworkBehaviorStat = {value}");
            SandWormBehaviorStateClientRPC(behaviour, value);
        }

        [ClientRpc]
        public void SandWormBehaviorStateClientRPC(NetworkBehaviour behaviour, int value)
        {
            Script.Logger.LogDebug($"ClientRPC NetworkBehaviorStat = {value}");
            SandWormAIPatch.ClientNetworkBehaviorState(behaviour, value);
        }

        [ServerRpc(RequireOwnership = true)]
        public void MovingTowardsPlayerServerRPC(NetworkBehaviour behaviour, bool value)
        {
            Script.Logger.LogDebug($"ServerRPC MovingTowardsTargetPlayer = {value}");
            SandWormAIPatch.ClientMovingTowardsPlayer(behaviour, value);
        }

        [ClientRpc]
        public void MovingTowardsPlayerClientRPC(NetworkBehaviour behaviour, bool value)
        {
            Script.Logger.LogDebug($"ClientRPC MovingTowardsTargetPlayer = {value}");
            SandWormAIPatch.ClientMovingTowardsPlayer(behaviour, value);
        }

        [ServerRpc(RequireOwnership = true)]
        public void MovingTowardsEnemyServerRPC(NetworkBehaviour behaviour, bool value)
        {
            Script.Logger.LogDebug($"ServerRPC MovingTowardsEnemy = {value}");
            MovingTowardsEnemyClientRPC(behaviour, value);
        }

        [ClientRpc]
        public void MovingTowardsEnemyClientRPC(NetworkBehaviour behaviour, bool value)
        {
            Script.Logger.LogDebug($"ClientRPC MovingTowardsEnemy = {value}");
            SandWormAIPatch.ClientMovingTowardsEnemy(behaviour, value);
        }

        public static Dictionary<string, Type> NetworkingDictionary = new Dictionary<string, Type>();
        static bool logNetworking = Script.Bools["debugNetworking"];
        */
    }
}
