using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class Leaderboard : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntityDisplay leaderboardEntityPrefab;

    [Header("Settings")]
    [SerializeField] private const int MaxEntitiesToDisplay = 10;

    private NetworkList<LeaderboardEntityState> leaderboardEntities = new NetworkList<LeaderboardEntityState>();

    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>();

    public static Leaderboard Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this && IsServer)
        {
            Debug.LogWarning("Leaderboard: Duplicate instance detected, destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("Leaderboard: Awake called, NetworkList initialized.");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Leaderboard: OnNetworkSpawn called. IsServer={IsServer}, IsClient={IsClient}");

        if (IsClient)
        {
            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged;
            Debug.Log($"Leaderboard: Subscribed to OnListChanged. Current count: {leaderboardEntities.Count}");
            foreach (LeaderboardEntityState entity in leaderboardEntities)
            {
                Debug.Log($"Leaderboard: Existing entity on spawn: {entity.PlayerName} ({entity.ClientId})");
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }
        }

#if UNITY_SERVER
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        Debug.Log($"Leaderboard: Found {players.Length} players on server spawn.");
        foreach (Player player in players)
        {
            Debug.Log($"Leaderboard: Adding player {player.PlayerName.Value} ({player.OwnerClientId}) to leaderboard.");
            HandlePlayerSpawned(player);
        }

        Player.OnPlayerSpawned += HandlePlayerSpawned;
        Player.OnPlayerDespawned += HandlePlayerDespawned;
        Debug.Log("Leaderboard: Subscribed to Player.OnPlayerSpawned and OnPlayerDespawned.");
#endif
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged;
            Debug.Log("Leaderboard: Unsubscribed from OnListChanged.");
        }

#if UNITY_SERVER
        Player.OnPlayerSpawned -= HandlePlayerSpawned;
        Player.OnPlayerDespawned -= HandlePlayerDespawned;
        Debug.Log("Leaderboard: Unsubscribed from Player.OnPlayerSpawned and OnPlayerDespawned.");
#endif

    }

    public void UpdateKills(ulong clientId, int kills)
    {
        Debug.Log($"Leaderboard: UpdateKills called for clientId={clientId}, kills={kills}");
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId == clientId)
            {
                leaderboardEntities[i] = new LeaderboardEntityState
                {
                    ClientId = clientId,
                    PlayerName = leaderboardEntities[i].PlayerName,
                    Kills = kills,
                    Coins = leaderboardEntities[i].Coins
                };
                Debug.Log($"Leaderboard: Updated kills for {clientId} to {kills}");
                break;
            }
        }
    }

    private void HandlePlayerSpawned(Player player)
    {
        int kills = 0;
        if (player.TryGetComponent<KillCounter>(out var killCounter))
        {
            kills = killCounter.Kills;
            Debug.Log($"Leaderboard: Player {player.PlayerName.Value} has {kills} kills on spawn.");
        }

        Debug.Log($"Leaderboard: HandlePlayerSpawned called for {player.PlayerName.Value} ({player.OwnerClientId})");
        leaderboardEntities.Add(new LeaderboardEntityState
        {
            ClientId = player.OwnerClientId,
            PlayerName = player.PlayerName.Value,
            Kills = kills,
            Coins = 0
        });

        player.Wallet.CoinCount.OnValueChanged += (oldCoins, newCoins) =>
            HandleCoinsChanged(player.OwnerClientId, newCoins);
    }

    private void HandlePlayerDespawned(Player player)
    {
        Debug.Log($"Leaderboard: HandlePlayerDespawned called for {player.PlayerName.Value} ({player.OwnerClientId})");
        foreach (LeaderboardEntityState entity in leaderboardEntities)
        {
            if (entity.ClientId != player.OwnerClientId) { continue; }

            leaderboardEntities.Remove(entity);
            Debug.Log($"Leaderboard: Removed entity for {player.OwnerClientId}");
            break;
        }

        player.Wallet.CoinCount.OnValueChanged -= (oldCoins, newCoins) =>
            HandleCoinsChanged(player.OwnerClientId, newCoins);
    }

    private void HandleCoinsChanged(ulong clientId, int newCoins)
    {
        Debug.Log($"Leaderboard: HandleCoinsChanged called for clientId={clientId}, newCoins={newCoins}");
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId != clientId) { continue; }

            leaderboardEntities[i] = new LeaderboardEntityState
            {
                ClientId = leaderboardEntities[i].ClientId,
                PlayerName = leaderboardEntities[i].PlayerName,
                Kills = leaderboardEntities[i].Kills,
                Coins = newCoins
            };

            Debug.Log($"Leaderboard: Updated coins for {clientId} to {newCoins}");
            return;
        }
    }

    public LeaderboardEntityDisplay GetEntityDisplay(ulong clientId)
    {
        return entityDisplays.FirstOrDefault(x => x.ClientId == clientId);
    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        Debug.Log($"Leaderboard: HandleLeaderboardEntitiesChanged called. Type={changeEvent.Type}, ClientId={changeEvent.Value.ClientId}");

        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    Debug.Log($"Leaderboard: Instantiating display for {changeEvent.Value.PlayerName} ({changeEvent.Value.ClientId})");
                    LeaderboardEntityDisplay leaderboardEntity = Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);
                    leaderboardEntity.Initialise(
                        changeEvent.Value.ClientId,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.Kills,
                        changeEvent.Value.Coins);
                    entityDisplays.Add(leaderboardEntity);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                LeaderboardEntityDisplay entityDisplay = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (entityDisplay != null)
                {
                    Debug.Log($"Leaderboard: Destroying display for {changeEvent.Value.ClientId}");
                    entityDisplay.transform.SetParent(null);
                    Destroy(entityDisplay.gameObject);
                    entityDisplays.Remove(entityDisplay);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                LeaderboardEntityDisplay displayToUpdate = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    Debug.Log($"Leaderboard: Updating display values for {changeEvent.Value.ClientId}");
                    displayToUpdate.UpdateValues(changeEvent.Value.Coins, changeEvent.Value.Kills);
                }
                break;
        }

        entityDisplays.Sort((x, y) =>
            (y.Kills * LeaderboardEntityDisplay.KillScoreMultiplier + y.Coins).CompareTo
            (x.Kills * LeaderboardEntityDisplay.KillScoreMultiplier + x.Coins));

        for (int i = 0; i < entityDisplays.Count; i++)
        {
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].UpdateDisplayText();
            entityDisplays[i].gameObject.SetActive(i < MaxEntitiesToDisplay);
        }

        LeaderboardEntityDisplay myDisplay =
            entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);

        if (myDisplay != null)
        {
            if (myDisplay.transform.GetSiblingIndex() >= MaxEntitiesToDisplay)
            {
                leaderboardEntityHolder.GetChild(MaxEntitiesToDisplay - 1).gameObject.SetActive(false);
                myDisplay.gameObject.SetActive(true);
            }
        }
    }
}