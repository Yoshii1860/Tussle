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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: Duplicate instance detected, destroying self.");
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
/*
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            StartCoroutine(SpawnPlayer(clientId));
        }
*/
    }

    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(SpawnPlayer(clientId));
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        yield return null;

        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == GameSceneName);
        }

        if (NetworkServer.Instance == null)
        {
            Debug.LogError("PlayerSpawner: NetworkServer.Instance is null, cannot spawn player.");
            yield break;
        }

        if (NetworkServer.Instance.TryGetCharacterId(clientId, out int characterId))
        {
            GameObject prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(characterId);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"PlayerSpawner: No prefab found for CharacterId {characterId}. Defaulting to Knight.");
                prefabToSpawn = PrefabManager.Instance.GetPrefabByCharacterId(0);
            }

            float[] spawnPosition = SpawnPoint.GetRandomSpawnPos();
            if (spawnPosition == null || spawnPosition.Length != 3)
            {
                Debug.LogWarning($"PlayerSpawner: Invalid spawn position for client {clientId}, defaulting to Vector3.zero.");
                spawnPosition = new float[] { 0, 0, 0 };
            }

            Vector3 newSpawnPosition = new Vector3(spawnPosition[0], spawnPosition[1], spawnPosition[2]);

            yield return new WaitForSeconds(0.5f);
            GameObject playerInstance = Instantiate(prefabToSpawn, newSpawnPosition, Quaternion.identity);
            if (playerInstance.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
                Debug.Log($"PlayerSpawner: Spawned player {clientId} as character ID {characterId} at {newSpawnPosition}");
            }
            else
            {
                Debug.LogError($"PlayerSpawner: Prefab {prefabToSpawn.name} missing NetworkObject. Destroying instance.");
                Destroy(playerInstance);
            }
        }
        else
        {
            Debug.LogWarning($"PlayerSpawner: Failed to get character ID for client {clientId}, defaulting to Knight.");
            if (knightPrefab == null)
            {
                Debug.LogError("PlayerSpawner: knightPrefab is null, cannot spawn default player.");
                yield break;
            }
            GameObject playerInstance = Instantiate(knightPrefab, Vector3.zero, Quaternion.identity);
            if (playerInstance.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
                Debug.Log($"PlayerSpawner: Spawned default player {clientId} as Knight at Vector3.zero");
            }
            else
            {
                Debug.LogError("PlayerSpawner: Knight prefab missing NetworkObject. Destroying instance.");
                Destroy(playerInstance);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}