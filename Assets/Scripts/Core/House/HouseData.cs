using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;

public class HouseData : NetworkBehaviour
{
    [SerializeField] private Transform playerEnterPoint;
    [SerializeField] private Transform playerExitPoint;
    [SerializeField] private GameObject houseInstance;
    [SerializeField] private string areaName;
    [SerializeField] private bool isLocked = false;
    private HashSet<ulong> playersInside = new HashSet<ulong>();
    private Dictionary<ulong, Action> playerDieHandlers = new Dictionary<ulong, Action>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        houseInstance.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || collision.gameObject.layer != LayerMask.NameToLayer("Walk")) return;

        if (isLocked)
        {
            if (collision.TryGetComponent<Player>(out Player player))
            {
                if (!player.HasKey())
                {
                    Debug.Log($"House {gameObject.name} is locked and player {player.PlayerName.Value} does not have a key.");
                    return; // Prevent entering if the house is locked and player doesn't have a key
                }
                else
                {
                    player.GetComponent<PlayerUIManager>().RemoveBuffClientRpc(ObjectType.Key);
                    Debug.Log($"House {gameObject.name} is locked, but player {player.PlayerName.Value} has a key.");
                }
            }
        }

        if (collision.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            ulong clientId = networkObject.OwnerClientId;
            if (!playersInside.Contains(clientId))
            {
                if (networkObject.TryGetComponent<Player>(out Player player))
                {
                    AudioManager.Instance.PlayDoorSFX();
                    EnterHouseClientRpc(clientId);
                    player.TeleportClientRpc(playerEnterPoint.position);
                    EnterHouseServerRpc(clientId);

                    Action handler = () => PlayerRequestingExitServerRpc(clientId, true);
                    player.GetComponent<Health>().OnDie += handler;
                    playerDieHandlers[clientId] = handler;

                    if (!string.IsNullOrEmpty(areaName))
                    {
                        AudioManager.Instance.PlayAreaMusic(areaName);
                    }
                }
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
    public void PlayerRequestingExitServerRpc(ulong clientId, bool forceExit = false)
    {
        if (!IsServer || !playersInside.Contains(clientId)) return;

        Player player = GameManager.Instance.GetPlayer(clientId);
        if (player != null)
        {
            if (playerDieHandlers.TryGetValue(clientId, out Action handler))
            {
                player.GetComponent<Health>().OnDie -= handler;
                playerDieHandlers.Remove(clientId);
            }

            if (!forceExit)
            {
                ExitHouseClientRpc(clientId);
                player.TeleportClientRpc(playerExitPoint.position);
            }

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