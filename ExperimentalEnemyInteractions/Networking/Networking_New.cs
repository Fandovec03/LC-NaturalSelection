using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Unity;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace NaturalSelection.Networking
{
    public class Networking_New : NetworkBehaviour, INaturalSelectNetworking
    {
        public delegate void NetworkDelegate(NetworkObjectReference netRef, string ID);

        public NetworkDelegate? testDelegate;

        EnemyAI INaturalSelectNetworking.OwningEnemy()
        {
            return this.GetComponent<EnemyAI>();
        }

        public INaturalSelectNetworking.NaturalNetworkDelegate? testInterfaceDelegate;

        /*
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
        */

        [ServerRpc]
        public void TestServerRPC(NetworkObjectReference noRef, string ID)
        {
            Script.LogNS(LogLevel.Message,"Natural selection Triggered ServerRPC");
            TestClientRPC(noRef, ID);
        }

        [ClientRpc]
        public void TestClientRPC(NetworkObjectReference noRef, string ID)
        {
            Script.LogNS(LogLevel.Message, "Natural selection Triggered ClientRPC");
            noRef.TryGet(out NetworkObject NetworkObj);

            if (NetworkObj != null)
            {
                Script.LogNS(LogLevel.Message, $"Got Referenced Network object: ID {NetworkObj.NetworkObjectId}");
            }
            Script.LogNS(LogLevel.Message, $"Got passed ID {ID}");

            this.testDelegate?.Invoke(noRef, ID);
            this.testInterfaceDelegate?.Invoke();

        }

        //public static Dictionary<string, Type> NetworkingDictionary = new Dictionary<string, Type>();
        //static bool logNetworking = Script.Bools["debugNetworking"];
        
    }
}
