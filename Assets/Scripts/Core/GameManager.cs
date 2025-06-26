using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private Dictionary<ulong, Player> playersByClientId = new();
    private Dictionary<ulong, bool> playerHasKey = new();
    private Tilemap[] tilemaps;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
    }

    public Tilemap[] GetAllTilemaps()
    {
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogWarning("GameManager: No tilemaps found in the scene.");
            return new Tilemap[0];
        }
        return tilemaps;
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

    public Player[] GetAllPlayers()
    {
        return new List<Player>(playersByClientId.Values).ToArray();
    }

    public void SetPlayerHasKey(ulong clientId, bool hasKey)
    {
        if (playerHasKey.ContainsKey(clientId))
        {
            playerHasKey[clientId] = hasKey;
        }
        else
        {
            playerHasKey.Add(clientId, hasKey);
        }
    }

    public bool GetPlayerHasKey(ulong clientId)
    {
        if (playerHasKey.TryGetValue(clientId, out var hasKey))
        {
            return hasKey;
        }
        Debug.LogWarning($"GameManager: Player {clientId} has no key status registered.");
        return false;
    }
}