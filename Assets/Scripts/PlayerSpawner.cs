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
            Debug.Log($"PlayerSpawner: Found character ID {characterId} for client {clientId}");
            GameObject prefabToSpawn = characterId switch
            {
                0 => knightPrefab,
                1 => archerPrefab,
                2 => priestPrefab,
                3 => soldierPrefab,
                4 => thiefPrefab,
                _ => knightPrefab // Default to Knight if something goes wrong
            };
            Debug.Log($"PlayerSpawner: Spawning prefab {prefabToSpawn.name} for character ID {characterId}");

            // Spawn the player object
            GameObject playerInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
            Debug.Log($"Spawned player {clientId} as character ID {characterId}");
        }
        else
        {
            Debug.LogError($"Failed to get character ID for client {clientId}, defaulting to Knight");
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