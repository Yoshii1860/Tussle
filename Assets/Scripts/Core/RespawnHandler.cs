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

        ulong clientId = player.OwnerClientId;
        player.NetworkObject.Despawn();

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

        playerInstance.NetworkObject.SpawnAsPlayerObject(clientId);
        playerInstance.Wallet.CoinCount.Value += coinsKept;

        Leaderboard.Instance.GetEntityDisplay(playerInstance.OwnerClientId).UpdateDisplayText();
    }
}
