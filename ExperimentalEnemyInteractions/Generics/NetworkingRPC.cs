using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace NaturalSelection.Generics
{

    internal class NetworkingRPC : NetworkBehaviour
    {
        internal static GameObject prefab;

        // Template from ButteRyBalance by ButteryStancakes
        internal static void Init()
        {
            if (prefab != null)
            {
                Script.Logger.LogDebug("Networking | Prefab already exist");
            }

            try
            {

                prefab = new(nameof(NetworkingRPC))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };

                NetworkObject networkObj = prefab.AddComponent<NetworkObject>();
                byte[] hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(typeof(NetworkingRPC).Assembly.GetName().Name + prefab.name));
                //networkObj. = System.Convert.ToUInt32(hash);

                prefab.AddComponent<NetworkingRPC>();
                NetworkManager.Singleton.AddNetworkPrefab(prefab);

                Script.Logger.LogDebug("Successfully instantiated network prefab");
            }
            catch (Exception e)
            {
                Script.Logger.LogError($"Failed instantiated network prefab! \n \n {e}");
            }
        }

        internal static void CreateNetworkObject()
        {
            try
            {
                if ((NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost) && prefab != null)
                {
                    Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<NetworkObject>().Spawn();
                }
            }
            catch (Exception e)
            {
                Script.Logger.LogError($"Failed to create network object! \n \n {e}");
            }
        }

        public static NetworkingRPC Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }

            Instance = this;

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = true)]
        public void NetworkBehaviorServerRpc()
        {
            NetworkBehaviorClientRpc();
            Script.Logger.LogMessage("Registered serverRPC");
        }

        [ClientRpc]
        public void NetworkBehaviorClientRpc()
        {
            Script.Logger.LogMessage("Registered clientRPC");
        }

    }
}
