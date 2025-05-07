using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject priestPrefab;
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private GameObject thiefPrefab;
    [SerializeField] private PlayerSelectionManager selectionManager;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(WaitForSelectionAndSpawn(clientId));
    }

    private IEnumerator WaitForSelectionAndSpawn(ulong clientId)
    {
        // Wait until the selection is available in the PlayerSelectionManager
        while (selectionManager.GetCharacterSelection(clientId) == CharacterType.Knight &&
               !selectionManager.HasSelection(clientId))
        {
            yield return null; // Wait for the next frame
        }

        // Retrieve the character type
        CharacterType characterType = selectionManager.GetCharacterSelection(clientId);

        // Determine the prefab to spawn
        GameObject prefabToSpawn = characterType switch
        {
            CharacterType.Knight => knightPrefab,
            CharacterType.Archer => archerPrefab,
            CharacterType.Priest => priestPrefab,
            CharacterType.Soldier => soldierPrefab,
            CharacterType.Thief => thiefPrefab,
            _ => knightPrefab // Default to Knight if something goes wrong
        };

        // Spawn the player object
        GameObject playerInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}