using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;

public class HouseData : NetworkBehaviour
{
    [SerializeField] private Transform playerEnterPoint;
    [SerializeField] private Transform playerExitPoint;
    [SerializeField] private GameObject houseInstance;
    private HashSet<ulong> playersInside = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        houseInstance.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || !collision.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) return;

        ulong clientId = networkObject.OwnerClientId;
        if (!playersInside.Contains(clientId))
        {
            if (networkObject.TryGetComponent<Player>(out Player player))
            {
                EnterHouseClientRpc(clientId);
                player.TeleportClientRpc(playerEnterPoint.position);
                EnterHouseServerRpc(clientId);
            }
        }
    }



    ////////// Enter House Rpcs //////////

    [ClientRpc]
    private void EnterHouseClientRpc(ulong clientId)
    {
        houseInstance.SetActive(true);

        Player player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            player.BlackscreenFade(1f, 1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnterHouseServerRpc(ulong clientId)
    {
        houseInstance.SetActive(true);

        playersInside.Add(clientId);
    }



    ////////// Exit House Rpcs //////////

    [ServerRpc(RequireOwnership = false)]
    public void PlayerRequestingExitServerRpc(ulong clientId)
    {
        if (!IsServer || !playersInside.Contains(clientId)) return;

        Player player = GameManager.Instance.GetPlayer(clientId);
        if (player != null)
        {
            ExitHouseClientRpc(clientId);
            player.TeleportClientRpc(playerExitPoint.position);
            ExitHouseServerRpc(clientId);
        }
    }

    [ClientRpc]
    private void ExitHouseClientRpc(ulong clientId)
    {
        Player player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            player.BlackscreenFade(1f, 1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitHouseServerRpc(ulong clientId)
    {
        playersInside.Remove(clientId);
    }
}