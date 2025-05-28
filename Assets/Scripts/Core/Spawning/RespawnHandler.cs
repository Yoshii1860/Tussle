using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RespawnHandler : NetworkBehaviour
{
    [SerializeField] private float respawnTime = 5f;

    [Range(0, 1)]
    [SerializeField] private float keptCoinPercentage = 0.66f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (Player player in players)
            {
                // Register the respawn event
                HandlePlayerSpawn(player);
            }

            // Register the respawn event
            Player.OnPlayerSpawned += HandlePlayerSpawn;
            Player.OnPlayerDespawned += HandlePlayerDespawn;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            // Unregister the respawn event
            Player.OnPlayerSpawned -= HandlePlayerSpawn;
            Player.OnPlayerDespawned -= HandlePlayerDespawn;
        }
    }

    private void HandlePlayerSpawn(Player player)
    {
        player.Health.OnDie += (health) => HandlePlayerDeath(player);
    }

    private void HandlePlayerDespawn(Player player)
    {
        player.Health.OnDie -= (health) => HandlePlayerDeath(player);
    }

    private void HandlePlayerDeath(Player player)
    {
        player.Health.PlayDeathAnimationClientRpc();
        
        int keptCoins = (int)(player.Wallet.CoinCount.Value * keptCoinPercentage);

        StartCoroutine(RespawnPlayer(player, keptCoins));
    }

    private IEnumerator RespawnPlayer(Player player, int coinsKept)
    {
        yield return new WaitForSeconds(respawnTime);

        if (player == null) { Debug.LogError("RespawnHandler: player is null!"); yield break; }
        ulong clientId = player.OwnerClientId;
        if (player.NetworkObject == null) { Debug.LogError("RespawnHandler: player.NetworkObject is null!"); yield break; }
        player.NetworkObject.Despawn();

        if (IsServer)
        {
            int charId;

            if (NetworkServer.Instance.TryGetCharacterId(clientId, out int characterId))
            {
                Debug.Log($"RespawnHandler: Respawning player with CharacterId {characterId} for client {clientId}");
                charId = characterId;
            }
            else
            {
                Debug.LogError($"RespawnHandler: Failed to retrieve CharacterId for client {clientId}. Defaulting to Knight.");
                charId = 0; // Default to Knight
            }


            GameObject prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(charId);
            if (prefabToSpawn == null)
            {
                Debug.LogError($"RespawnHandler: Failed to retrieve prefab for CharacterId {charId}. Defaulting to Knight.");
                prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(0); // Default to Knight
            }

            Graveyard nearestGraveyard = Graveyard.GetNearestGraveyard(player.transform.position);
            Vector3 spawnPosition = nearestGraveyard != null ? nearestGraveyard.transform.position : Vector3.zero;

            Player playerInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity).GetComponent<Player>();
            if (playerInstance == null) { Debug.LogError("RespawnHandler: Instantiated prefab does not have a Player component!"); yield break; }
            if (playerInstance.gameObject == null) { Debug.LogError("RespawnHandler: Instantiated player GameObject is null!"); yield break; }

            playerInstance.NetworkObject.SpawnAsPlayerObject(clientId);
            if (playerInstance.Wallet == null) { Debug.LogError("RespawnHandler: playerInstance.Wallet is null!"); }
            playerInstance.Wallet.CoinCount.Value += coinsKept;

            if (Leaderboard.Instance == null) { Debug.LogError("RespawnHandler: Leaderboard.Instance is null!"); }
            var entityDisplay = Leaderboard.Instance?.GetEntityDisplay(playerInstance.OwnerClientId);
            if (entityDisplay != null)
            {
                entityDisplay.UpdateDisplayText();
            }
            else
            {
                Debug.LogWarning($"RespawnHandler: No LeaderboardEntityDisplay found for client {clientId}. Cannot update display text.");
            }
        }
    }
}
