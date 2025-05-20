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

    private const string GameSceneName = "Game";

    public static PlayerSpawner Instance { get; private set; }

    private void Awake()
    {
        Debug.Log($"PlayerSpawner: Awake called in scene {SceneManager.GetActiveScene().name}");
        DontDestroyOnLoad(gameObject);

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.Log("PlayerSpawner: Duplicate instance detected, destroying self");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        Debug.Log($"PlayerSpawner: Enabled in scene {SceneManager.GetActiveScene().name}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"PlayerSpawner: OnNetworkSpawn called, IsServer: {IsServer}, Scene: {SceneManager.GetActiveScene().name}");
        if (IsServer)
        {
            Debug.Log("PlayerSpawner: Server spawned, registering OnClientConnected callback.");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Check if any clients are already connected
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                Debug.Log($"PlayerSpawner: Found existing client {clientId}, triggering spawn");
                StartCoroutine(SpawnPlayer(clientId));
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"PlayerSpawner: Client {clientId} connected, starting spawn coroutine. Current scene: {SceneManager.GetActiveScene().name}");
        StartCoroutine(SpawnPlayer(clientId));
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        Debug.Log($"PlayerSpawner: Spawning player for client {clientId}, current scene: {SceneManager.GetActiveScene().name}");
        yield return null; // Give a frame for initialization

        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            Debug.LogWarning($"PlayerSpawner: Expected Game scene but current scene is {SceneManager.GetActiveScene().name}, aborting spawn for client {clientId}");
            yield break;
        }

#if UNITY_SERVER
        if (NetworkServer.Instance == null)
        {
            Debug.LogError("PlayerSpawner: NetworkServer.Instance is null, cannot spawn player");
            yield break;
        }

        if (NetworkServer.Instance.TryGetCharacterId(clientId, out int characterId))
        {
            Debug.Log($"PlayerSpawner: Retrieved character ID {characterId} for client {clientId}");
            GameObject prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(characterId);
            if (prefabToSpawn == null)
            {
                Debug.LogError($"PlayerSpawner: Failed to retrieve prefab for CharacterId {characterId}. Defaulting to Knight.");
                prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(0); // Default to Knight
            }

            float[] spawnPosition;
            if (NetworkServer.Instance.TryGetSpawnPosition(clientId, out spawnPosition))
            {
                Debug.Log($"PlayerSpawner: Spawn position for client {clientId} is {spawnPosition[0]}, {spawnPosition[1]}, {spawnPosition[2]}");
            }
            else
            {
                Debug.LogError($"PlayerSpawner: Failed to get spawn position for client {clientId}, defaulting to Vector3.zero");
                spawnPosition = new float[] { 0, 0, 0 };
            }
            Vector3 newSpawnPosition = new Vector3(spawnPosition[0], spawnPosition[1], spawnPosition[2]);

            Debug.Log($"PlayerSpawner: Instantiating prefab {prefabToSpawn.name} for client {clientId}");
            GameObject playerInstance = Instantiate(prefabToSpawn, newSpawnPosition, Quaternion.identity);
            if (playerInstance.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
                Debug.Log($"Spawned player {clientId} as character ID {characterId} at {newSpawnPosition}");
            }
            else
            {
                Debug.LogError($"PlayerSpawner: Prefab {prefabToSpawn.name} does not have a NetworkObject component. Destroying instance.");
                Destroy(playerInstance);
            }
        }
        else
        {
            Debug.LogError($"Failed to get character ID for client {clientId}, defaulting to Knight");
            if (knightPrefab == null)
            {
                Debug.LogError("PlayerSpawner: knightPrefab is null, cannot spawn default player");
                yield break;
            }
            GameObject playerInstance = Instantiate(knightPrefab, Vector3.zero, Quaternion.identity);
            if (playerInstance.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
                Debug.Log($"Spawned default player {clientId} as Knight at Vector3.zero");
            }
            else
            {
                Debug.LogError($"PlayerSpawner: Knight prefab does not have a NetworkObject component. Destroying instance.");
                Destroy(playerInstance);
            }
        }
#endif
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("PlayerSpawner: OnNetworkDespawn called");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}