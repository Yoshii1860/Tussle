using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject priestPrefab;
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private GameObject thiefPrefab;

    private const string GameSceneName = "Game"; // Replace with your actual game scene name

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("PlayerSpawner: Server spawned, registering OnClientConnected callback.");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"PlayerSpawner: Client {clientId} connected, starting spawn coroutine.");

        StartCoroutine(SpawnPlayer(clientId));
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        Debug.Log($"PlayerSpawner: Spawning player for client {clientId}");
        yield return null;

        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == GameSceneName);

        if (NetworkServer.Instance.TryGetCharacterId(clientId, out int characterId))
        {
            GameObject prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(characterId);
            if (prefabToSpawn == null)
            {
                Debug.LogError($"PlayerSpawner: Failed to retrieve prefab for CharacterId {characterId}. Defaulting to Knight.");
                prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(0); // Default to Knight
            }

            float[] spawnPosition;
            if (NetworkServer.Instance.TryGetSpawnPosition(clientId, out spawnPosition))
            {
                Debug.Log($"PlayerSpawner: Spawn position for client {clientId} is {spawnPosition}");
            }
            else
            {
                Debug.LogError($"PlayerSpawner: Failed to get spawn position for client {clientId}, defaulting to Vector3.zero");
                spawnPosition = new float[] { 0, 0, 0 };
            }
            Vector3 newSpawnPosition = new Vector3(spawnPosition[0], spawnPosition[1], spawnPosition[2]);

            // Spawn the player object
            GameObject playerInstance = Instantiate(prefabToSpawn, newSpawnPosition, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
            Debug.Log($"Spawned player {clientId} as character ID {characterId}");
        }
        else
        {
            Debug.LogError($"Failed to get character ID for client {clientId}, defaulting to Knight");
            GameObject prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(0); // Default to Knight
            GameObject playerInstance = Instantiate(knightPrefab, Vector3.zero, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("PlayerSpawner: Disabled");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}