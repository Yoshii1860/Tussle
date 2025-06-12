using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private Dictionary<ulong, Player> playersByClientId = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterPlayer(Player player)
    {
        playersByClientId[player.OwnerClientId] = player;
        Debug.LogWarning($"GameManager: Player {player.OwnerClientId} registered.");
    }

    public void UnregisterPlayer(Player player)
    {
        playersByClientId.Remove(player.OwnerClientId);
        Debug.LogWarning($"GameManager: Player {player.OwnerClientId} registered.");
    }

    public Player GetPlayer(ulong clientId)
    {
        playersByClientId.TryGetValue(clientId, out var player);
        return player;
    }
}